
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
		private bool enabled = false;

		#region RegionModule

		public string Name { get { return "AARModule"; } }

		public Type ReplaceableInterface { get { return null; } }

		public AARModule(){}

		public void Initialise(IConfigSource source)
		{
			IConfig config = source.Configs["AARModule"];

			enabled = (config != null && config.GetBoolean("Enabled", false));
			if(enabled)
			{
            	m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			}
		}

		public void Close(){}
		public void AddRegion(Scene scene)
		{
			if(enabled)
			{
				m_scene = scene;
			}
		}

		public void RemoveRegion(Scene scene)
		{
			if( aar != null)
				aar.cleanup();
		}

		public void RegionLoaded(Scene scene)
		{
			if(enabled)
			{
				aar = new AAR(m_scene, this, delegate(string s){m_log.DebugFormat("[AAR]: {0}", s);});
			}
		}

		#endregion
	}
}
