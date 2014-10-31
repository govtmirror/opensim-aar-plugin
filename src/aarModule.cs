
using System;
using System.Reflection;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Framework;
using Nini.Config;
using log4net;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.OptionalModules.World.NPC;

using Mono.Addins;
using OpenMetaverse;
using System.Collections.Generic;
using OpenMetaverse.StructuredData;

[assembly: Addin("AARModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace MOSES.AAR
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "AARModule")]
	public class AARModule : INonSharedRegionModule
	{

		private Scene m_scene;
		private static ILog m_log;
		private AAR aar;
		private Replay dispatch;

		#region RegionModule

		public string Name { get { return "AARModule"; } }

		public Type ReplaceableInterface { get { return null; } }

		public AARModule()
		{
			this.aar = new AAR(delegate(string s){m_log.DebugFormat("[AAR]: {0}", s);});
		}

		public void Initialise(IConfigSource source)
		{
            m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			m_log.DebugFormat("[AAR]: Initialized Module");
			//OpenSim.Region.OptionalModules.World.NPC.NPCModule mod;
			//AvatarAppearance m;

		}

		public void Close()
		{
			m_log.DebugFormat("[AAR]: Closed Module");
		}

		private readonly Commander m_commander = new Commander("aar");
		public void AddRegion(Scene scene)
		{
			m_scene = scene;


			/* Actor Events */
			m_scene.EventManager.OnNewPresence 						+= OnAddActor;
			m_scene.EventManager.OnRemovePresence 					+= OnRemoveActor;
			m_scene.EventManager.OnAvatarAppearanceChange 			+= OnAvatarAppearanceChange;
			m_scene.EventManager.OnScenePresenceUpdated 			+= OnScenePresenceUpdated;
			m_scene.EventManager.OnMakeChildAgent 					+= OnRemoveActor;
			m_scene.EventManager.OnMakeRootAgent 					+= OnAddActor;

			/* Object Events */
			m_scene.EventManager.OnObjectAddedToScene				+= OnObjectAddedToScene;
			m_scene.EventManager.OnSceneObjectLoaded				+= OnObjectAddedToScene;
			m_scene.EventManager.OnObjectBeingRemovedFromScene		+= OnObjectBeingRemovedFromScene;
			m_scene.EventManager.OnSceneObjectPartUpdated			+= OnSceneObjectPartUpdated;


			m_scene.EventManager.OnPluginConsole 					+= OnPluginConsole;
			m_scene.EventManager.OnRegionHeartbeatEnd 				+= OnFrame;

			m_log.DebugFormat("[AAR]: Region {0} Added", scene.RegionInfo.RegionName);
			dispatch = new Replay(scene);
			dispatch.npc.AddRegion(scene);
			aar.setDispatch(dispatch);
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

		public void RemoveRegion(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Removed", scene.RegionInfo.RegionName);

			/* Actor Events */
			m_scene.EventManager.OnNewPresence 						-= OnAddActor;
			m_scene.EventManager.OnRemovePresence 					-= OnRemoveActor;
			m_scene.EventManager.OnAvatarAppearanceChange 			-= OnAvatarAppearanceChange;
			m_scene.EventManager.OnScenePresenceUpdated 			-= OnScenePresenceUpdated;
			m_scene.EventManager.OnMakeChildAgent 					-= OnRemoveActor;
			m_scene.EventManager.OnMakeRootAgent 					-= OnAddActor;

			/* Object Events */
			m_scene.EventManager.OnObjectAddedToScene             	-= OnObjectAddedToScene;
			m_scene.EventManager.OnSceneObjectLoaded              	-= OnObjectAddedToScene;
			m_scene.EventManager.OnObjectBeingRemovedFromScene    	-= OnObjectBeingRemovedFromScene;
			m_scene.EventManager.OnSceneObjectPartUpdated         	-= OnSceneObjectPartUpdated;

			m_scene.EventManager.OnPluginConsole 					-= OnPluginConsole;
			m_scene.EventManager.OnRegionHeartbeatEnd 				-= OnFrame;

			m_scene.UnregisterModuleCommander(m_commander.Name);
			dispatch.npc.RemoveRegion(scene);
		}

		public void RegionLoaded(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Loaded", scene.RegionInfo.RegionName);
			initCommander();
			dispatch.npc.RegionLoaded(scene);
		}

		#endregion

		#region onObject

		private void OnObjectAddedToScene(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				aar.addObject(part);
			}
		}

		private void OnObjectBeingRemovedFromScene(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				aar.removeObject(part);
			}
		}

		private void OnSceneObjectPartUpdated(SceneObjectPart sop, bool flag)
		{
			aar.updateObject(sop, flag);
		}

		#endregion

		private void OnFrame(Scene s)
		{
			aar.tick();
		}

		private void OnPluginConsole(string[] args)
		{
			if (args[0] == "aar")
			{
				string[] tmpArgs = new string[args.Length - 2];
				int i;
				for (i = 2; i < args.Length; i++)
					tmpArgs[i - 2] = args[i];

				m_commander.ProcessConsoleCommand(args[1], tmpArgs);
			}
		}

		private void OnAddActor(ScenePresence presence)
		{
			this.aar.addAvatar(presence);
		}

		private void OnRemoveActor(OpenMetaverse.UUID uuid)
		{
			this.aar.removeAvatar(uuid);
		}
		private void OnRemoveActor(ScenePresence presence)
		{
			OnRemoveActor(presence.UUID);
		}

		private void OnAvatarAppearanceChange(ScenePresence presence)
		{
			aar.avatarAppearanceChanged(presence.UUID,presence.Appearance.Pack());
		}

		private void  OnScenePresenceUpdated(ScenePresence presence)
		{
			this.aar.avatarPresenceChanged(presence);
		}


		#region Console

		private void statusAction(Object[] args)
		{
			//MainConsole.Instance.OutputFormat("aar test print using MainConsole Output format");
			aar.printActionList();

		}

		private void recordAction(Object[] args)
		{
			this.aar.startRecording();
		}

		private void stopAction(Object[] args)
		{
			if(this.aar.state == AARState.error)
			{
				m_log.Debug("[AAR]: AAR stop called, but AAR in error");
			}
			else if(this.aar.state == AARState.playback)
			{
				this.aar.stopPlaying();
			}
			else if(this.aar.state == AARState.recording)
			{
				this.aar.stopRecording();
			}
		}

		private void playAction(Object[] args)
		{
			this.aar.startPlaying();
		}

		private void initCommander(){
			Command statusCmd = new Command("status", CommandIntentions.COMMAND_NON_HAZARDOUS, statusAction,
			                              "print aar module status");
			m_commander.RegisterCommand("status",statusCmd);

			Command recordCmd = new Command("record", CommandIntentions.COMMAND_HAZARDOUS, recordAction,
			                                "begin recording the scene");
			m_commander.RegisterCommand("record", recordCmd);

			Command stopCmd = new Command("stop", CommandIntentions.COMMAND_HAZARDOUS, stopAction,
			                                "halt recording or playback");
			m_commander.RegisterCommand("stop", stopCmd);

			Command playCmd = new Command("play", CommandIntentions.COMMAND_HAZARDOUS, playAction,
			                              "play back recorded events");
			m_commander.RegisterCommand("play", playCmd);

			m_scene.RegisterModuleCommander(m_commander);
			m_log.Debug("[AAR]: commander initialized and registered");
		}
		#endregion
	}

	/*
	 * This class is an interface to the AAR replay code, which affects the scene in question during playback
	 */
	public class Replay
	{
		private Dictionary<UUID,ScenePresence> stooges = new Dictionary<UUID, ScenePresence>();
		private Dictionary<UUID,SceneObjectGroup> sticks = new Dictionary<UUID, SceneObjectGroup>();
		public AARNPCModule npc;
		private Scene m_scene;

		public Replay(Scene scene)
		{
			npc = new AARNPCModule();
			m_scene = scene;
		}

		public void haltScripts()
		{
			m_scene.ScriptsEnabled = false;
		}

		public void restoreScripts()
		{
			m_scene.ScriptsEnabled = true;
			m_scene.StartScripts();
		}

		#region AvatarDispatch

		public void createActor(UUID originalUuid, string firstName, string lastName)
		{
			if(stooges.ContainsKey(originalUuid))
			{
				return;
			}
			UUID uuid = npc.CreateNPC(firstName,lastName,Vector3.Zero,UUID.Zero,false,m_scene, new AvatarAppearance());
			ScenePresence presence;
			m_scene.TryGetScenePresence(uuid, out presence);
			stooges[originalUuid] = presence;
		}

		public void moveActor(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, bool isFlying, uint control)
		{
			stooges[uuid].AgentControlFlags = control;
			stooges[uuid].AbsolutePosition = position;
			stooges[uuid].Velocity = velocity;
			stooges[uuid].Rotation = rotation;
			stooges[uuid].Flying = isFlying;
			//npc.MoveToTarget(uuid,m_scene,position,!isFlying,false,false);

			/*
			npc.MoveToTarget(uuid, m_scene, position,true,false,false);
			stooges[uuid].AgentControlFlags = 0;
			if(stooges[uuid].Animator.Animations.DefaultAnimation != animation)
			{
				stooges[uuid].Animator.AddAnimation(animation.AnimID,animation.ObjectID);
			}
			*/
		}

		public void animateActor(UUID uuid, OpenSim.Framework.Animation[] animations)
		{
			stooges[uuid].Animator.ResetAnimations();
			foreach(OpenSim.Framework.Animation a in animations)
			{
				stooges[uuid].Animator.AddAnimation(a.AnimID,a.ObjectID);
			}
		}

		public void changeAppearance(UUID uuid, OSDMap appearance){
			npc.SetNPCAppearance(stooges[uuid].UUID, new AvatarAppearance(appearance), m_scene);
		}

		public void deleteActor(UUID uuid)
		{
			npc.DeleteNPC(stooges[uuid].UUID, m_scene);
			stooges.Remove(uuid);
		}

		public void deleteAllActors()
		{
			foreach(ScenePresence sp in stooges.Values)
			{
				npc.DeleteNPC(sp.UUID, m_scene);
			}
			stooges.Clear();
		}

		#endregion

		#region ObjectDispatch

		public void createObject(UUID uuid, String name, PrimitiveBaseShape shape)
		{
			//SceneObjectPart sop = 
			//	new SceneObjectPart(
			//		uuid, shape, Vector3.Zero, Quaternion.Identity, Vector3.Zero);
				
			SceneObjectGroup sog = new SceneObjectGroup(UUID.Zero,Vector3.Zero,shape);
				
			m_scene.AddNewSceneObject(sog, false);
			sticks[uuid] = sog;
		}

		public void moveObject(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			if(!sticks.ContainsKey(uuid))
			{
				return;
			}
			sticks[uuid].AbsolutePosition = position;//   MoveToTarget(position,0);
			sticks[uuid].UpdateGroupRotationR(rotation);//UpdateRotation(rotation);
			sticks[uuid].Velocity = velocity;
			//sticks[uuid].AngularVelocity = angularVelocity;
			sticks[uuid].ScheduleGroupForTerseUpdate();
		}

		public void deleteObject(UUID uuid)
		{
			m_scene.DeleteSceneObject(sticks[uuid],false);
			sticks.Remove(uuid);

		}

		public void deleteAllObjects()
		{
			foreach(SceneObjectGroup stick in sticks.Values)
			{
				m_scene.DeleteSceneObject(stick,false);
			}
			sticks.Clear();
		}

		#endregion
	}

	/* Extend NPC module to force enabled, so we dont rely on the NPC opensim ini config to perform */
	public class AARNPCModule : NPCModule
	{
		new public bool Enabled { get; private set; }
		public AARNPCModule() : base()
		{
			this.Enabled = true;
		}
	}
}
