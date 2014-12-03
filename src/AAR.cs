using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;
using System.Linq;
using OpenSim.Region.Framework.Interfaces;

namespace MOSES.AAR
{
	public delegate void AARLog(string msg);

	public class AAR
	{
		//logging delegate
		private AARLog log;
		//callbacks to affect the scene
		private Recorder recorder;

		public static string AARBOXNAME = "AARModule Storage Box";

		//queue of processed scene events
		//private Queue<AAREvent> processedActions = new Queue<AAREvent>();

		public AAR(Scene scene, IRegionModuleBase module, AARLog log)
		{
			this.log = log;

			scene.AddCommand("Aar",module,"aar status","status","Print the status of the AAR module", statusAction);

			recorder = new Recorder(log);
			recorder.initialize(scene);
			recorder.registerCommands(module,scene);
		}

		public void cleanup()
		{
			recorder.cleanup();
		}

		#region Console

		private void statusAction(string module, string[] args)
		{
			log("AAR status command");
			recorder.printStatus();
		}
		#endregion
	}
}