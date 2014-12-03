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

		abstract public void process(Replay dispatch, AARLog log);
	}

	[Serializable]
	class EventStart : AAREvent
	{
		public EventStart(long time) : base(time){}

		override public void process(Replay dispatch, AARLog log)
		{
			log("AAR Event Playback Start");
		}
	}

	[Serializable]
	class EventEnd : AAREvent
	{
		public EventEnd(long time) : base(time){}

		override public void process(Replay dispatch, AARLog log)
		{
			log("AAR Event Playback Completed");
		}
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
		public PrimitiveBaseShape shape;

		public ObjectAddedEvent(UUID uuid, String name, PrimitiveBaseShape shape, long time) : base(uuid, time)
		{
			this.name = name;
			this.shape = shape;
		}

		override public void process(Replay dispatch, AARLog log)
		{
			dispatch.createObject(this.uuid,this.name,this.shape);
		}
	}

	[Serializable]
	class ObjectRemovedEvent : ObjectEvent
	{

		public ObjectRemovedEvent(UUID uuid, long time) : base(uuid, time){}

		override public void process(Replay dispatch, AARLog log)
		{
			dispatch.deleteObject(this.uuid);
		}
	}

	[Serializable]
	class ObjectMovedEvent : ObjectEvent
	{
		Vector3 position{get; set;}
		Quaternion rotation{get; set;}
		Vector3 velocity{get; set;}
		Vector3 angularVelocity{get; set;}

		public ObjectMovedEvent(UUID uuid, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, long time) : base(uuid, time)
		{
			this.velocity = velocity;
			this.rotation = rotation;
			this.position = position;
			this.angularVelocity = angularVelocity;
		}

		override public void process(Replay dispatch, AARLog log)
		{
			dispatch.moveObject(this.uuid,this.position,this.rotation,this.velocity,this.angularVelocity);
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

		public ActorAddedEvent(string firstName, string lastName, UUID uuid, long time)
			: base(uuid, time)
		{
			this.firstName = firstName;
			this.lastName = lastName;
		}

		override public void process(Replay dispatch, AARLog log)
		{
			log(string.Format("Adding actor {0} as {1}", this.uuid, this.fullName));
			dispatch.createActor(this.uuid,this.firstName,this.lastName);
		}
	}

	[Serializable]
	class ActorRemovedEvent : ActorEvent
	{
		public ActorRemovedEvent(UUID uuid, long time)
			: base(uuid, time){}

		override public void process(Replay dispatch, AARLog log)
		{
			log(string.Format("Removing actor {0}", this.uuid));
			dispatch.deleteActor(this.uuid);
		}
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

		override public void process(Replay dispatch, AARLog log)
		{
			dispatch.changeAppearance(this.uuid,this.notecard);
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

		override public void process(Replay dispatch, AARLog log)
		{
			dispatch.animateActor(this.uuid,this.animations);
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

		override public void process(Replay dispatch, AARLog log)
		{
			log(string.Format("Moving actor {0} to {1}", this.uuid, this.position));
			dispatch.moveActor(this.uuid, this.position, this.rotation, this.velocity, this.isFlying, this.controlFlags);
		}
	}

	#endregion
}