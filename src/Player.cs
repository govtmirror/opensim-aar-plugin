using System;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.ScriptEngine.Shared.Api;
using OpenSim.Region.ScriptEngine.XEngine;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using OpenMetaverse.StructuredData;
using OpenSim.Region.OptionalModules.World.NPC;
using System.Diagnostics;

namespace MOSES.AAR
{
	public class Player
	{
		private AARLog log;
		private IDialogModule dialog;
		private Scene m_scene;
		private SceneObjectGroup aarBox;
		private bool isPlaying = false;
		private bool scriptsHalted = false;
		private Queue<AAREvent> recordedActions;
		private Queue<AAREvent> processedActions = new Queue<AAREvent>();

		private Dictionary<UUID,ScenePresence> actors = new Dictionary<UUID, ScenePresence>();
		private Dictionary<UUID,SceneObjectGroup> objects = new Dictionary<UUID, SceneObjectGroup>();
		private Stopwatch sw = new Stopwatch();

		private XEngine xEngine;
		private NPCModule npc;

		public Player (AARLog log)
		{
			this.log = log;
		}

		public void printStatus()
		{
			string msg = "Recorder class ";
			if(isPlaying)
			{
				msg += "is playing";
			}
			else
			{
				msg += "is standing by";
			}
			log(msg);
		}

		public void initialize(Scene scene)
		{
			scene.EventManager.OnFrame 					+= onFrame;

			m_scene = scene;
			dialog =  m_scene.RequestModuleInterface<IDialogModule>();

			/* lookup xEngine */
			IScriptModule scriptModule = null;
			foreach (IScriptModule sm in scene.RequestModuleInterfaces<IScriptModule>())
			{
				if (sm.ScriptEngineName == scene.DefaultScriptEngine)
					scriptModule = sm;
				else if (scriptModule == null)
					scriptModule = sm;
			}
			xEngine = (XEngine)scriptModule;

			/* lookup NPC module */
			npc = (NPCModule)m_scene.RequestModuleInterface<INPCModule>();
		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			scene.AddCommand("Aar", regionModule,"aar load","load [id]","load a session by id",loadSession);
			scene.AddCommand("Aar", regionModule,"aar unload", "unload", "halt playback and unload a session",unloadSession);
			scene.AddCommand("Aar", regionModule,"aar list","list","list recorded sessions", listSessions);
			scene.AddCommand("Aar", regionModule,"aar play","play","begin playback of a loaded session", beginPlayback);
			scene.AddCommand("Aar", regionModule,"aar pause","pause","pause the playback of a loaded session", pausePlayback);
			scene.AddCommand("Aar", regionModule,"aar purge","purge","delete all contents of the aar storage prim", purgeBox);
		}
		public void deinitialize(Scene scene)
		{
			scene.EventManager.OnFrame 					-= onFrame;
		}

		public void onFrame()
		{
			if(isPlaying)
			{
				try
				{
					while( recordedActions.Count > 0 && sw.ElapsedMilliseconds > recordedActions.Peek().time)
					{
						AAREvent e = recordedActions.Dequeue();
						processedActions.Enqueue(e);

						dispatchEvent(e);
					}
					return;
				}
				catch(Exception e)
				{
					/*string msg = "Error processing: ";
					if(recordedActions == null)
					{
						msg += "recordedActions is null - ";
					}
					else if(recordedActions.Peek() == null)
					{
						msg += "recordedActions.Peek() is null - ";
					}
					else
					{
						msg += string.Format("recordedActions.Count: {0}", recordedActions.Count);
					}
					if(sw == null)
					{
						msg += "stopwatch is null - ";
					}
					else
					{
						msg += string.Format("Stopwatch elapsed millisecionds: {0}", sw.ElapsedMilliseconds);
					}*/

					isPlaying = false;
					log(e.Message + ": " + e.StackTrace);
					//log(msg + e.StackTrace);
				}
			}
		}

		private void dispatchEvent(AAREvent e)
		{
			if(e is EventStart)
			{
				log("AAR Event Playback Start");
			}
			else if(e is EventEnd)
			{
				log("AAR Event Playback Completed");
				pausePlayback();
			}
			else if(e is ObjectAddedEvent)
			{
				ObjectAddedEvent oe = (ObjectAddedEvent)e;
				createObjectPart(oe.uuid,oe.name);
			}
			else if(e is ObjectMovedEvent)
			{
				ObjectMovedEvent oe = (ObjectMovedEvent)e;
				moveObjectPart(oe.uuid,oe.position,oe.rotation,oe.velocity,oe.angularVelocity);
			}
			else if(e is ObjectRemovedEvent)
			{
				ObjectRemovedEvent oe = (ObjectRemovedEvent)e;
				deleteObject(oe.uuid);
			}
			else if(e is ActorAddedEvent)
			{
				ActorAddedEvent ae = (ActorAddedEvent)e;
				createActor(ae.uuid,ae.firstName,ae.lastName,ae.notecard);
			}
			else if(e is ActorRemovedEvent)
			{
				ActorRemovedEvent ae = (ActorRemovedEvent)e;
				deleteActor(ae.uuid);
			}
			else if(e is ActorAppearanceEvent)
			{
				ActorAppearanceEvent ae = (ActorAppearanceEvent)e;
				changeAppearance(ae.uuid,ae.notecard);
			}
			else if(e is ActorAnimationEvent)
			{
				ActorAnimationEvent ae = (ActorAnimationEvent)e;
				animateActor(ae.uuid,ae.animations);
			}
			else if(e is ActorMovedEvent)
			{
				ActorMovedEvent ae = (ActorMovedEvent)e;
				moveActor(ae.uuid,ae.position,ae.rotation,ae.velocity,ae.isFlying,ae.controlFlags);
			}
			else if(e is ChatEvent)
			{
				ChatEvent ce = (ChatEvent)e;
				chat(ce.sender,ce.message,ce.msgType,ce.channel);
			}
			else
			{
				log(string.Format("Invalid event {0}", e.GetType().Name));
			}
		}

		#region commands

		public void purgeBox(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			foreach(UUID id in aarBox.RootPart.Inventory.GetInventoryList())
			{
				aarBox.RootPart.Inventory.RemoveInventoryItem(id);
			}
		}

		public void beginPlayback(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			sw.Restart();
			isPlaying = true;
		}

		public void pausePlayback(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			pausePlayback();
		}
		private void pausePlayback()
		{

		}

		public void listSessions(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			Dictionary<string,int> sessions = getSessions();
			foreach(string id in sessions.Keys)
			{
				log(string.Format("session: {0}", id));
			}
		}

		public void loadSession(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			if(args.Length <= 2)
			{
				log("Error loading session, Usage: aar load <id>");
				return;
			}
			string sessionId = args[2];
			Dictionary<string,int> sessions = getSessions();
			if(! sessions.ContainsKey(sessionId))
			{
				log("Error loading session: session does not exist");
				return;
			}

			dialog.SendGeneralAlert("AAR Module: Halting scripts in preparation for Playback");
			haltScripts();
			dialog.SendGeneralAlert("AAR Module: Initializing scene");
			loadSession(sessionId, sessions[sessionId]);
			dialog.SendGeneralAlert("AAR Module: Scene ready for playback");
		}

		public void unloadSession(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			isPlaying = false;
			recordedActions = null;
			dialog.SendGeneralAlert("AAR Module: Clearing Scene");
			//TODO delete managed objects and NPC characters
			deleteAllActors();
			dialog.SendGeneralAlert("AAR Module: Resuming normal region script operation");
			resumeScripts();
			dialog.SendGeneralAlert("AAR Module: Unload complete");
		}

		#endregion

		#region AvatarDispatch

		private void chat(UUID agent, string msg, ChatTypeEnum msgType, int channel)
		{
			UUID speaker = actors[agent].UUID;
			switch(msgType)
			{
			case ChatTypeEnum.Say:
				npc.Say(speaker,m_scene,msg,channel);
				break;
			case ChatTypeEnum.Shout:
				npc.Shout(speaker,m_scene,msg,channel);
				break;
			case ChatTypeEnum.Whisper:
				npc.Whisper(speaker,m_scene,msg,channel);
				break;
			}
		}

		private AvatarAppearance loadAvatarAppearance(string notecard)
		{
			OSSL_Api osslApi = new OSSL_Api();
			osslApi.Initialize(xEngine, aarBox.RootPart, null, null);
			string data = osslApi.osGetNotecard(notecard);
			OSDMap appearanceOsd = (OSDMap)OSDParser.DeserializeLLSDXml(data);
			AvatarAppearance appearance = new AvatarAppearance();
			appearance.Unpack(appearanceOsd);
			return appearance;
		}

		private void createActor(UUID originalUuid, string firstName, string lastName, string appearanceNotecard)
		{
			if(actors.ContainsKey(originalUuid))
			{
				return;
			}
			if(npc == null)
			{
				npc = (NPCModule)m_scene.RequestModuleInterface<INPCModule>();
			}

			//read appearance out of notecard
			AvatarAppearance appearance = loadAvatarAppearance(appearanceNotecard);

			//create npc
			UUID uuid = npc.CreateNPC(firstName,lastName,Vector3.Zero,UUID.Zero,false,m_scene, appearance);
			ScenePresence presence;
			m_scene.TryGetScenePresence(uuid, out presence);
			actors[originalUuid] = presence;
		}

		private void moveActor(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, bool isFlying, uint control)
		{
			actors[uuid].AgentControlFlags = control;
			actors[uuid].AbsolutePosition = position;
			actors[uuid].Velocity = velocity;
			actors[uuid].Rotation = rotation;
			actors[uuid].Flying = isFlying;
		}

		private void animateActor(UUID uuid, OpenSim.Framework.Animation[] animations)
		{
			actors[uuid].Animator.ResetAnimations();
			foreach(OpenSim.Framework.Animation a in animations)
			{
				actors[uuid].Animator.AddAnimation(a.AnimID,a.ObjectID);
			}
		}

		private void changeAppearance(UUID uuid, string appearanceNotecard){
			AvatarAppearance appearance = loadAvatarAppearance(appearanceNotecard);
			npc.SetNPCAppearance(actors[uuid].UUID, appearance, m_scene);
		}

		private void deleteActor(UUID uuid)
		{
			npc.DeleteNPC(actors[uuid].UUID, m_scene);
			actors.Remove(uuid);
		}

		private void deleteAllActors()
		{
			foreach(ScenePresence sp in actors.Values)
			{
				npc.DeleteNPC(sp.UUID, m_scene);
			}
			actors.Clear();
		}

		#endregion

		#region ObjectDispatch

		private void createObjectPart(UUID uuid, string name)
		{
			TaskInventoryItem item = aarBox.RootPart.Inventory.GetInventoryItem(uuid.ToString());

			List<SceneObjectGroup> rezzedObjects = m_scene.RezObject(aarBox.RootPart,item,Vector3.Zero,null,Vector3.Zero,0);

			if(rezzedObjects == null)
			{
				log(string.Format("Rezzing of object {0} failed", name));
				return;
			}

			if(objects == null)
			{
				log(string.Format("objects dictionary is null"));
				return;
			}

			foreach(SceneObjectGroup sog in rezzedObjects)
			{
				objects[sog.UUID] = sog;
			}

			//SceneObjectGroup sog = new SceneObjectGroup(UUID.Zero,Vector3.Zero,shape);

			//m_scene.AddNewSceneObject(sog, false);
			//objects[uuid] = sog;
		}

		private void moveObjectPart(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			if(!objects.ContainsKey(uuid))
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
			objects[uuid].AbsolutePosition = position;//   MoveToTarget(position,0);
			objects[uuid].UpdateGroupRotationR(rotation);//UpdateRotation(rotation);
			objects[uuid].Velocity = velocity;
			objects[uuid].ScheduleGroupForTerseUpdate();
		}

		private void deleteObject(UUID uuid)
		{
			m_scene.DeleteSceneObject(objects[uuid],false);
			objects.Remove(uuid);

		}

		private void deleteAllObjects()
		{
			foreach(SceneObjectGroup stick in objects.Values)
			{
				m_scene.DeleteSceneObject(stick,false);
			}
			objects.Clear();
		}

		#endregion

		private void loadSession(string sessionId, int maxPiece)
		{
			log(string.Format("loading session {0}", sessionId));
			string data = "";
			OSSL_Api osslApi = new OSSL_Api();
			osslApi.Initialize(xEngine, aarBox.RootPart, null, null);
			for(int n = 0; n <= maxPiece; n++)
			{
				string notecardName = string.Format("session-{0}-{1}", sessionId,n);
				log(string.Format("pulling notecard {0}", notecardName));
				data += osslApi.osGetNotecard(notecardName);
			}

			byte[] rawData = Convert.FromBase64String(data);
			using (MemoryStream msCompressed = new MemoryStream(rawData))
			using (GZipStream gZipStream = new GZipStream(msCompressed, CompressionMode.Decompress))
			using (MemoryStream msDecompressed = new MemoryStream())
			{
				gZipStream.CopyTo(msDecompressed);
				msDecompressed.Seek(0, SeekOrigin.Begin);
				BinaryFormatter bf = new BinaryFormatter();
				recordedActions = (Queue<AAREvent>)bf.Deserialize(msDecompressed);
			}
			log(string.Format("Loaded {0} actions", recordedActions.Count));

			while(! (recordedActions.Peek() is EventStart))
			{
				var e = recordedActions.Dequeue();
				processedActions.Enqueue(e);
				dispatchEvent(e);
			}
		}

		private Dictionary<string,int> getSessions()
		{
			Dictionary<string,int> sessions = new Dictionary<string,int>();
			foreach(TaskInventoryItem eb in aarBox.RootPart.Inventory.GetInventoryItems())
			{
				string[] parts = eb.Name.Split('-');
				if(parts[0] == "session")
				{
					string sessionId = parts[1];
					int part = Convert.ToInt32(parts[2]);
					if(sessions.ContainsKey(sessionId))
					{
						if(sessions[sessionId] < part)
						{
							sessions[sessionId] = part;
						}
					}
					else
					{
						sessions[sessionId] = part;
					}
				}
			}
			return sessions;
		}

		private void haltScripts()
		{
			if(scriptsHalted)
				return;
			scriptsHalted = true;
			EntityBase[] ents = m_scene.Entities.GetEntities();
			foreach(EntityBase eb in ents)
			{
				if (eb is SceneObjectGroup)
				{
					((SceneObjectGroup)eb).RemoveScriptInstances(false);
				}
			}
		}
		private void resumeScripts()
		{
			if(!scriptsHalted)
				return;
			scriptsHalted = false;
			dialog.SendGeneralAlert("AAR Module: Restarting scripts after playback complete");
			//this reloads scripts, it may reload all assemblies, but it works reliably
			m_scene.CreateScriptInstances();
			dialog.SendGeneralAlert("AAR Module: Region resuming normal functionality");
		}
	}
}

