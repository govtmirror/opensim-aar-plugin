using System;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;

namespace MOSES.AAR
{
	public class Player
	{
		public Player ()
		{
		}

		public void initialize(Scene scene)
		{

		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			//scene.AddCommand(regionModule,"aar playback","start playing","really start playing",startRecording);
		}
		public void deinitialize(Scene scene)
		{

		}
	}
}

