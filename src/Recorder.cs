using System;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.XEngine;
using OpenSim.Region.ScriptEngine.Shared.Api;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace MOSES.AAR
{
	public class Recorder
	{

		private Dictionary<OpenMetaverse.UUID, AvatarActor> avatars = new Dictionary<OpenMetaverse.UUID, AvatarActor>();
		private Dictionary<OpenMetaverse.UUID, ObjectActor> objects = new Dictionary<UUID, ObjectActor>();

		private Queue<AAREvent> recordedActions = new Queue<AAREvent>();
		private Stopwatch sw = new Stopwatch();
		private AARLog log;
		private bool isRecording = false;
		private bool hasRecording = false;
		private UUID sessionId;
		private static int CHUNKSIZE = 16000000;

		private SceneObjectGroup aarBox = null;
		private XEngine xEngine = null;
		private Scene m_scene;

		public Recorder (AARLog log)
		{
			this.log = log;
		}

		public void printStatus()
		{
			string msg = "Recorder Class ";
			if(!isRecording)
			{
				msg += "is not recording";
			}
			else
			{
				msg += string.Format("is holding {0} events over {1} seconds",recordedActions.Count, sw.ElapsedMilliseconds/1000);
			}
			log(msg);
		}

		public void initialize(Scene scene)
		{
			/* Actor Events */
			scene.EventManager.OnNewPresence 					+= onAddAvatar;
			scene.EventManager.OnRemovePresence 				+= onRemoveAvatar;
			scene.EventManager.OnAvatarAppearanceChange 		+= onAvatarAppearanceChanged;
			scene.EventManager.OnScenePresenceUpdated 			+= onAvatarPresenceChanged;
			scene.EventManager.OnMakeChildAgent 				+= onRemoveAvatar;
			scene.EventManager.OnMakeRootAgent 					+= onAddAvatar;

			/* Object Events */
			scene.EventManager.OnObjectAddedToScene				+= onAddObject;
			scene.EventManager.OnSceneObjectLoaded				+= onAddObject;
			scene.EventManager.OnObjectBeingRemovedFromScene	+= onRemoveObject;
			scene.EventManager.OnSceneObjectPartUpdated			+= onUpdateObject;

			/* lookup xEngine */
			IScriptModule scriptModule = null;
			foreach (IScriptModule sm in scene.RequestModuleInterfaces<IScriptModule>())
			{
				if (sm.ScriptEngineName == scene.DefaultScriptEngine)
					scriptModule = sm;
				else if (scriptModule == null)
					scriptModule = sm;
			}
			xEngine = (XEngine)scriptModule;
			m_scene = scene;
		}
		public void registerCommands(IRegionModuleBase regionModule, Scene scene)
		{
			scene.AddCommand("Aar", regionModule,"aar record","record","Begin recording an event",startRecording);
			scene.AddCommand("Aar", regionModule,"aar stop", "stop", "Stop recording an event",stopRecording);
			scene.AddCommand("Aar", regionModule,"aar save", "save", "Persist a recorded event", saveSession);
		}

		public void cleanup()
		{
			//delete any appearance cards for a session that hasn't been saved.
			foreach(AvatarActor a in avatars.Values)
			{
				foreach(string item in a.appearances)
				{
					TaskInventoryItem it = aarBox.RootPart.Inventory.GetInventoryItem(item);
					aarBox.RootPart.Inventory.RemoveInventoryItem(it.ItemID);
				}
			}
			isRecording = false;
			hasRecording = false;
			recordedActions.Clear();
		}

		/*
		 * 
		 *   

            Scene.EventManager.OnTerrainTainted                 += OnTerrainTainted;


            Scene.EventManager.OnChatBroadcast                  += OnLocalChatBroadcast;
            Scene.EventManager.OnChatFromClient                 += OnLocalChatFromClient;
            Scene.EventManager.OnChatFromWorld                  += OnLocalChatFromWorld;
            Scene.EventManager.OnAttach                         += OnLocalAttach;
            Scene.EventManager.OnObjectGrab                     += OnLocalGrabObject;
            Scene.EventManager.OnObjectGrabbing                 += OnLocalObjectGrabbing;
            Scene.EventManager.OnObjectDeGrab                   += OnLocalDeGrabObject;
		 * 

OnSceneGroupMove
OnSceneGroubGrab
OnSceneGroupSpin[Start]
OnLandObjectAdded	???
OnLandObjectRemoved

OnAttach
*/

		public void deinitialize(Scene scene)
		{
			/* Actor Events */
			scene.EventManager.OnNewPresence 					-= onAddAvatar;
			scene.EventManager.OnRemovePresence 				-= onRemoveAvatar;
			scene.EventManager.OnAvatarAppearanceChange 		-= onAvatarAppearanceChanged;
			scene.EventManager.OnScenePresenceUpdated 			-= onAvatarPresenceChanged;
			scene.EventManager.OnMakeChildAgent 				-= onRemoveAvatar;
			scene.EventManager.OnMakeRootAgent 					-= onAddAvatar;

			/* Object Events */
			scene.EventManager.OnObjectAddedToScene				-= onAddObject;
			scene.EventManager.OnSceneObjectLoaded				-= onAddObject;
			scene.EventManager.OnObjectBeingRemovedFromScene	-= onRemoveObject;
			scene.EventManager.OnSceneObjectPartUpdated			-= onUpdateObject;
		}
		
		#region commands
		public void startRecording(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			if(isRecording)
			{
				log("Error starting: AAR is already recording");
				return;
			}
			if(hasRecording)
			{
				log("Overwriting recorded session");
			}
			isRecording = true;
			hasRecording = true;
			sessionId = UUID.Random();
			sw.Restart();
			recordedActions.Clear();
			foreach(AvatarActor a in avatars.Values)
			{
				a.appearanceCount = 0;
				a.appearances.Clear();
				string appearanceName = persistAppearance(a.uuid,a.appearanceCount);
				a.appearances.Add(appearanceName);
				recordedActions.Enqueue(new ActorAddedEvent(a.firstName, a.lastName, a.uuid, appearanceName, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorMovedEvent(a, sw.ElapsedMilliseconds));
				recordedActions.Enqueue(new ActorAnimationEvent(a.uuid, a.animations, sw.ElapsedMilliseconds));
			}
			//FIXME: skip adding initial objects for now, assume the region is populated by oar file
			recordedActions.Enqueue(new EventStart(sw.ElapsedMilliseconds));
			log("Record Start");
		}
		public void stopRecording(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			if( !isRecording )
			{
				log("Error stopping: AAR is not recording");
				return;
			}
			recordedActions.Enqueue(new EventEnd(sw.ElapsedMilliseconds));
			isRecording = false;
			sw.Stop();
		}
		public void saveSession(string module, string[] args)
		{
			if(aarBox == null)
			{
				aarBox = AAR.findAarBox(m_scene);
			}
			if(isRecording)
			{
				log("Error saving session, module is still recording");
				return;
			}
			if(!hasRecording)
			{
				log("Error saving session, there is no recorded session");
				return;
			}
			//persist the session
			string session = "";
			using (MemoryStream msCompressed = new MemoryStream())
			using (GZipStream gZipStream = new GZipStream(msCompressed, CompressionMode.Compress))
			using (MemoryStream msDecompressed = new MemoryStream())
			{
				new BinaryFormatter().Serialize(msDecompressed, recordedActions);
				byte[] byteArray = msDecompressed.ToArray();

				gZipStream.Write(byteArray, 0, byteArray.Length);
				gZipStream.Close();
				session = Convert.ToBase64String(msCompressed.ToArray());
			}
			//the recorded session events are all compressed and encoded into the string in session
			OSSL_Api osslApi = new OSSL_Api();
			osslApi.Initialize(xEngine, aarBox.RootPart, null, null);
			for(int i = 0, n = 0; i < session.Length; i+= CHUNKSIZE, n++)
			{
				string sessionChunk;
				if(i+CHUNKSIZE > session.Length)
				{
					sessionChunk = session.Substring(i, session.Length-i);
				}
				else
				{
					sessionChunk = session.Substring(i, CHUNKSIZE);
				}
				OpenSim.Region.ScriptEngine.Shared.LSL_Types.list l = new OpenSim.Region.ScriptEngine.Shared.LSL_Types.list();
				l.Add(sessionChunk);
				string notecardName = string.Format("session:{0}:{1}", sessionId,n);
				osslApi.osMakeNotecard(notecardName,l);
			}

			log(string.Format("{0} bytes", System.Text.ASCIIEncoding.ASCII.GetByteCount(session)));
			foreach(AvatarActor a in avatars.Values)
			{
				a.appearances.Clear();
				a.appearanceCount = 0;
			}
			hasRecording = false;
		}
		#endregion

		#region AvatarInterface

		public void onAddAvatar(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID))
			{
				log("Duplicate Presence Detected, not adding avatar");
			}
			else
			{
				avatars[client.UUID] = new AvatarActor(client);
				avatars[client.UUID].appearanceCount = 0;
				avatars[client.UUID].appearances.Clear();
				if(isRecording)
				{
					string appearanceName = persistAppearance(client.UUID,avatars[client.UUID].appearanceCount);
					avatars[client.UUID].appearances.Add(appearanceName);
					log(string.Format("New Presence: {0} , tracking {1} Actors", this.avatars[client.UUID].firstName, this.avatars.Count));
					recordedActions.Enqueue(new ActorAddedEvent(avatars[client.UUID].firstName, avatars[client.UUID].lastName, client.UUID, appearanceName, sw.ElapsedMilliseconds));
					recordedActions.Enqueue(new ActorMovedEvent(avatars[client.UUID], sw.ElapsedMilliseconds));
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID, avatars[client.UUID].animations, sw.ElapsedMilliseconds));
				}
			}
		}

		public void onAvatarAppearanceChanged(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID) && isRecording)
			{
				avatars[client.UUID].appearanceCount++;
				string appearanceName = persistAppearance(client.UUID, avatars[client.UUID].appearanceCount);
				recordedActions.Enqueue(new ActorAppearanceEvent(client.UUID, appearanceName,sw.ElapsedMilliseconds));
			}
		}

		public void onAvatarPresenceChanged(ScenePresence client)
		{
			if(this.avatars.ContainsKey(client.UUID) && isRecording)
			{
				//determine what has changed about the avatar

				//Position/Control flags
				if(avatars[client.UUID].movementChanged(client))
				{
					recordedActions.Enqueue(new ActorMovedEvent(client, sw.ElapsedMilliseconds));
					avatars[client.UUID].updateMovement(client);
				}

				//animation update
				OpenSim.Framework.Animation[] anims = client.Animator.Animations.ToArray();
				if( ! anims.SequenceEqual(avatars[client.UUID].animations))
				{
					recordedActions.Enqueue(new ActorAnimationEvent(client.UUID,anims, sw.ElapsedMilliseconds));
					avatars[client.UUID].animations = anims;
				}

				//client.Animator.Animations.ToArray;
				//client.Appearance; //not really, we have a separate signal for appeatance changed

				//client.GetAttachments;
				////client.GetWorldRotation;
				//client.IsSatOnObject;
				//client.Lookat;

				//client.SitGround;

			}
		}

		public void onRemoveAvatar(ScenePresence client){	onRemoveAvatar(client.UUID);	}
		public void onRemoveAvatar(OpenMetaverse.UUID uuid)
		{
			if(this.avatars.ContainsKey(uuid))
			{
				if(isRecording)
				{
					recordedActions.Enqueue(new ActorRemovedEvent(uuid, sw.ElapsedMilliseconds));
				}
				this.avatars.Remove(uuid);
			}
		}

		#endregion

		#region ObjectInterface

		public void onAddObject(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				//objects appear to be instantiated twice...
				if(! objects.ContainsKey(part.UUID))
				{
					objects.Add(part.UUID, new ObjectActor(part));
					if(isRecording)
					{
						persistObject(part);
						recordedActions.Enqueue(new ObjectAddedEvent(part.UUID, part.Name,sw.ElapsedMilliseconds));
						recordedActions.Enqueue(new ObjectMovedEvent(part.UUID,part.AbsolutePosition,part.GetWorldRotation(),part.Velocity,part.AngularVelocity,sw.ElapsedMilliseconds));
					}
				}
			}
		}

		public void onRemoveObject(SceneObjectGroup sog)
		{
			foreach(SceneObjectPart part in sog.Parts)
			{
				//test first, objects are removed more than once
				if(objects.ContainsKey(part.UUID))
				{
					objects.Remove(part.UUID);
					if(isRecording)
					{
						recordedActions.Enqueue(new ObjectRemovedEvent(part.UUID,sw.ElapsedMilliseconds));
					}
				}
			}
		}

		public void onUpdateObject(SceneObjectPart sop, bool flag)
		{
			if(objects.ContainsKey(sop.UUID))
			{
				if(objects[sop.UUID].movementChanged(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity)){
					objects[sop.UUID].updateMovement(sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity);
					if(isRecording)
					{
						recordedActions.Enqueue(new ObjectMovedEvent(sop.UUID,sop.AbsolutePosition,sop.GetWorldRotation(),sop.Velocity,sop.AngularVelocity,sw.ElapsedMilliseconds));
					}
				}
			}
		}

		#endregion

		#region persistance

		private void persistObject(SceneObjectPart sop)
		{
			OpenSim.Framework.InventoryItemBase item = new InventoryItemBase(sop.UUID);
			item.Name = sop.UUID.ToString();
			aarBox.AddInventoryItem(UUID.Zero,sop.LocalId,item,sop.UUID);
		}

		private string persistAppearance(UUID avatarID, int appearanceVersion)
		{
			OSSL_Api osslApi = new OSSL_Api();
			osslApi.Initialize(xEngine, aarBox.RootPart, null, null);
			string notecardName = string.Format("{0}:appearance:{1}:{2}",sessionId,avatarID,appearanceVersion);
			osslApi.osAgentSaveAppearance(avatarID.ToString(), notecardName);
			return notecardName;
		}

		#endregion
	}
}

