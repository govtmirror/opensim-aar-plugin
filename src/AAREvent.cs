using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;


namespace MOSES.AAR
{
	[Serializable]
	abstract class AAREvent
	{
		public long time;

		public AAREvent(long time)
		{
			this.time = time;
		}
	}

	[Serializable]
	class EventStart : AAREvent
	{
		public EventStart(long time) : base(time){}
	}

	[Serializable]
	class EventEnd : AAREvent
	{
		public EventEnd(long time) : base(time){}
	}

	[Serializable]
	abstract class ObjectEvent : AAREvent
	{
		public UUID uuid {get; set;}

		public ObjectEvent(UUID uuid, long time) : base(time)
		{
			this.uuid = uuid;
		}
	}

	[Serializable]
	class ObjectAddedEvent : ObjectEvent
	{
		public string name;

		public ObjectAddedEvent(UUID uuid, String name, long time) : base(uuid, time)
		{
			this.name = name;
		}
	}

	[Serializable]
	class ObjectRemovedEvent : ObjectEvent
	{
		public ObjectRemovedEvent(UUID uuid, long time) : base(uuid, time){}
	}

	[Serializable]
	class ObjectMovedEvent : ObjectEvent
	{
		public Vector3 position{get; set;}
		public Quaternion rotation{get; set;}
		public Vector3 velocity{get; set;}
		public Vector3 angularVelocity{get; set;}

		public ObjectMovedEvent(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, long time) : base(uuid, time)
		{
			this.velocity = velocity;
			this.rotation = rotation;
			this.position = position;
			this.angularVelocity = angularVelocity;
		}
	}

	#region ActorEvents

	[Serializable]
	abstract class ActorEvent : AAREvent
	{
		public UUID uuid;

		public ActorEvent(UUID uuid, long time)
			: base(time)
		{
			this.uuid = uuid;
		}
	}

	[Serializable]
	class ActorAddedEvent : ActorEvent
	{
		public string firstName;
		public string lastName;
		public string fullName {get { return string.Format("{0} {1}", this.firstName, this.lastName); } }
		public string notecard;

		public ActorAddedEvent(string firstName, string lastName, UUID uuid, string notecard, long time)
			: base(uuid, time)
		{
			this.firstName = firstName;
			this.lastName = lastName;
			this.notecard = notecard;
		}
	}

	[Serializable]
	class ActorRemovedEvent : ActorEvent
	{
		public ActorRemovedEvent(UUID uuid, long time)
			: base(uuid, time){}
	}

	[Serializable]
	class ActorAppearanceEvent : ActorEvent
	{
		public string notecard;

		public ActorAppearanceEvent(UUID uuid, string notecard, long time)
			:base(uuid, time)
		{
			this.notecard = notecard;
		}
	}

	[Serializable]
	class ActorAnimationEvent : ActorEvent
	{
		public OpenSim.Framework.Animation[] animations;

		public ActorAnimationEvent(UUID uuid, OpenSim.Framework.Animation[] animations, long time)
			: base(uuid,time)
		{
			this.animations = animations;
		}
	}

	[Serializable]
	class ActorMovedEvent : ActorEvent
	{
		public uint controlFlags;
		public Vector3 velocity;
		public bool isFlying;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 AngularVelocity;

		public ActorMovedEvent(ScenePresence presence, long time) : 
			base(presence.UUID, time)
		{
			this.controlFlags = presence.AgentControlFlags;
			this.velocity = presence.Velocity;
			this.isFlying = presence.Flying;
			this.position = presence.AbsolutePosition;
			this.rotation = presence.Rotation;
			this.AngularVelocity = presence.AngularVelocity;
		}

		public ActorMovedEvent(AvatarActor a, long time) :
			base(a.uuid, time)
		{
			this.controlFlags = a.controlFlags;
			this.velocity = a.velocity;
			this.isFlying = a.isFlying;
			this.position = a.position;
			this.rotation = a.rotation;
			this.AngularVelocity = a.angularVelocity;
		}
	}

	#endregion
}