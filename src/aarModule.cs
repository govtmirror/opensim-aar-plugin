
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
	public class AARModule : INonSharedRegionModule, MOSES.AAR.IDispatch
	{

		private Scene m_scene;
		private static ILog m_log;
		private AAR aar;
		private AARNPCModule npc;
		private Dictionary<UUID,ScenePresence> stooges = new Dictionary<UUID, ScenePresence>();
		//private AARListener listener;

		#region RegionModule

		public string Name { get { return "AARModule"; } }

		public Type ReplaceableInterface { get { return null; } }

		public AARModule()
		{
			this.aar = new AAR(delegate(string s){m_log.DebugFormat("[AAR]: {0}", s);}, this);
			this.npc = new AARNPCModule();
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

			m_scene.EventManager.OnPluginConsole += this.EventManager_OnPluginConsole;

			m_scene.EventManager.OnNewPresence += this.EventManager_OnAddActor;
			m_scene.EventManager.OnRemovePresence += this.EventManager_OnRemoveActor;
			m_scene.EventManager.OnAvatarAppearanceChange += this.EventManager_OnAvatarAppearanceChange;
			m_scene.EventManager.OnScenePresenceUpdated += this.EventManager_OnScenePresenceUpdated;

			m_scene.EventManager.OnMakeChildAgent += this.EventManager_OnRemoveActor;
			m_scene.EventManager.OnMakeRootAgent += this.EventManager_OnAddActor;

			m_scene.EventManager.OnRegionHeartbeatEnd += this.EventManager_OnFrame;
			m_log.DebugFormat("[AAR]: Region {0} Added", scene.RegionInfo.RegionName);
			this.npc.AddRegion(scene);
		}

		public void RemoveRegion(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Removed", scene.RegionInfo.RegionName);
			m_scene.EventManager.OnPluginConsole -= EventManager_OnPluginConsole;
			m_scene.EventManager.OnNewPresence -= this.EventManager_OnAddActor;
			m_scene.EventManager.OnRemovePresence -= this.EventManager_OnRemoveActor;
			m_scene.EventManager.OnAvatarAppearanceChange -= this.EventManager_OnAvatarAppearanceChange;
			m_scene.EventManager.OnScenePresenceUpdated -= this.EventManager_OnScenePresenceUpdated;

			m_scene.EventManager.OnMakeChildAgent += this.EventManager_OnRemoveActor;
			m_scene.EventManager.OnMakeRootAgent += this.EventManager_OnAddActor;
			m_scene.EventManager.OnRegionHeartbeatEnd += this.EventManager_OnFrame;
			m_scene.UnregisterModuleCommander(m_commander.Name);
			this.npc.RemoveRegion(scene);
		}

		public void RegionLoaded(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Loaded", scene.RegionInfo.RegionName);
			initCommander();
			this.npc.RegionLoaded(scene);
			//listener = new AARListener(m_scene,delegate(string s){m_log.DebugFormat("[AAR]: {0}", s);});
			//m_scene.AddNewAgent(listener,PresenceType.User);
			//m_scene.SubscribeToClientEvents(listener);
			//m_scene.SubscribeToClientGridEvents(listener);
		}

		#endregion

		#region EventManager

		private void EventManager_OnFrame(Scene s)
		{
			aar.tick();
		}

		private void EventManager_OnPluginConsole(string[] args)
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

		private void EventManager_OnAddActor(ScenePresence presence)
		{
			this.aar.addActor(presence);
		}

		private void EventManager_OnRemoveActor(OpenMetaverse.UUID uuid)
		{
			this.aar.removeActor(uuid);
		}
		private void EventManager_OnRemoveActor(ScenePresence presence)
		{
			this.EventManager_OnRemoveActor(presence.UUID);
		}

		private void EventManager_OnAvatarAppearanceChange(ScenePresence presence)
		{
			aar.actorAppearanceChanged(presence.UUID,presence.Appearance.Pack());
		}

		private void EventManager_OnScenePresenceUpdated(ScenePresence presence)
		{
			this.aar.actorPresenceChanged(presence);
		}

		/*
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

		#endregion

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

		#region Dispatch
		/*create a box
			string rawSogId = string.Format("00000000-0000-0000-0000-{0:X12}", 0x10);
			SceneObjectGroup sog 
				= new SceneObjectGroup(
					new SceneObjectPart(
					UUID.Zero, PrimitiveBaseShape.Default, Vector3.Zero, Quaternion.Identity, Vector3.Zero) 
					{ Name = name, UUID = new UUID(rawSogId), Scale = new Vector3(1, 1, 1) });
			if(!m_scene.AddNewSceneObject(sog, false))
			{
				m_log.Debug("[AAR]: Error adding new object to scene, playback halted");
				aar.stopPlaying();
			}
			return sog.UUID;
		*/

		/* move box
			SceneObjectGroup sog = m_scene.GetGroupByPrim(uuid);
			sog.AbsolutePosition = position;
			sog.ScheduleGroupForTerseUpdate();
		*/

		/* remove box ???
			SceneObjectGroup sog = m_scene.GetGroupByPrim(uuid);
			m_scene.RemoveGroupTarget(sog);
		*/

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
	}

	/* Extend NPC module and force enabled, so we dont rely on the NPC opensim ini config to perform */
	class AARNPCModule : NPCModule
	{
		new public bool Enabled { get; private set; }
		public AARNPCModule() : base()
		{
			this.Enabled = true;
		}
	}
}
