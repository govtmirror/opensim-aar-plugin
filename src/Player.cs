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

namespace MOSES.AAR
{
	public class Player
	{
		private AARLog log;
		private IDialogModule dialog;
		private Scene m_scene;
		private SceneObjectGroup aarBox;
		private bool isPlaying = false;
		private Queue<AAREvent> recordedActions;
		private XEngine xEngine;

		public Player (AARLog log, SceneObjectGroup container)
		{
			this.log = log;
			aarBox = container;
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
		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			scene.AddCommand("Aar", regionModule,"aar load","load [id]","load a session by id",loadSession);
			scene.AddCommand("Aar", regionModule,"aar unload", "unload", "halt playback and unload a session",unloadSession);
			scene.AddCommand("Aar", regionModule,"aar list","list","list recorded sessions", listSessions);

		}
		public void deinitialize(Scene scene)
		{
			scene.EventManager.OnFrame 					-= onFrame;
		}

		public void onFrame()
		{
			if(!isPlaying)
			{
				return;
			}
		}

		public void listSessions(string module, string[] args)
		{
			Dictionary<UUID,int> sessions = getSessions();
			foreach(UUID id in sessions.Keys)
			{
				log(string.Format("session: {0}", id));
			}
		}

		public void loadSession(string module, string[] args)
		{
			if(args.Length <= 2)
			{
				log("Error loading session, Usage: aar load <id>");
				return;
			}
			UUID sessionId;
			if(!UUID.TryParse(args[2],out sessionId))
			{
				log("Error loading session, malformed uuid session key");
				return;
			}
			Dictionary<UUID,int> sessions = getSessions();
			if(! sessions.ContainsKey(sessionId))
			{
				log("Error loading session: session does not exist");
				return;
			}

			//haltScripts();
			loadSession(sessionId, sessions[sessionId]);
		}

		public void unloadSession(string module, string[] args)
		{
			isPlaying = false;
			recordedActions = null;
			//TODO delete managed objects and NPC characters
			resumeScripts();
		}

		private void loadSession(UUID sessionId, int maxPiece)
		{
			string data = "";
			OSSL_Api osslApi = new OSSL_Api();
			osslApi.Initialize(xEngine, aarBox.RootPart, null, null);
			for(int n = 0; n <= maxPiece; n++)
			{
				string notecardName = string.Format("session:{0}:{1}", sessionId,n);
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
		}

		private Dictionary<UUID,int> getSessions()
		{
			Dictionary<UUID,int> sessions = new Dictionary<UUID,int>();
			foreach(TaskInventoryItem eb in aarBox.RootPart.Inventory.GetInventoryItems())
			{
				string[] parts = eb.Name.Split(':');
				if(parts[0] == "session")
				{
					UUID sessionId = new UUID(parts[1]);
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
			dialog.SendGeneralAlert("AAR Module: Halting scripts in preparation for Playback");
			EntityBase[] ents = m_scene.Entities.GetEntities();
			foreach(EntityBase eb in ents)
			{
				if (eb is SceneObjectGroup)
				{
					((SceneObjectGroup)eb).RemoveScriptInstances(false);
					//unloads all script assemblies, very slow
					//((SceneObjectGroup)eb).RemoveScriptInstances(false);
				}
			}
		}
		private void resumeScripts()
		{
			dialog.SendGeneralAlert("AAR Module: Restarting scripts after playback complete");
			//this reloads scripts, it may reload all assemblies, but it works reliably
			m_scene.CreateScriptInstances();
			dialog.SendGeneralAlert("AAR Module: Region resuming normal functionality");
		}
	}
}

