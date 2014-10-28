using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;
using System.Linq;

namespace MOSES.AAR
{
	public enum AARState
	{
		stopped,
		playback,
		recording,
		initiailizing,
		uninitialized,
		error
	}
	public delegate void Logger (string s);

	public class AAR
	{
		//current state of the AAR
		public AARState state { get; private set; }
		//logging delegate
		private Logger log;
		//callbacks to affec tthe scene
		private Replay dispatch;
		//tracking avatars in world
		private Dictionary<OpenMetaverse.UUID, AARActor> avatars = new Dictionary<OpenMetaverse.UUID, AARActor>();
		//queue of scene events
		private Queue<AAREvent> recordedActions = new Queue<AAREvent>();
		//queue of processed scene events
		private Queue<AAREvent> processedActions = new Queue<AAREvent>();
		private Stopwatch sw = new Stopwatch();
		private long elapsedTime = 0;

		public AAR(Logger log, Replay dispatch)
		{
			this.dispatch = dispatch;
			this.log = log;
			this.state = AARState.uninitialized;
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
			foreach(AARActor a in avatars.Values)
			{
				recordedActions.Enqueue(new ActorAddedEvent(a.firstName, a.lastName, a.uuid, elapsedTime));
				recordedActions.Enqueue(new ActorAppearanceEvent(a.uuid, a.appearance, elapsedTime));
				recordedActions.Enqueue(new ActorMovedEvent(a, elapsedTime));
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
				avatars[client.UUID] = new AARActor(client);
				log(string.Format("New Presence: {0} , tracking {1} Actors", this.avatars[client.UUID].firstName, this.avatars.Count));
				if(this.state == AARState.recording)
				{
					recordedActions.Enqueue(new ActorAddedEvent(avatars[client.UUID].firstName, avatars[client.UUID].lastName, client.UUID, elapsedTime));
					recordedActions.Enqueue(new ActorAppearanceEvent(client.UUID, avatars[client.UUID].appearance,elapsedTime));
					recordedActions.Enqueue(new ActorMovedEvent(avatars[client.UUID], elapsedTime));
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID, avatars[client.UUID].animations, elapsedTime));
				}
				return true;
			}
		}

		public bool actorAppearanceChanged(UUID uuid, OSDMap appearance)
		{
			if(this.avatars.ContainsKey(uuid))
			{
				if(this.state == AARState.recording)
				{
					recordedActions.Enqueue(new ActorAppearanceEvent(uuid, appearance,elapsedTime));
					return true;
				}
			}
			return false;
		}

		public bool actorPresenceChanged(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				if(state != AARState.recording)
				{
					return false;
				}
				//determine what has changed about the avatar
				//Position/Control flags
				if(avatars[client.UUID].movementChanged(client))
				{
					recordedActions.Enqueue(new ActorMovedEvent(client, elapsedTime));
					avatars[client.UUID].updateMovement(client);
				}

				//animation update
				OpenSim.Framework.Animation[] anims = client.Animator.Animations.ToArray();
				if( ! anims.SequenceEqual(avatars[client.UUID].animations))
				{
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID,anims, elapsedTime));
					avatars[client.UUID].animations = anims;
				}

				//client.Animator.Animations.ToArray;
				//client.Appearance; //not really, we have a separate signal for appeatance changed

				//client.GetAttachments;
				////client.GetWorldRotation;
				//client.IsSatOnObject;
				//client.Lookat;

				//client.SitGround;

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
				Queue<AAREvent> tmp = processedActions;
				processedActions = recordedActions;
				recordedActions = tmp;
			}
		}
	}
}