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
		private Dictionary<OpenMetaverse.UUID, AvatarActor> avatars = new Dictionary<OpenMetaverse.UUID, AvatarActor>();
		private Dictionary<OpenMetaverse.UUID, ObjectActor> objects = new Dictionary<UUID, ObjectActor>();
		//queue of scene events
		private Queue<AAREvent> recordedActions = new Queue<AAREvent>();
		//queue of processed scene events
		private Queue<AAREvent> processedActions = new Queue<AAREvent>();
		private Stopwatch sw = new Stopwatch();
		private long elapsedTime = 0;

		public AAR(Logger log)
		{
			this.log = log;
			this.state = AARState.stopped;
		}

		public void setDispatch(Replay dispatch)
		{
			this.dispatch = dispatch;
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
			foreach(AvatarActor a in avatars.Values)
			{
				recordedActions.Enqueue(new ActorAddedEvent(a.firstName, a.lastName, a.uuid, elapsedTime));
				recordedActions.Enqueue(new ActorAppearanceEvent(a.uuid, a.appearance, elapsedTime));
				recordedActions.Enqueue(new ActorMovedEvent(a, elapsedTime));
				recordedActions.Enqueue(new ActorAnimationEvent(a.uuid, a.animations, elapsedTime));
			}
			//FIXME: skip adding initial objects for now, the region is populated
			recordedActions.Enqueue(new EventStart(elapsedTime));
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
			dispatch.haltScripts();
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

		#region AvatarInterface

		public bool addAvatar(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				log("Duplicate Presence Detected, not adding avatar");
				return false;
			}
			else
			{
				avatars[client.UUID] = new AvatarActor(client);
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

		public bool avatarAppearanceChanged(UUID uuid, OSDMap appearance)
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

		public bool avatarPresenceChanged(ScenePresence client)
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

		public bool removeAvatar(OpenMetaverse.UUID uuid)
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

		#endregion

		#region ObjectInterface

		public bool addObject(SceneObjectPart sop)
		{
			//objects appear to be instantiated twice...
			if(! objects.ContainsKey(sop.UUID))
			{
				objects.Add(sop.UUID, new ObjectActor(sop));
				if(this.state == AARState.recording)
				{
					recordedActions.Enqueue(new ObjectAddedEvent(sop.UUID, sop.Name,sop.Shape,elapsedTime));
					recordedActions.Enqueue(new ObjectMovedEvent(sop.UUID,sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity,elapsedTime));
				}
			}
			return true;
		}

		public bool removeObject(SceneObjectPart sop)
		{
			//test first, objects are removed more than once
			if(objects.ContainsKey(sop.UUID))
			{
				objects.Remove(sop.UUID);
				if(this.state == AARState.recording)
				{
					recordedActions.Enqueue(new ObjectRemovedEvent(sop.UUID,elapsedTime));
				}
			}
			return true;
		}

		public bool updateObject(SceneObjectPart sop, bool flag)
		{
			if(objects.ContainsKey(sop.UUID))
			{
				if(objects[sop.UUID].movementChanged(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity)){
					objects[sop.UUID].updateMovement(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity);
					if(this.state == AARState.recording)
					{
						recordedActions.Enqueue(new ObjectMovedEvent(sop.UUID,sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity,elapsedTime));
					}
				}
			}
			return false;
		}

		#endregion

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
			log(string.Format("Tracked {0} actions, {1} avatars and {2} objects", recordedActions.Count, avatars.Count, objects.Count));
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
				dispatch.deleteAllObjects();
				Queue<AAREvent> tmp = processedActions;
				processedActions = recordedActions;
				recordedActions = tmp;
				dispatch.restoreScripts();
			}
		}
	}
}