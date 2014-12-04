using System;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;

namespace MOSES.AAR
{
	public class Player
	{
		private AARLog log;
		private IDialogModule dialog;
		private Scene m_scene;
		private SceneObjectGroup aarBox;
		private bool isPlaying = false;

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
		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			//scene.AddCommand("Aar", regionModule,"aar off","off","turn scripts off",haltScripts);
			//scene.AddCommand("Aar", regionModule,"aar on", "on", "turn scripts on",resumeScripts);
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
			foreach(UUID id in sessions.Keys)
			{
				log(string.Format("session: {0}", id));
			}
		}

		private void haltScripts(string module, string[] args)
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
		private void resumeScripts(string module, string[] args)
		{
			dialog.SendGeneralAlert("AAR Module: Restarting scripts after playback complete");
			//this reloads scripts, it may reload all assemblies, but it works reliably
			m_scene.CreateScriptInstances();
			dialog.SendGeneralAlert("AAR Module: Region resuming normal functionality");
		}
	}
}

