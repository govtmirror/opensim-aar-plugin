using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;

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
		MoveActor,
		EventBegin,
		EventEnd
	}

	public interface IDispatch
	{
		UUID createActor(string firstName, string lastName, AvatarAppearance appearance, Vector3 position);
		void moveActor(UUID uuid, Vector3 position, OpenSim.Framework.Animation animation);
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
		private Queue<Event> recordedActions = new Queue<Event>();
		//queue of processed scene events
		private Queue<Event> processedActions = new Queue<Event>();
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
			recordedActions.Clear();
			recordedActions.Enqueue(new Event(AAREvent.EventBegin, elapsedTime));
			foreach(Actor a in avatars.Values)
			{
				recordedActions.Enqueue(new ActorEvent(AAREvent.AddActor, a.firstName, a.lastName, a.appearance, a.Position, elapsedTime));
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
			recordedActions.Enqueue(new Event(AAREvent.EventEnd, elapsedTime));
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
				log(string.Format("New Presence: {0} , tracking {1} Actors", this.avatars[client.UUID].firstName, this.avatars.Count));
				if(this.state == AARState.recording)
				{
					recordedActions.Enqueue(new ActorEvent(AAREvent.AddActor, avatars[client.UUID].firstName, avatars[client.UUID].lastName, avatars[client.UUID].appearance, avatars[client.UUID].Position, elapsedTime));
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
						recordedActions.Enqueue(new ActorMovedEvent(
							AAREvent.MoveActor, 
							avatars[client.UUID].firstName, 
							avatars[client.UUID].lastName, 
							avatars[client.UUID].appearance, 
							client.Animator.Animations.DefaultAnimation,
							avatars[client.UUID].Position, 
							elapsedTime
						));
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
					recordedActions.Enqueue(new ActorEvent(AAREvent.RemoveActor, avatars[uuid].firstName, avatars[uuid].lastName, avatars[uuid].appearance, avatars[uuid].Position, elapsedTime));
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
			log(string.Format("Tracked {0} actions", recordedActions.Count));
		}

		private void processPlayback()
		{
			//log(string.Format("playback at elapsed {0}, next event at {1}", elapsedTime, recordedActions.Peek().time));
			while( recordedActions.Count > 0 && elapsedTime > recordedActions.Peek().time){
				var e = recordedActions.Dequeue();
				processedActions.Enqueue(e);
				switch(e.type){
				case AAREvent.EventBegin:
					break;
				case AAREvent.EventEnd:
					state = AARState.stopped;
					log("Playback Completed");
					foreach(Actor a in stooges.Values)
					{
						dispatch.deleteActor(a.uuid);
					}
					stooges.Clear();
					Queue<Event> tmp = processedActions;
					processedActions = recordedActions;
					recordedActions = tmp;
					return;
				case AAREvent.AddActor:
					ActorEvent ae = (ActorEvent)e;
					log(string.Format("Adding actor {0} at {1}", ae.fullName, ae.position));
					UUID uuid = dispatch.createActor(ae.firstName, ae.lastName, new AvatarAppearance(ae.appearance), ae.position);
					stooges[ae.fullName] = new Actor(uuid,ae.firstName, ae.lastName,ae.position, ae.appearance);
					break;
				case AAREvent.MoveActor:
					ActorMovedEvent ame = (ActorMovedEvent)e;
					if(stooges.ContainsKey(ame.fullName))
					{
						log(string.Format("Moving actor {0} to {1}", ame.fullName, ame.position));
						dispatch.moveActor(stooges[ame.fullName].uuid, ame.position, ame.animation);
					}
					else
					{
						log(string.Format("Received avatar moved event for nonexistant entity {0}", ame.fullName));
					}
					break;
				case AAREvent.RemoveActor:
					ae = (ActorEvent)e;
					log(string.Format("Removing actor {0}", ae.fullName));
					dispatch.deleteActor(stooges[ae.fullName].uuid);
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
		public long time;

		public Event(AAREvent type, long time)
		{
			this.type = type;
			this.time = time;
		}
	}

	class ActorEvent : Event
	{
		public string firstName;
		public string lastName;
		public OSDMap appearance;
		public Vector3 position;
		public string fullName {get { return string.Format("{0} {1}", this.firstName, this.lastName); } }

		public ActorEvent(AAREvent eventType, string firstName, string lastName, OSDMap appearance, Vector3 position, long time) : base(eventType, time)
		{
			this.firstName = firstName;
			this.lastName = lastName;
			this.appearance = appearance;
			this.position = position;
		}
	}

	class ActorMovedEvent : ActorEvent
	{
		public OpenSim.Framework.Animation animation;

		public ActorMovedEvent(AAREvent eventType, string firstName, string lastName, OSDMap appearance, OpenSim.Framework.Animation animation, Vector3 position, long time) : 
			base(eventType, firstName, lastName, appearance, position, time)
		{
			this.animation = animation;
		}
	}

	class Actor
	{
		public UUID uuid { get; private set; }
		public string firstName { get; private set; }
		public string lastName {get; set; }
		public string fullname {get; set; }
		public Vector3 Position { get; set;}
		public OSDMap appearance { get; set; }

		public Actor(ScenePresence presence)
		{
			this.uuid = presence.UUID;
			this.firstName = presence.Firstname;
			this.lastName = presence.Lastname;
			this.Position = presence.AbsolutePosition;
			this.appearance = presence.Appearance.Pack();
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
		}

		public Actor(UUID uuid, string firstName, string lastName, Vector3 position, OSDMap appearance)
		{
			this.uuid = uuid;
			this.firstName = firstName;
			this.lastName = lastName;
			this.Position = position;
			this.appearance = appearance;
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
		}
	}
}