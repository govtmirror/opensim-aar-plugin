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
		public UUID uuid {get; protected set; }
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 velocity;
		public Vector3 angularVelocity;

		public AARActor(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			this.uuid = uuid;
			this.position = position;
			this.rotation = rotation;
			this.velocity = velocity;
			this.angularVelocity = angularVelocity;
		}

		public bool movementChanged(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			if(this.position != position ||
			   this.rotation != rotation ||
			   this.velocity != velocity ||
			   this.angularVelocity != angularVelocity)
			{
				return true;
			}
			return false;
		}

		public void updateMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
		{
			this.position = position;
			this.rotation = rotation;
			this.velocity = velocity;
			this.angularVelocity = angularVelocity;
		}
	}

	class ObjectActor : AARActor
	{
		public string name;

		public ObjectActor(SceneObjectPart sog) : base(sog.UUID,sog.AbsolutePosition,sog.GetWorldRotation(),sog.Velocity,sog.AngularVelocity)
		{
			name = sog.Name;
		}
	}

	class AvatarActor : AARActor
	{
		public string firstName { get; private set; }
		public string lastName {get; set; }
		public string fullname {get; set; }
		public uint controlFlags { get; set;}
		public OSDMap appearance { get; set; }
		public bool isFlying;
		public OpenSim.Framework.Animation[] animations;

		public AvatarActor(ScenePresence presence) : base(presence.UUID,presence.AbsolutePosition,presence.Rotation,presence.Velocity,presence.AngularVelocity)
		{
			this.uuid = presence.UUID;
			this.firstName = presence.Firstname;
			this.lastName = presence.Lastname;
			this.controlFlags = presence.AgentControlFlags;
			this.appearance = presence.Appearance.Pack();
			this.fullname = string.Format("{0} {1}", this.firstName, this.lastName);
			this.isFlying = presence.Flying;
			this.animations = presence.Animator.Animations.ToArray();
		}

		public bool movementChanged(ScenePresence client)
		{
			if(this.movementChanged(client.AbsolutePosition,client.Rotation,client.Velocity,client.AngularVelocity) ||
				client.AgentControlFlags != controlFlags ||
				client.Flying != isFlying)
			{
				return true;
			}
			return false;
		}

		public void updateMovement(ScenePresence presence)
		{
			this.updateMovement(presence.AbsolutePosition,presence.Rotation,presence.Velocity,presence.AngularVelocity);
			controlFlags = presence.AgentControlFlags;
			isFlying = presence.Flying;
		}
	}
}

