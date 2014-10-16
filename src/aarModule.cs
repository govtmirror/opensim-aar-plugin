
using System;
using System.Reflection;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using Nini.Config;
using log4net;
using log4net.Config;

using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using Mono.Addins;

[assembly: Addin("AARModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace MOSES.AAR
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "AARModule")]
	public class AARModule : INonSharedRegionModule
	{
		#region INonSharedRegionModule
		private Scene m_scene;
		private static ILog m_log;
		
		public string Name { get { return "AARModule"; } }

		public Type ReplaceableInterface { get { return null; } }

		public void Initialise(IConfigSource source)
		{
            m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			m_log.DebugFormat("[AAR]: Initialized Module");
		}

		public void Close()
		{
			m_log.DebugFormat("[AAR]: Closed Module");
		}

		private readonly Commander m_commander = new Commander("aar");
		public void AddRegion(Scene scene)
		{
			m_scene = scene;
			m_scene.EventManager.OnPluginConsole += EventManager_OnPluginConsole;
			m_log.DebugFormat("[AAR]: Region {0} Added", scene.RegionInfo.RegionName);
		}

		public void RemoveRegion(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Removed", scene.RegionInfo.RegionName);
			m_scene.EventManager.OnPluginConsole -= EventManager_OnPluginConsole;
			m_scene.UnregisterModuleCommander(m_commander.Name);
		}

		public void RegionLoaded(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Loaded", scene.RegionInfo.RegionName);
			initCommander();
		}
        #endregion
		// test function to print to console
		private void testPrint(Object[] args)
		{
			m_log.Debug("[AAR]: testPrint is called");
			//MainConsole.Instance.OutputFormat("aar test print using MainConsole Output format");
			MainConsole.Instance.Output("aar test print using MainConsole Output");
			//m_log.DebugFormat("aar test print using debug format");
		}

		// event listener to process console commands...because our callbacks arent actually used?
		private void EventManager_OnPluginConsole(string[] args)
		{
			if (args[0] == "aar")
			{
				if (args.Length == 1)
				{
					m_commander.ProcessConsoleCommand("help", new string[0]);
					return;
				}

				string[] tmpArgs = new string[args.Length - 2];
				int i;
				for (i = 2; i < args.Length; i++)
					tmpArgs[i - 2] = args[i];

				m_commander.ProcessConsoleCommand(args[1], tmpArgs);
			}
		}

		private void initCommander(){
			Command testcmd = new Command(
				"test", 
				CommandIntentions.COMMAND_NON_HAZARDOUS, 
				testPrint, 
				"test aar print function");
			m_commander.RegisterCommand("test",testcmd);
			m_scene.RegisterModuleCommander(m_commander);
			m_log.Debug("[AAR]: commander initialized and registered");
		}
	}
}
