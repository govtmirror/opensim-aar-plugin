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
		private Replay dispatch;
		//tracking avatars in world

		//queue of processed scene events
		//private Queue<AAREvent> processedActions = new Queue<AAREvent>();

		public AAR(Scene scene, IRegionModuleBase module, AARLog log)
		{
			this.log = log;

			scene.AddCommand("Aar",module,"aar status","status","Print the status of the AAR module", statusAction);
			scene.AddCommand("Aar",module,"aar stop","stop","halt recording or playback", stopAction);
			scene.AddCommand("Aar",module,"aar play","play","play back recorded events", playAction);
		}

		#region Console

		private void statusAction(string module, string[] args)
		{
			//MainConsole.Instance.OutputFormat("aar test print using MainConsole Output format");
			//aar.printActionList();
		}

		private void stopAction(string module, string[] args)
		{

		}

		private void playAction(string module, string[] args)
		{

		}
		#endregion
	}
}