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

	public interface IDispatch
	{
		void createActor(UUID uuid, string firstName, string lastName);
		void moveActor(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, bool isFlying, uint controlFlags);
		void animateActor(UUID uuid, OpenSim.Framework.Animation[] animations);
		void changeAppearance(UUID uuid, OSDMap appearance);
		void deleteActor(UUID uuid);
		void deleteAllActors();
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
		private Queue<AAREvent> recordedActions = new Queue<AAREvent>();
		//queue of processed scene events
		private Queue<AAREvent> processedActions = new Queue<AAREvent>();
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
			recordedActions.Enqueue(new EventStart(elapsedTime));
			foreach(Actor a in avatars.Values)
			{
				recordedActions.Enqueue(new ActorAddedEvent(a.firstName, a.lastName, a.uuid, elapsedTime));
				recordedActions.Enqueue(new ActorAppearanceEvent(a.uuid, a.appearance, elapsedTime));
				recordedActions.Enqueue(new ActorMovedEvent(a.uuid,a.controlFlags, a.position, a.rotation, a.velocity, a.isFlying, elapsedTime));
				recordedActions.Enqueue(new ActorAnimationEvent(a.uuid, a.animations, elapsedTime));
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
			recordedActions.Enqueue(new EventEnd(elapsedTime));
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
					recordedActions.Enqueue(new ActorAddedEvent(avatars[client.UUID].firstName, avatars[client.UUID].lastName, client.UUID, elapsedTime));
					recordedActions.Enqueue(new ActorAppearanceEvent(client.UUID, avatars[client.UUID].appearance,elapsedTime));
					recordedActions.Enqueue(new ActorMovedEvent(client.UUID,avatars[client.UUID].controlFlags,avatars[client.UUID].position,avatars[client.UUID].rotation,avatars[client.UUID].velocity, avatars[client.UUID].isFlying, elapsedTime));
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID, avatars[client.UUID].animations, elapsedTime));
				}
				return true;
			}
		}

		public bool actorMoved(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				if( client.AgentControlFlags != this.avatars[client.UUID].controlFlags ||
				   client.AbsolutePosition != this.avatars[client.UUID].position ||
				   client.Flying != this.avatars[client.UUID].isFlying ||
				   client.Velocity != this.avatars[client.UUID].velocity)
				{
					this.avatars[client.UUID].controlFlags = client.AgentControlFlags;
					this.avatars[client.UUID].position = client.AbsolutePosition;
					this.avatars[client.UUID].rotation = client.Rotation;
					this.avatars[client.UUID].velocity = client.Velocity;
					this.avatars[client.UUID].isFlying = client.Flying;
					this.avatars[client.UUID].animations = client.Animator.Animations.ToArray();

					if(this.state == AARState.recording)
					{
						recordedActions.Enqueue(new ActorMovedEvent( 
							client.UUID, 
							avatars[client.UUID].controlFlags,
							avatars[client.UUID].position,
							avatars[client.UUID].rotation,
							avatars[client.UUID].velocity,
							avatars[client.UUID].isFlying,
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
					recordedActions.Enqueue(new ActorRemovedEvent(uuid, elapsedTime));
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

				e.process(dispatch, log);
			}
			if(recordedActions.Count == 0)
			{
				state = AARState.stopped;
				dispatch.deleteAllActors();
				stooges.Clear();
				Queue<AAREvent> tmp = processedActions;
				processedActions = recordedActions;
				recordedActions = tmp;
			}
		}
	}
	
	class Actor
	{
		public UUID uuid { get; private set; }
		public string firstName { get; private set; }
		public string lastName {get; set; }
		public string fullname {get; set; }
		public uint controlFlags { get; set;}
		public OSDMap appearance { get; set; }
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 velocity;
		public bool isFlying;
		public OpenSim.Framework.Animation[] animations;

		public Actor(ScenePresence presence)
		{
			this.uuid = presence.UUID;
			this.firstName = presence.Firstname;
			this.lastName = presence.Lastname;
			this.controlFlags = presence.AgentControlFlags;
			this.appearance = presence.Appearance.Pack();
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
			this.position = presence.AbsolutePosition;
			this.rotation = presence.Rotation;
			this.isFlying = presence.Flying;
			this.velocity = presence.Velocity;
			this.animations = presence.Animator.Animations.ToArray();
		}

		public Actor(UUID uuid, string firstName, string lastName, uint controlFlags, OSDMap appearance, Vector3 position, Quaternion rotation, Vector3 velocity, bool isFlying)
		{
			this.uuid = uuid;
			this.firstName = firstName;
			this.lastName = lastName;
			this.controlFlags = controlFlags;
			this.appearance = appearance;
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
			this.position = position;
			this.isFlying = isFlying;
			this.velocity = velocity;
		}
	}
}