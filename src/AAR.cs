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
		private Player player;

		public static string AARBOXNAME = "AARModule Storage Box";

		//queue of processed scene events
		//private Queue<AAREvent> processedActions = new Queue<AAREvent>();

		public AAR(Scene scene, IRegionModuleBase module, AARLog log)
		{
			this.log = log;

			scene.AddCommand("Aar",module,"aar status","status","Print the status of the AAR module", statusAction);

			/* lookup aarBox */
			SceneObjectGroup aarBox = null;
			foreach(EntityBase ent in scene.Entities.GetEntities())
			{
				if(ent.Name == AAR.AARBOXNAME)
				{
					log("Found aarBox");
					aarBox = (SceneObjectGroup)ent;
				}
			}
			if(aarBox == null)
			{
				aarBox = new SceneObjectGroup(UUID.Zero,Vector3.Zero,PrimitiveBaseShape.CreateBox());
				aarBox.Name = AAR.AARBOXNAME;
				scene.AddNewSceneObject(aarBox, true);
				log("Created new aarBox");
			}

			recorder = new Recorder(log, aarBox);
			recorder.initialize(scene);
			recorder.registerCommands(module,scene);

			player = new Player(log, aarBox);
			player.initialize(scene);
			player.registerCommands(module,scene);
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
			player.printStatus();
		}
		#endregion
	}
}