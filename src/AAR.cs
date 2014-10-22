using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;

namespace MOSES.AAR
{
	public enum AARState
	{
		stopped,
		playback,
		recording,
		initiailizing,
		error
	}
	public delegate void Logger (string s);

	public enum AAREvent
	{
		AddActor,
		RemoveActor,
		MoveActor
	}

	public interface IDispatch
	{
		UUID createActor(string name, Vector3 position);
		void moveActor(UUID uuid, Vector3 position);
		void deleteActor(UUID uuid);
	}

	public class AAR
	{
		//current state of the AAR
		public AARState state { get; private set; }
		//logging delegate
		private Logger log;
		//callbacks to affec tthe scene
		private IDispatch dispatch;
		//tracking avatars in world
		private Dictionary<OpenMetaverse.UUID, Actor> avatars = new Dictionary<OpenMetaverse.UUID, Actor>();
		//tracking AAR created avatars
		private Dictionary<string, Actor> stooges = new Dictionary<string, Actor>();
		//queue of scene events
		private Queue<Event> action = new Queue<Event>();
		private Stopwatch sw = new Stopwatch();
		private long elapsedTime = 0;

		public AAR(Logger log, IDispatch dispatch)
		{
			this.dispatch = dispatch;
			this.log = log;
			this.state = AARState.stopped;
		}

		public void tick()
		{
			elapsedTime = sw.ElapsedMilliseconds;
			if(state == AARState.playback)
			{
				this.processPlayback();
			}
		}

		public bool startRecording()
		{
			if(this.state == AARState.recording)
			{
				log("Error starting: AAR is already recording");
				return false;
			}
			state = AARState.recording;
			sw.Reset();
			sw.Start();
			elapsedTime = 0;
			action.Clear();
			foreach(Actor a in avatars.Values)
			{
				action.Enqueue(new Event(AAREvent.AddActor, a.name, elapsedTime, a.Position));
			}
			log("Record Start");
			return true;
		}

		public bool stopRecording()
		{
			if( this.state != AARState.recording )
			{
				log("Error stopping: AAR is not recording");
				return false;
			}
			this.state = AARState.stopped;
			sw.Stop();
			return true;
		}

		public bool startPlaying()
		{
			if(state != AARState.stopped)
			{
				log("Error, AAR cannot playback, it is not stopped");
				return false;
			}
			this.state = AARState.playback;
			sw.Reset();
			sw.Start();
			elapsedTime = 0;
			return true;
		}

		public bool stopPlaying()
		{
			if( this.state != AARState.playback )
			{
				log("Error stopping: AAR is not playing back");
				return false;
			}
			this.state = AARState.stopped;
			sw.Stop();
			return true;
		}

		public bool addActor(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				log("Duplicate Presence Detected, not adding avatar");
				return false;
			}
			else
			{
				avatars[client.UUID] = new Actor(client);
				log(string.Format("New Presence: {0} , tracking {1} Actors", this.avatars[client.UUID].name, this.avatars.Count));
				if(this.state == AARState.recording)
				{
					action.Enqueue(new Event(AAREvent.AddActor, avatars[client.UUID].name, elapsedTime, avatars[client.UUID].Position));
				}
				return true;
			}
		}

		public bool actorMoved(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				if( client.AbsolutePosition != this.avatars[client.UUID].Position)
				{
					this.avatars[client.UUID].Position = client.AbsolutePosition;
					if(this.state == AARState.recording)
					{
						action.Enqueue(new Event(AAREvent.MoveActor, avatars[client.UUID].name, elapsedTime, avatars[client.UUID].Position));
					}
					return true;
				}
			}
			return false;
		}

		public bool removeActor(OpenMetaverse.UUID uuid)
		{
			if(this.avatars.ContainsKey(uuid))
			{
				if(this.state == AARState.recording)
				{
					action.Enqueue(new Event(AAREvent.RemoveActor, avatars[uuid].name, elapsedTime, avatars[uuid].Position));
				}
				this.avatars.Remove(uuid);
				return true;
			}
			return false;
		}

		public bool addObject()
		{
			return false;
		}

		public bool removeObject()
		{
			return false;
		}

		public void printActionList()
		{
			switch(state){
			case AARState.playback:
				log("STATE: playback");
				break;
			case AARState.recording:
				log("STATE recording");
				break;
			case AARState.stopped:
				log("STATE stopped");
				break;
			default:
				log("STATE unknown");
				break;
			}
			log(string.Format("Tracked {0} actions", action.Count));
		}

		private void processPlayback()
		{
			if(action.Count == 0){
				state = AARState.stopped;
				log("Playback Completed");
				return;
			}
			log(string.Format("playback at elapsed {0}, next event at {1}", elapsedTime, action.Peek().time));
			while( action.Count > 0 && elapsedTime > action.Peek().time){
				Event e = action.Dequeue();
				switch(e.type){
				case AAREvent.AddActor:
					log(string.Format("Adding actor {0} at {1}", e.description, e.position));
					UUID uuid = dispatch.createActor(e.description, e.position);
					stooges[e.description] = new Actor(uuid,e.description,e.position);
					break;
				case AAREvent.MoveActor:
					log(string.Format("Moving actor {0} to {1}", e.description, e.position));
					dispatch.moveActor(stooges[e.description].uuid, e.position);
					break;
				case AAREvent.RemoveActor:
					log(string.Format("Removing actor {0}", e.description));
					dispatch.deleteActor(stooges[e.description].uuid);
					break;
				default:
					log("Invalid command during playback");
					break;
				}
			}
		}
	}

	class Event
	{
		public AAREvent type;
		public string description;
		public long time;
		public Vector3 position;

		public Event(AAREvent type, string description, long time, Vector3 position)
		{
			this.type = type;
			this.description = description;
			this.time = time;
			this.position = position;
		}

		public override string ToString ()
		{
			return base.ToString() + ": " + type.ToString() + ", " + description + ", " + time.ToString();
		}
	}

	class Actor
	{
		public UUID uuid { get; private set; }
		public string name { get; private set; }
		public Vector3 Position { get; set;}

		public Actor(ScenePresence presence)
		{
			this.uuid = presence.UUID;
			this.name = string.Format("{0} {1}", presence.Firstname, presence.Lastname);
			this.Position = presence.AbsolutePosition;
		}

		public Actor(UUID uuid, string name, Vector3 position)
		{
			this.uuid = uuid;
			this.name = name;
			this.Position = position;
		}
	}
}