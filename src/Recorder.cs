using System;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenSim.Region.Framework.Interfaces;

namespace MOSES.AAR
{
	public class Recorder
	{

		private Dictionary<OpenMetaverse.UUID, AvatarActor> avatars = new Dictionary<OpenMetaverse.UUID, AvatarActor>();
		private Dictionary<OpenMetaverse.UUID, ObjectActor> objects = new Dictionary<UUID, ObjectActor>();

		private Queue<AAREvent> recordedActions = new Queue<AAREvent>();
		private Stopwatch sw = new Stopwatch();
		private AARLog log;
		private bool isRecording = false;

		public Recorder (AARLog log)
		{
			this.log = log;
		}

		public void initialize(Scene scene)
		{
			/* Actor Events */
			scene.EventManager.OnNewPresence 					+= onAddAvatar;
			scene.EventManager.OnRemovePresence 				+= onRemoveAvatar;
			scene.EventManager.OnAvatarAppearanceChange 		+= onAvatarAppearanceChanged;
			scene.EventManager.OnScenePresenceUpdated 			+= onAvatarPresenceChanged;
			scene.EventManager.OnMakeChildAgent 				+= onRemoveAvatar;
			scene.EventManager.OnMakeRootAgent 					+= onAddAvatar;

			/* Object Events */
			scene.EventManager.OnObjectAddedToScene				+= onAddObject;
			scene.EventManager.OnSceneObjectLoaded				+= onAddObject;
			scene.EventManager.OnObjectBeingRemovedFromScene	+= onRemoveObject;
			scene.EventManager.OnSceneObjectPartUpdated			+= onUpdateObject;
		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			scene.AddCommand(regionModule,"aar record","start recording","really start recording",startRecording);
		}

		/*
		 * 
		 *   

            Scene.EventManager.OnRegionStarted                  += OnRegionStarted;
            Scene.EventManager.OnTerrainTainted                 += OnTerrainTainted;

            Scene.EventManager.OnNewScript                      += OnLocalNewScript;
            Scene.EventManager.OnUpdateScript                   += OnLocalUpdateScript;
            Scene.EventManager.OnScriptReset                    += OnLocalScriptReset;
            Scene.EventManager.OnChatBroadcast                  += OnLocalChatBroadcast;
            Scene.EventManager.OnChatFromClient                 += OnLocalChatFromClient;
            Scene.EventManager.OnChatFromWorld                  += OnLocalChatFromWorld;
            Scene.EventManager.OnAttach                         += OnLocalAttach;
            Scene.EventManager.OnObjectGrab                     += OnLocalGrabObject;
            Scene.EventManager.OnObjectGrabbing                 += OnLocalObjectGrabbing;
            Scene.EventManager.OnObjectDeGrab                   += OnLocalDeGrabObject;
            Scene.EventManager.OnScriptColliderStart            += OnLocalScriptCollidingStart;
            Scene.EventManager.OnScriptColliding                += OnLocalScriptColliding;
            Scene.EventManager.OnScriptCollidingEnd             += OnLocalScriptCollidingEnd;
            Scene.EventManager.OnScriptLandColliderStart        += OnLocalScriptLandCollidingStart;
            Scene.EventManager.OnScriptLandColliding            += OnLocalScriptLandColliding;
            Scene.EventManager.OnScriptLandColliderEnd          += OnLocalScriptLandCollidingEnd;
		 * 
OnShutdown/OnSceneShuttingDown	-region quitting
OnSetRootAgentScene	???
OnObjectGrab[bing]	-somebody grabbed something
OnObjectDeGrab
OnSceneGroupMove
OnSceneGroubGrab
OnSceneGroupSpin[Start]
OnLandObjectAdded	???
OnLandObjectRemoved
OnCrossAgentToNewRegion ???
OnClientClosed
OnScriptChangedEvent	-something scripty changing
OnScriptMovingStartEvent ??? TODO?
OnScriptMovingEndEvent ??? TODO?
OnMakeChildAgent
OnMAkeRootAgent
OnSaveNewWindlightProfile[Targeted]
OnIncomingSceneObject
OnAvatarKilled
OnObjectAddedToScene - [PhysicalScene]
OnObjectRemovedFromScene
OnOarFileLoaded [Saved]
OnAttach
*/

		public void deinitialize(Scene scene)
		{
			/* Actor Events */
			scene.EventManager.OnNewPresence 					-= onAddAvatar;
			scene.EventManager.OnRemovePresence 				-= onRemoveAvatar;
			scene.EventManager.OnAvatarAppearanceChange 		-= onAvatarAppearanceChanged;
			scene.EventManager.OnScenePresenceUpdated 			-= onAvatarPresenceChanged;
			scene.EventManager.OnMakeChildAgent 				-= onRemoveAvatar;
			scene.EventManager.OnMakeRootAgent 					-= onAddAvatar;

			/* Object Events */
			scene.EventManager.OnObjectAddedToScene				-= onAddObject;
			scene.EventManager.OnSceneObjectLoaded				-= onAddObject;
			scene.EventManager.OnObjectBeingRemovedFromScene	-= onRemoveObject;
			scene.EventManager.OnSceneObjectPartUpdated			-= onUpdateObject;
		}
		
		#region commands
		public void startRecording(string module, string[] args)
		{
			if(isRecording)
			{
				log("Error starting: AAR is already recording");
				return;
			}
			isRecording = true;
			sw.Restart();
			recordedActions.Clear();
			foreach(AvatarActor a in avatars.Values)
			{
				recordedActions.Enqueue(new ActorAddedEvent(a.firstName, a.lastName, a.uuid, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorAppearanceEvent(a.uuid, a.appearance, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorMovedEvent(a, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorAnimationEvent(a.uuid, a.animations, sw.ElapsedMilliseconds));
			}
			//FIXME: skip adding initial objects for now, assume the region is populated
			recordedActions.Enqueue(new EventStart(sw.ElapsedMilliseconds));
			log("Record Start");
		}
		public void stopRecording(string module, string[] args)
		{
			if( !isRecording )
			{
				log("Error stopping: AAR is not recording");
				return;
			}
			recordedActions.Enqueue(new EventEnd(sw.ElapsedMilliseconds));
			isRecording = false;
			sw.Stop();
		}
		#endregion

		#region AvatarInterface

		public void onAddAvatar(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				log("Duplicate Presence Detected, not adding avatar");
			}
			else
			{
				avatars[client.UUID] = new AvatarActor(client);
				log(string.Format("New Presence: {0} , tracking {1} Actors", this.avatars[client.UUID].firstName, this.avatars.Count));
				recordedActions.Enqueue(new ActorAddedEvent(avatars[client.UUID].firstName, avatars[client.UUID].lastName, client.UUID, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorAppearanceEvent(client.UUID, avatars[client.UUID].appearance,sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorMovedEvent(avatars[client.UUID], sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorAnimationEvent(client.UUID, avatars[client.UUID].animations, sw.ElapsedMilliseconds));
			}
		}

		public void onAvatarAppearanceChanged(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				recordedActions.Enqueue(new ActorAppearanceEvent(client.UUID, client.Appearance.Pack(),sw.ElapsedMilliseconds));
			}
		}

		public void onAvatarPresenceChanged(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				//determine what has changed about the avatar
				//Position/Control flags
				if(avatars[client.UUID].movementChanged(client))
				{
					recordedActions.Enqueue(new ActorMovedEvent(client, sw.ElapsedMilliseconds));
					avatars[client.UUID].updateMovement(client);
				}

				//animation update
				OpenSim.Framework.Animation[] anims = client.Animator.Animations.ToArray();
				if( ! anims.SequenceEqual(avatars[client.UUID].animations))
				{
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID,anims, sw.ElapsedMilliseconds));
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
		}

		public void onRemoveAvatar(ScenePresence client){	onRemoveAvatar(client.UUID);	}
		public void onRemoveAvatar(OpenMetaverse.UUID uuid)
		{
			if(this.avatars.ContainsKey(uuid))
			{
				recordedActions.Enqueue(new ActorRemovedEvent(uuid, sw.ElapsedMilliseconds));
				this.avatars.Remove(uuid);
			}
		}

		#endregion

		#region ObjectInterface

		public void onAddObject(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				//objects appear to be instantiated twice...
				if(! objects.ContainsKey(part.UUID))
				{
					objects.Add(part.UUID, new ObjectActor(part));
					recordedActions.Enqueue(new ObjectAddedEvent(part.UUID, part.Name,part.Shape,sw.ElapsedMilliseconds));
					recordedActions.Enqueue(new ObjectMovedEvent(part.UUID,part.AbsolutePosition,part.GetWorldRotation(),part.Velocity,part.AngularVelocity,sw.ElapsedMilliseconds));
				}
			}
		}

		public void onRemoveObject(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				//test first, objects are removed more than once
				if(objects.ContainsKey(part.UUID))
				{
					objects.Remove(part.UUID);
					recordedActions.Enqueue(new ObjectRemovedEvent(part.UUID,sw.ElapsedMilliseconds));
				}
			}
		}

		public void onUpdateObject(SceneObjectPart sop, bool flag)
		{
			if(objects.ContainsKey(sop.UUID))
			{
				if(objects[sop.UUID].movementChanged(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity)){
					objects[sop.UUID].updateMovement(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity);
					recordedActions.Enqueue(new ObjectMovedEvent(sop.UUID,sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity,sw.ElapsedMilliseconds));
				}
			}
		}

		#endregion

	}
}

