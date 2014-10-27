using System;
using System.Collections.Generic;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using System.Diagnostics;
using OpenSim.Framework;
using OpenMetaverse.StructuredData;


namespace MOSES.AAR
{
	abstract class AAREvent
	{
		public long time;

		public AAREvent(long time)
		{
			this.time = time;
		}

		abstract public void process(IDispatch dispatch, Logger log);
	}

	class EventStart : AAREvent
	{
		public EventStart(long time) : base(time){}

		override public void process(IDispatch dispatch, Logger log)
		{
			log("AAR Event Playback Start");
		}
	}

	class EventEnd : AAREvent
	{
		public EventEnd(long time) : base(time){}

		override public void process(IDispatch dispatch, Logger log)
		{
			log("AAR Event Playback Completed");
		}
	}

	abstract class ActorEvent : AAREvent
	{
		public UUID uuid;

		public ActorEvent(UUID uuid, long time)
			: base(time)
		{
			this.uuid = uuid;
		}
	}

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

		override public void process(IDispatch dispatch, Logger log)
		{
			log(string.Format("Adding actor {0} as {1}", this.uuid, this.fullName));
			dispatch.createActor(this.uuid,this.firstName,this.lastName);
		}
	}

	class ActorRemovedEvent : ActorEvent
	{
		public ActorRemovedEvent(UUID uuid, long time)
			: base(uuid, time){}

		override public void process(IDispatch dispatch, Logger log)
		{
			log(string.Format("Removing actor {0}", this.uuid));
			dispatch.deleteActor(this.uuid);
		}
	}

	class ActorAppearanceEvent : ActorEvent
	{
		public OSDMap appearance;

		public ActorAppearanceEvent(UUID uuid, OSDMap appearance, long time)
			:base(uuid, time)
		{
			this.appearance = appearance;
		}

		override public void process(IDispatch dispatch, Logger log)
		{
			dispatch.changeAppearance(this.uuid,this.appearance);
		}
	}

	class ActorAnimationEvent : ActorEvent
	{
		public OpenSim.Framework.Animation[] animations;

		public ActorAnimationEvent(UUID uuid, OpenSim.Framework.Animation[] animations, long time)
			: base(uuid,time)
		{
			this.animations = animations;
		}

		override public void process(IDispatch dispatch, Logger log)
		{
			dispatch.animateActor(this.uuid,this.animations);
		}
	}

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

		public ActorMovedEvent(Actor a, long time) :
			base(a.uuid, time)
		{
			this.controlFlags = a.controlFlags;
			this.velocity = a.velocity;
			this.isFlying = a.isFlying;
			this.position = a.position;
			this.rotation = a.rotation;
			this.AngularVelocity = a.angularVelocity;
		}

		override public void process(IDispatch dispatch, Logger log)
		{
			log(string.Format("Moving actor {0} to {1}", this.uuid, this.position));
			dispatch.moveActor(this.uuid, this.position, this.rotation, this.velocity, this.isFlying, this.controlFlags);
		}
	}
}