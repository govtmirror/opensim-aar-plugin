
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
		private static ILog m_log;
		private AAR aar = null;
		private Scene m_scene;

		#region RegionModule

		public string Name { get { return "AARModule"; } }

		public Type ReplaceableInterface { get { return null; } }

		public AARModule(){}

		public void Initialise(IConfigSource source)
		{
            m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		}

		public void Close(){}
		public void AddRegion(Scene scene)
		{
			scene.AddCommand("Aar",this,"aar init","init","initialize the aar module", regionReady);
			m_scene = scene;
		}

		public void RemoveRegion(Scene scene)
		{
			if( aar != null)
				aar.cleanup();
		}

		public void RegionLoaded(Scene scene){}

		public void regionReady(string module, string[] args)
		{
			if(aar == null)
				aar = new AAR(m_scene, this, delegate(string s){m_log.DebugFormat("[AAR]: {0}", s);});
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
		public NPCModule npc;
		private Scene m_scene;
		IDialogModule dialog;

		public Replay(Scene scene)
		{
			npc = new NPCModule();
			m_scene = scene;
			dialog =  m_scene.RequestModuleInterface<IDialogModule>();
		}

		public void onPlaybackStarted()
		{
			if(dialog == null)
				dialog =  m_scene.RequestModuleInterface<IDialogModule>();
			dialog.SendGeneralAlert("AAR Module: Playback Starting");
		}

		public void onPlaybackComplete()
		{
			if(dialog == null)
				dialog =  m_scene.RequestModuleInterface<IDialogModule>();
			dialog.SendGeneralAlert("AAR Module: Playback Complete");
		}

		public void haltScripts()
		{
			if(dialog == null)
				dialog =  m_scene.RequestModuleInterface<IDialogModule>();
			dialog.SendGeneralAlert("AAR Module: Halting scripts in preparation for Playback");
			m_scene.ScriptsEnabled = false;
		}

		public void restoreScripts()
		{
			if(dialog == null)
				dialog =  m_scene.RequestModuleInterface<IDialogModule>();
			dialog.SendGeneralAlert("AAR Module: Restarting scripts after playback complete");
			m_scene.ScriptsEnabled = true;
			//m_scene.StartScripts();
			dialog.SendGeneralAlert("AAR Module: Region resuming normal functionality");
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

		public void changeAppearance(UUID uuid, string notecard){
			//npc.SetNPCAppearance(stooges[uuid].UUID, new AvatarAppearance(appearance), m_scene);
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
				//we may be attempting to move an object that we did not create
				SceneObjectGroup sog;
				m_scene.TryGetSceneObjectGroup(uuid, out sog);
				if(sog != null)
				{
					sog.AbsolutePosition = position;
					sog.UpdateGroupRotationR(rotation);
					sog.Velocity = velocity;
					sog.ScheduleGroupForTerseUpdate();
				}
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
}
