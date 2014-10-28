using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;
using System.Linq;

namespace MOSES.AAR
{
	class AARActor
	{
		public UUID uuid { get; private set; }
		public string firstName { get; private set; }
		public string lastName {get; set; }
		public string fullname {get; set; }
		public uint controlFlags { get; set;}
		public OSDMap appearance { get; set; }
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 velocity;
		public Vector3 angularVelocity;
		public bool isFlying;
		public OpenSim.Framework.Animation[] animations;

		public AARActor(ScenePresence presence)
		{
			this.uuid = presence.UUID;
			this.firstName = presence.Firstname;
			this.lastName = presence.Lastname;
			this.controlFlags = presence.AgentControlFlags;
			this.appearance = presence.Appearance.Pack();
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
			this.position = presence.AbsolutePosition;
			this.rotation = presence.Rotation;
			this.isFlying = presence.Flying;
			this.velocity = presence.Velocity;
			this.animations = presence.Animator.Animations.ToArray();
			this.angularVelocity = presence.AngularVelocity;
		}

		public bool movementChanged(ScenePresence client)
		{
			if( client.AgentControlFlags != controlFlags ||
			   client.AbsolutePosition != position ||
			   client.Flying != isFlying ||
			   client.Velocity != velocity ||
			   client.Rotation != rotation ||
			   client.AngularVelocity != angularVelocity)
			{
				return true;
			}
			return false;
		}

		public void updateMovement(ScenePresence presence)
		{
			controlFlags = presence.AgentControlFlags;
			position = presence.AbsolutePosition;
			isFlying = presence.Flying;
			velocity = presence.Velocity;
			rotation = presence.Rotation;
			angularVelocity = presence.AngularVelocity;
		}
	}
}

