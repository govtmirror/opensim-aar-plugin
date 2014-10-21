using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;

namespace MOSES.AAR
{
	public enum AARState
	{
		stopped,
		playback,
		recording,
		initiailizing,
		error
	}
	public delegate void Logger (string s);

	public class AAR
	{
		public AARState state { get; private set; }
		private Logger log;
		private Dictionary<Guid,ScenePresence> avatars;

		public AAR(Logger log)
		{
			this.log = log;
			this.state = AARState.stopped;
			this.avatars = new Dictionary<Guid, ScenePresence>();
		}

		public bool startRecording()
		{
			this.log("log function called from inside AAR class");
			this.state = AARState.recording;
			return false;
		}

		public bool stopRecording()
		{
			this.state = AARState.stopped;
			return false;
		}

		public bool startPlaying()
		{
			return false;
		}

		public bool stopPlaying()
		{
			return false;
		}

		public bool addActor(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID.Guid))
			{
				this.log("Duplicate Presence Detected, not adding avatar");
				return false;
			}
			else
			{
				this.avatars[client.UUID.Guid] = client;
				this.log(string.Format("New Presence: {0} {1}, tracking {2} Actors", client.Firstname, client.Lastname, this.avatars.Count));
				if(!this.avatars.ContainsKey(client.UUID.Guid))
				{
					this.log("Just added new actor, but continskey fails");
				}
				return true;
			}
		}

		public bool actorMoved(ScenePresence client)
		{
			if(!this.avatars.ContainsKey(client.UUID.Guid))
			{
				this.log("Received client moved for untracked avatar");
				return false;
			}
			if(client.AbsolutePosition != this.avatars[client.UUID.Guid].AbsolutePosition)
			{
				this.log(string.Format("Avatar {0} {1} moved to {2}", client.Firstname, client.Lastname, client.AbsolutePosition));
				this.avatars[client.UUID.Guid] = client;
				return true;
			}
			return false;
		}

		public bool removeActor(OpenMetaverse.UUID uuid)
		{
			if(this.avatars.ContainsKey(uuid.Guid))
			{
				this.avatars.Remove(uuid.Guid);
				return true;
			}
			return false;
		}

		public bool addObject()
		{
			return false;
		}

		public bool removeObject()
		{
			return false;
		}
	}
}