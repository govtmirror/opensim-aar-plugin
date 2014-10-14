
using System;
using System.Reflection;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Framework.Console;
using Nini.Config;
using log4net;
using log4net.Config;

using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using Mono.Addins;

[assembly: Addin("AarModule", "0.1")]
[assembly: AddinDependency("Opensim", "0.5")]

namespace MOSES.AarModule
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "AarModule", Id = "AarModule")]
	public class AarModule : INonSharedRegionModule
	{
		#region INonSharedRegionModule
        
		private static ILog m_log;
		
		public string Name { get { return "AAR Module"; } }

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

		public void AddRegion(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Added", scene.RegionInfo.RegionName);
		}

		public void RemoveRegion(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Removed", scene.RegionInfo.RegionName);
		}

		public void RegionLoaded(Scene scene)
		{
			m_log.DebugFormat("[AAR]: Region {0} Loaded", scene.RegionInfo.RegionName);
		}
        #endregion
	}
}
