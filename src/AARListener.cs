using OpenSim.Region.Framework.Scenes;
using System;
using OpenSim.Region.Framework.Scenes.Animation;
using OpenMetaverse;
using OpenSim.Framework;
using System.Collections.Generic;

namespace MOSES.AAR
{
	public class AARListener : IClientAPI
	{
		private Logger log;

		public AARListener(Scene scene, Logger log)
		{
			CircuitCode = (uint)Util.RandomClass.Next(0,int.MaxValue);
			circuitData = new AgentCircuitData();
			circuitData.AgentID = UUID.Random();
			circuitData.firstname = "";
			circuitData.lastname = "";
			circuitData.ServiceURLs = new Dictionary<string, object>();
			this.log = log;
			this.Scene = scene;
		}

		#region IClientAPI implementation

		private AgentCircuitData circuitData;
		public uint CircuitCode {get; private set;}
		public AgentCircuitData RequestClientInfo (){	return circuitData;		}
		public IScene Scene {get; private set;}

		public event GenericMessage OnGenericMessage;
		public event ImprovedInstantMessage OnInstantMessage;
		public event ChatMessage OnChatFromClient;
		public event TextureRequest OnRequestTexture;
		public event RezObject OnRezObject;
		public event ModifyTerrain OnModifyTerrain;
		public event BakeTerrain OnBakeTerrain;
		public event EstateChangeInfo OnEstateChangeInfo;
		public event EstateManageTelehub OnEstateManageTelehub;
		public event CachedTextureRequest OnCachedTextureRequest;
		public event SetAppearance OnSetAppearance;
		public event AvatarNowWearing OnAvatarNowWearing;
		public event RezSingleAttachmentFromInv OnRezSingleAttachmentFromInv;
		public event RezMultipleAttachmentsFromInv OnRezMultipleAttachmentsFromInv;
		public event UUIDNameRequest OnDetachAttachmentIntoInv;
		public event ObjectAttach OnObjectAttach;
		public event ObjectDeselect OnObjectDetach;
		public event ObjectDrop OnObjectDrop;
		public event StartAnim OnStartAnim;
		public event StopAnim OnStopAnim;
		public event LinkObjects OnLinkObjects;
		public event DelinkObjects OnDelinkObjects;
		public event RequestMapBlocks OnRequestMapBlocks;
		public event RequestMapName OnMapNameRequest;
		public event TeleportLocationRequest OnTeleportLocationRequest;
		public event DisconnectUser OnDisconnectUser;
		public event RequestAvatarProperties OnRequestAvatarProperties;
		public event SetAlwaysRun OnSetAlwaysRun;
		public event TeleportLandmarkRequest OnTeleportLandmarkRequest;
		public event TeleportCancel OnTeleportCancel;
		public event DeRezObject OnDeRezObject;
		public event Action<IClientAPI> OnRegionHandShakeReply;
		public event GenericCall1 OnRequestWearables;
		public event Action<IClientAPI, bool> OnCompleteMovementToRegion;
		public event UpdateAgent OnPreAgentUpdate;
		public event UpdateAgent OnAgentUpdate;
		public event UpdateAgent OnAgentCameraUpdate;
		public event AgentRequestSit OnAgentRequestSit;
		public event AgentSit OnAgentSit;
		public event AvatarPickerRequest OnAvatarPickerRequest;
		public event Action<IClientAPI> OnRequestAvatarsData;
		public event AddNewPrim OnAddPrim;
		public event FetchInventory OnAgentDataUpdateRequest;
		public event TeleportLocationRequest OnSetStartLocationRequest;
		public event RequestGodlikePowers OnRequestGodlikePowers;
		public event GodKickUser OnGodKickUser;
		public event ObjectDuplicate OnObjectDuplicate;
		public event ObjectDuplicateOnRay OnObjectDuplicateOnRay;
		public event GrabObject OnGrabObject;
		public event DeGrabObject OnDeGrabObject;
		public event MoveObject OnGrabUpdate;
		public event SpinStart OnSpinStart;
		public event SpinObject OnSpinUpdate;
		public event SpinStop OnSpinStop;
		public event UpdateShape OnUpdatePrimShape;
		public event ObjectExtraParams OnUpdateExtraParams;
		public event ObjectRequest OnObjectRequest;
		public event ObjectSelect OnObjectSelect;
		public event ObjectDeselect OnObjectDeselect;
		public event GenericCall7 OnObjectDescription;
		public event GenericCall7 OnObjectName;
		public event GenericCall7 OnObjectClickAction;
		public event GenericCall7 OnObjectMaterial;
		public event RequestObjectPropertiesFamily OnRequestObjectPropertiesFamily;
		public event UpdatePrimFlags OnUpdatePrimFlags;
		public event UpdatePrimTexture OnUpdatePrimTexture;
		public event UpdateVector OnUpdatePrimGroupPosition;
		public event UpdateVector OnUpdatePrimSinglePosition;
		public event UpdatePrimRotation OnUpdatePrimGroupRotation;
		public event UpdatePrimSingleRotation OnUpdatePrimSingleRotation;
		public event UpdatePrimSingleRotationPosition OnUpdatePrimSingleRotationPosition;
		public event UpdatePrimGroupRotation OnUpdatePrimGroupMouseRotation;
		public event UpdateVector OnUpdatePrimScale;
		public event UpdateVector OnUpdatePrimGroupScale;
		public event StatusChange OnChildAgentStatus;
		public event GenericCall2 OnStopMovement;
		public event Action<UUID> OnRemoveAvatar;
		public event ObjectPermissions OnObjectPermissions;
		public event CreateNewInventoryItem OnCreateNewInventoryItem;
		public event LinkInventoryItem OnLinkInventoryItem;
		public event CreateInventoryFolder OnCreateNewInventoryFolder;
		public event UpdateInventoryFolder OnUpdateInventoryFolder;
		public event MoveInventoryFolder OnMoveInventoryFolder;
		public event FetchInventoryDescendents OnFetchInventoryDescendents;
		public event PurgeInventoryDescendents OnPurgeInventoryDescendents;
		public event FetchInventory OnFetchInventory;
		public event RequestTaskInventory OnRequestTaskInventory;
		public event UpdateInventoryItem OnUpdateInventoryItem;
		public event CopyInventoryItem OnCopyInventoryItem;
		public event MoveInventoryItem OnMoveInventoryItem;
		public event RemoveInventoryFolder OnRemoveInventoryFolder;
		public event RemoveInventoryItem OnRemoveInventoryItem;
		public event UDPAssetUploadRequest OnAssetUploadRequest;
		public event XferReceive OnXferReceive;
		public event RequestXfer OnRequestXfer;
		public event ConfirmXfer OnConfirmXfer;
		public event AbortXfer OnAbortXfer;
		public event RezScript OnRezScript;
		public event UpdateTaskInventory OnUpdateTaskInventory;
		public event MoveTaskInventory OnMoveTaskItem;
		public event RemoveTaskInventory OnRemoveTaskItem;
		public event RequestAsset OnRequestAsset;
		public event UUIDNameRequest OnNameFromUUIDRequest;
		public event ParcelAccessListRequest OnParcelAccessListRequest;
		public event ParcelAccessListUpdateRequest OnParcelAccessListUpdateRequest;
		public event ParcelPropertiesRequest OnParcelPropertiesRequest;
		public event ParcelDivideRequest OnParcelDivideRequest;
		public event ParcelJoinRequest OnParcelJoinRequest;
		public event ParcelPropertiesUpdateRequest OnParcelPropertiesUpdateRequest;
		public event ParcelSelectObjects OnParcelSelectObjects;
		public event ParcelObjectOwnerRequest OnParcelObjectOwnerRequest;
		public event ParcelAbandonRequest OnParcelAbandonRequest;
		public event ParcelGodForceOwner OnParcelGodForceOwner;
		public event ParcelReclaim OnParcelReclaim;
		public event ParcelReturnObjectsRequest OnParcelReturnObjectsRequest;
		public event ParcelDeedToGroup OnParcelDeedToGroup;
		public event RegionInfoRequest OnRegionInfoRequest;
		public event EstateCovenantRequest OnEstateCovenantRequest;
		public event FriendActionDelegate OnApproveFriendRequest;
		public event FriendActionDelegate OnDenyFriendRequest;
		public event FriendshipTermination OnTerminateFriendship;
		public event MoneyTransferRequest OnMoneyTransferRequest;
		public event EconomyDataRequest OnEconomyDataRequest;
		public event MoneyBalanceRequest OnMoneyBalanceRequest;
		public event UpdateAvatarProperties OnUpdateAvatarProperties;
		public event ParcelBuy OnParcelBuy;
		public event RequestPayPrice OnRequestPayPrice;
		public event ObjectSaleInfo OnObjectSaleInfo;
		public event ObjectBuy OnObjectBuy;
		public event BuyObjectInventory OnBuyObjectInventory;
		public event RequestTerrain OnRequestTerrain;
		public event RequestTerrain OnUploadTerrain;
		public event ObjectIncludeInSearch OnObjectIncludeInSearch;
		public event UUIDNameRequest OnTeleportHomeRequest;
		public event ScriptAnswer OnScriptAnswer;
		public event AgentSit OnUndo;
		public event AgentSit OnRedo;
		public event LandUndo OnLandUndo;
		public event ForceReleaseControls OnForceReleaseControls;
		public event GodLandStatRequest OnLandStatRequest;
		public event DetailedEstateDataRequest OnDetailedEstateDataRequest;
		public event SetEstateFlagsRequest OnSetEstateFlagsRequest;
		public event SetEstateTerrainBaseTexture OnSetEstateTerrainBaseTexture;
		public event SetEstateTerrainDetailTexture OnSetEstateTerrainDetailTexture;
		public event SetEstateTerrainTextureHeights OnSetEstateTerrainTextureHeights;
		public event CommitEstateTerrainTextureRequest OnCommitEstateTerrainTextureRequest;
		public event SetRegionTerrainSettings OnSetRegionTerrainSettings;
		public event EstateRestartSimRequest OnEstateRestartSimRequest;
		public event EstateChangeCovenantRequest OnEstateChangeCovenantRequest;
		public event UpdateEstateAccessDeltaRequest OnUpdateEstateAccessDeltaRequest;
		public event SimulatorBlueBoxMessageRequest OnSimulatorBlueBoxMessageRequest;
		public event EstateBlueBoxMessageRequest OnEstateBlueBoxMessageRequest;
		public event EstateDebugRegionRequest OnEstateDebugRegionRequest;
		public event EstateTeleportOneUserHomeRequest OnEstateTeleportOneUserHomeRequest;
		public event EstateTeleportAllUsersHomeRequest OnEstateTeleportAllUsersHomeRequest;
		public event UUIDNameRequest OnUUIDGroupNameRequest;
		public event RegionHandleRequest OnRegionHandleRequest;
		public event ParcelInfoRequest OnParcelInfoRequest;
		public event RequestObjectPropertiesFamily OnObjectGroupRequest;
		public event ScriptReset OnScriptReset;
		public event GetScriptRunning OnGetScriptRunning;
		public event SetScriptRunning OnSetScriptRunning;
		public event Action<Vector3, bool, bool> OnAutoPilotGo;
		public event TerrainUnacked OnUnackedTerrain;
		public event ActivateGesture OnActivateGesture;
		public event DeactivateGesture OnDeactivateGesture;
		public event ObjectOwner OnObjectOwner;
		public event DirPlacesQuery OnDirPlacesQuery;
		public event DirFindQuery OnDirFindQuery;
		public event DirLandQuery OnDirLandQuery;
		public event DirPopularQuery OnDirPopularQuery;
		public event DirClassifiedQuery OnDirClassifiedQuery;
		public event EventInfoRequest OnEventInfoRequest;
		public event ParcelSetOtherCleanTime OnParcelSetOtherCleanTime;
		public event MapItemRequest OnMapItemRequest;
		public event OfferCallingCard OnOfferCallingCard;
		public event AcceptCallingCard OnAcceptCallingCard;
		public event DeclineCallingCard OnDeclineCallingCard;
		public event SoundTrigger OnSoundTrigger;
		public event StartLure OnStartLure;
		public event TeleportLureRequest OnTeleportLureRequest;
		public event NetworkStats OnNetworkStatsUpdate;
		public event ClassifiedInfoRequest OnClassifiedInfoRequest;
		public event ClassifiedInfoUpdate OnClassifiedInfoUpdate;
		public event ClassifiedDelete OnClassifiedDelete;
		public event ClassifiedDelete OnClassifiedGodDelete;
		public event EventNotificationAddRequest OnEventNotificationAddRequest;
		public event EventNotificationRemoveRequest OnEventNotificationRemoveRequest;
		public event EventGodDelete OnEventGodDelete;
		public event ParcelDwellRequest OnParcelDwellRequest;
		public event UserInfoRequest OnUserInfoRequest;
		public event UpdateUserInfo OnUpdateUserInfo;
		public event RetrieveInstantMessages OnRetrieveInstantMessages;
		public event PickDelete OnPickDelete;
		public event PickGodDelete OnPickGodDelete;
		public event PickInfoUpdate OnPickInfoUpdate;
		public event AvatarNotesUpdate OnAvatarNotesUpdate;
		public event AvatarInterestUpdate OnAvatarInterestUpdate;
		public event GrantUserFriendRights OnGrantUserRights;
		public event MuteListRequest OnMuteListRequest;
		public event PlacesQuery OnPlacesQuery;
		public event FindAgentUpdate OnFindAgent;
		public event TrackAgentUpdate OnTrackAgent;
		public event NewUserReport OnUserReport;
		public event SaveStateHandler OnSaveState;
		public event GroupAccountSummaryRequest OnGroupAccountSummaryRequest;
		public event GroupAccountDetailsRequest OnGroupAccountDetailsRequest;
		public event GroupAccountTransactionsRequest OnGroupAccountTransactionsRequest;
		public event FreezeUserUpdate OnParcelFreezeUser;
		public event EjectUserUpdate OnParcelEjectUser;
		public event ParcelBuyPass OnParcelBuyPass;
		public event ParcelGodMark OnParcelGodMark;
		public event GroupActiveProposalsRequest OnGroupActiveProposalsRequest;
		public event GroupVoteHistoryRequest OnGroupVoteHistoryRequest;
		public event SimWideDeletesDelegate OnSimWideDeletes;
		public event SendPostcard OnSendPostcard;
		public event MuteListEntryUpdate OnUpdateMuteListEntry;
		public event MuteListEntryRemove OnRemoveMuteListEntry;
		public event GodlikeMessage onGodlikeMessage;
		public event GodUpdateRegionInfoUpdate OnGodUpdateRegionInfoUpdate;
		public event ViewerEffectEventHandler OnViewerEffect;
		public event Action<IClientAPI> OnLogout;
		public event Action<IClientAPI> OnConnectionClosed;

		public ulong GetGroupPowers (UUID groupID)
		{
			throw new NotImplementedException ();
		}

		public bool IsGroupMember (UUID GroupID)
		{
			throw new NotImplementedException ();
		}

		public void InPacket (object NewPack)
		{
			log("aarlistener inpacket");
			throw new NotImplementedException ();
		}

		public void ProcessInPacket (OpenMetaverse.Packets.Packet NewPack)
		{
			log("aarlistener processinpacket");
			throw new NotImplementedException ();
		}

		public void Close ()
		{
			log("aarlistener close");
			throw new NotImplementedException ();
		}

		public void Close (bool force)
		{
			log("aarlistener close force");
			throw new NotImplementedException ();
		}

		public void Kick (string message)
		{
			log("aarlistener kick");
			throw new NotImplementedException ();
		}

		public void Start ()
		{
			log("aarlistener start");
			throw new NotImplementedException ();
		}

		public void Stop ()
		{
			log("aarlistener stop");
			throw new NotImplementedException ();
		}

		public void SendWearables (AvatarWearable[] wearables, int serial)
		{
			log("aarlistener sendwearables");
			throw new NotImplementedException ();
		}

		public void SendAppearance (UUID agentID, byte[] visualParams, byte[] textureEntry)
		{
			log("aarlistener sendAppearance");
			throw new NotImplementedException ();
		}

		public void SendCachedTextureResponse (ISceneEntity avatar, int serial, System.Collections.Generic.List<CachedTextureResponseArg> cachedTextures)
		{
			throw new NotImplementedException ();
		}

		public void SendStartPingCheck (byte seq)
		{
			throw new NotImplementedException ();
		}

		public void SendKillObject (System.Collections.Generic.List<uint> localID)
		{
			throw new NotImplementedException ();
		}

		public void SendAnimations (UUID[] animID, int[] seqs, UUID sourceAgentId, UUID[] objectIDs)
		{
			log("aarlistener sendAnimations");
			throw new NotImplementedException ();
		}

		public void SendRegionHandshake (RegionInfo regionInfo, RegionHandshakeArgs args)
		{
			log("aarlistener sendregionhandshake");
			throw new NotImplementedException ();
		}

		public void SendChatMessage (string message, byte type, Vector3 fromPos, string fromName, UUID fromAgentID, UUID ownerID, byte source, byte audible)
		{
			log("aarlistener sendChatMessage");
			throw new NotImplementedException ();
		}

		public void SendInstantMessage (GridInstantMessage im)
		{
			throw new NotImplementedException ();
		}

		public void SendGenericMessage (string method, UUID invoice, System.Collections.Generic.List<string> message)
		{
			throw new NotImplementedException ();
		}

		public void SendGenericMessage (string method, UUID invoice, System.Collections.Generic.List<byte[]> message)
		{
			throw new NotImplementedException ();
		}

		public void SendLayerData (float[] map)
		{
			throw new NotImplementedException ();
		}

		public void SendLayerData (int px, int py, float[] map)
		{
			throw new NotImplementedException ();
		}

		public void SendWindData (Vector2[] windSpeeds)
		{
			throw new NotImplementedException ();
		}

		public void SendCloudData (float[] cloudCover)
		{
			throw new NotImplementedException ();
		}

		public void MoveAgentIntoRegion (RegionInfo regInfo, Vector3 pos, Vector3 look)
		{
			log("aarlistener MoveAgentIntoRegion");
			throw new NotImplementedException ();
		}

		public void InformClientOfNeighbour (ulong neighbourHandle, System.Net.IPEndPoint neighbourExternalEndPoint)
		{
			throw new NotImplementedException ();
		}



		public void CrossRegion (ulong newRegionHandle, Vector3 pos, Vector3 lookAt, System.Net.IPEndPoint newRegionExternalEndPoint, string capsURL)
		{
			throw new NotImplementedException ();
		}

		public void SendMapBlock (System.Collections.Generic.List<MapBlockData> mapBlocks, uint flag)
		{
			throw new NotImplementedException ();
		}

		public void SendLocalTeleport (Vector3 position, Vector3 lookAt, uint flags)
		{
			log("aarlistener sendlocalteleport");
			throw new NotImplementedException ();
		}

		public void SendRegionTeleport (ulong regionHandle, byte simAccess, System.Net.IPEndPoint regionExternalEndPoint, uint locationID, uint flags, string capsURL)
		{
			log("aarlistener sendregionteleport");
			throw new NotImplementedException ();
		}

		public void SendTeleportFailed (string reason)
		{
			throw new NotImplementedException ();
		}

		public void SendTeleportStart (uint flags)
		{
			throw new NotImplementedException ();
		}

		public void SendTeleportProgress (uint flags, string message)
		{
			throw new NotImplementedException ();
		}

		public void SendMoneyBalance (UUID transaction, bool success, byte[] description, int balance, int transactionType, UUID sourceID, bool sourceIsGroup, UUID destID, bool destIsGroup, int amount, string item)
		{
			throw new NotImplementedException ();
		}

		public void SendPayPrice (UUID objectID, int[] payPrice)
		{
			throw new NotImplementedException ();
		}

		public void SendCoarseLocationUpdate (System.Collections.Generic.List<UUID> users, System.Collections.Generic.List<Vector3> CoarseLocations)
		{
			throw new NotImplementedException ();
		}

		public void SetChildAgentThrottle (byte[] throttle)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarDataImmediate (ISceneEntity avatar)
		{
			log("aarlistener sendavatardataimmediately");
			throw new NotImplementedException ();
		}

		public void SendEntityUpdate (ISceneEntity entity, PrimUpdateFlags updateFlags)
		{
			log("aarlistener sendEntityUpdate");
			throw new NotImplementedException ();
		}

		public void ReprioritizeUpdates ()
		{
			throw new NotImplementedException ();
		}

		public void FlushPrimUpdates ()
		{
			throw new NotImplementedException ();
		}

		public void SendInventoryFolderDetails (UUID ownerID, UUID folderID, System.Collections.Generic.List<InventoryItemBase> items, System.Collections.Generic.List<InventoryFolderBase> folders, int version, bool fetchFolders, bool fetchItems)
		{
			throw new NotImplementedException ();
		}

		public void SendInventoryItemDetails (UUID ownerID, InventoryItemBase item)
		{
			throw new NotImplementedException ();
		}

		public void SendInventoryItemCreateUpdate (InventoryItemBase Item, uint callbackId)
		{
			throw new NotImplementedException ();
		}

		public void SendRemoveInventoryItem (UUID itemID)
		{
			throw new NotImplementedException ();
		}

		public void SendTakeControls (int controls, bool passToAgent, bool TakeControls)
		{
			throw new NotImplementedException ();
		}

		public void SendTaskInventory (UUID taskID, short serial, byte[] fileName)
		{
			throw new NotImplementedException ();
		}

		public void SendTelehubInfo (UUID ObjectID, string ObjectName, Vector3 ObjectPos, Quaternion ObjectRot, System.Collections.Generic.List<Vector3> SpawnPoint)
		{
			throw new NotImplementedException ();
		}

		public void SendBulkUpdateInventory (InventoryNodeBase node)
		{
			throw new NotImplementedException ();
		}

		public void SendXferPacket (ulong xferID, uint packet, byte[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendAbortXferPacket (ulong xferID)
		{
			throw new NotImplementedException ();
		}

		public void SendEconomyData (float EnergyEfficiency, int ObjectCapacity, int ObjectCount, int PriceEnergyUnit, int PriceGroupCreate, int PriceObjectClaim, float PriceObjectRent, float PriceObjectScaleFactor, int PriceParcelClaim, float PriceParcelClaimFactor, int PriceParcelRent, int PricePublicObjectDecay, int PricePublicObjectDelete, int PriceRentLight, int PriceUpload, int TeleportMinPrice, float TeleportPriceExponent)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarPickerReply (AvatarPickerReplyAgentDataArgs AgentData, System.Collections.Generic.List<AvatarPickerReplyDataArgs> Data)
		{
			throw new NotImplementedException ();
		}

		public void SendAgentDataUpdate (UUID agentid, UUID activegroupid, string firstname, string lastname, ulong grouppowers, string groupname, string grouptitle)
		{
			log("aarlistener sendagentupdate");
			throw new NotImplementedException ();
		}

		public void SendPreLoadSound (UUID objectID, UUID ownerID, UUID soundID)
		{
			log("aarlistener sendpreloadsound");
			throw new NotImplementedException ();
		}

		public void SendPlayAttachedSound (UUID soundID, UUID objectID, UUID ownerID, float gain, byte flags)
		{
			log("aarlistener sendplayattachedsound");
			throw new NotImplementedException ();
		}

		public void SendTriggeredSound (UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position, float gain)
		{
			log("aarlistener sendtriggeredsound");
			throw new NotImplementedException ();
		}

		public void SendAttachedSoundGainChange (UUID objectID, float gain)
		{
			throw new NotImplementedException ();
		}

		public void SendNameReply (UUID profileId, string firstname, string lastname)
		{
			throw new NotImplementedException ();
		}

		public void SendAlertMessage (string message)
		{
			throw new NotImplementedException ();
		}

		public void SendAgentAlertMessage (string message, bool modal)
		{
			throw new NotImplementedException ();
		}

		public void SendLoadURL (string objectname, UUID objectID, UUID ownerID, bool groupOwned, string message, string url)
		{
			throw new NotImplementedException ();
		}

		public void SendDialog (string objectname, UUID objectID, UUID ownerID, string ownerFirstName, string ownerLastName, string msg, UUID textureID, int ch, string[] buttonlabels)
		{
			throw new NotImplementedException ();
		}

		public void SendSunPos (Vector3 sunPos, Vector3 sunVel, ulong CurrentTime, uint SecondsPerSunCycle, uint SecondsPerYear, float OrbitalPosition)
		{
			throw new NotImplementedException ();
		}

		public void SendViewerEffect (OpenMetaverse.Packets.ViewerEffectPacket.EffectBlock[] effectBlocks)
		{
			throw new NotImplementedException ();
		}

		public void SendViewerTime (int phase)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarProperties (UUID avatarID, string aboutText, string bornOn, byte[] charterMember, string flAbout, uint flags, UUID flImageID, UUID imageID, string profileURL, UUID partnerID)
		{
			log("aarlistener sendavatarproperties");
			throw new NotImplementedException ();
		}

		public void SendScriptQuestion (UUID taskID, string taskName, string ownerName, UUID itemID, int question)
		{
			throw new NotImplementedException ();
		}

		public void SendHealth (float health)
		{
			throw new NotImplementedException ();
		}

		public void SendEstateList (UUID invoice, int code, UUID[] Data, uint estateID)
		{
			throw new NotImplementedException ();
		}

		public void SendBannedUserList (UUID invoice, EstateBan[] banlist, uint estateID)
		{
			throw new NotImplementedException ();
		}

		public void SendRegionInfoToEstateMenu (RegionInfoForEstateMenuArgs args)
		{
			throw new NotImplementedException ();
		}

		public void SendEstateCovenantInformation (UUID covenant)
		{
			throw new NotImplementedException ();
		}

		public void SendDetailedEstateData (UUID invoice, string estateName, uint estateID, uint parentEstate, uint estateFlags, uint sunPosition, UUID covenant, uint covenantChanged, string abuseEmail, UUID estateOwner)
		{
			throw new NotImplementedException ();
		}

		public void SendLandProperties (int sequence_id, bool snap_selection, int request_result, ILandObject lo, float simObjectBonusFactor, int parcelObjectCapacity, int simObjectCapacity, uint regionFlags)
		{
			throw new NotImplementedException ();
		}

		public void SendLandAccessListData (System.Collections.Generic.List<LandAccessEntry> accessList, uint accessFlag, int localLandID)
		{
			throw new NotImplementedException ();
		}

		public void SendForceClientSelectObjects (System.Collections.Generic.List<uint> objectIDs)
		{
			log("aarlistener sendforceclientselectobjects");
			throw new NotImplementedException ();
		}

		public void SendCameraConstraint (Vector4 ConstraintPlane)
		{
			throw new NotImplementedException ();
		}

		public void SendLandObjectOwners (LandData land, System.Collections.Generic.List<UUID> groups, System.Collections.Generic.Dictionary<UUID, int> ownersAndCount)
		{
			throw new NotImplementedException ();
		}

		public void SendLandParcelOverlay (byte[] data, int sequence_id)
		{
			throw new NotImplementedException ();
		}

		public void SendParcelMediaCommand (uint flags, ParcelMediaCommandEnum command, float time)
		{
			throw new NotImplementedException ();
		}

		public void SendParcelMediaUpdate (string mediaUrl, UUID mediaTextureID, byte autoScale, string mediaType, string mediaDesc, int mediaWidth, int mediaHeight, byte mediaLoop)
		{
			throw new NotImplementedException ();
		}

		public void SendAssetUploadCompleteMessage (sbyte AssetType, bool Success, UUID AssetFullID)
		{
			throw new NotImplementedException ();
		}

		public void SendConfirmXfer (ulong xferID, uint PacketID)
		{
			throw new NotImplementedException ();
		}

		public void SendXferRequest (ulong XferID, short AssetType, UUID vFileID, byte FilePath, byte[] FileName)
		{
			throw new NotImplementedException ();
		}

		public void SendInitiateDownload (string simFileName, string clientFileName)
		{
			throw new NotImplementedException ();
		}

		public void SendImageFirstPart (ushort numParts, UUID ImageUUID, uint ImageSize, byte[] ImageData, byte imageCodec)
		{
			throw new NotImplementedException ();
		}

		public void SendImageNextPart (ushort partNumber, UUID imageUuid, byte[] imageData)
		{
			throw new NotImplementedException ();
		}

		public void SendImageNotFound (UUID imageid)
		{
			throw new NotImplementedException ();
		}

		public void SendShutdownConnectionNotice ()
		{
			throw new NotImplementedException ();
		}

		public void SendSimStats (SimStats stats)
		{
			log("aarlistener sendsimstats");
			throw new NotImplementedException ();
		}

		public void SendObjectPropertiesFamilyData (ISceneEntity Entity, uint RequestFlags)
		{
			throw new NotImplementedException ();
		}

		public void SendObjectPropertiesReply (ISceneEntity Entity)
		{
			throw new NotImplementedException ();
		}

		public void SendPartPhysicsProprieties (ISceneEntity Entity)
		{
			throw new NotImplementedException ();
		}

		public void SendAgentOffline (UUID[] agentIDs)
		{
			log("aarlistener sendagentoffline");
			throw new NotImplementedException ();
		}

		public void SendAgentOnline (UUID[] agentIDs)
		{
			log("aarlistener semdagentonline");
			throw new NotImplementedException ();
		}

		public void SendSitResponse (UUID TargetID, Vector3 OffsetPos, Quaternion SitOrientation, bool autopilot, Vector3 CameraAtOffset, Vector3 CameraEyeOffset, bool ForceMouseLook)
		{
			throw new NotImplementedException ();
		}

		public void SendAdminResponse (UUID Token, uint AdminLevel)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupMembership (GroupMembershipData[] GroupMembership)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupNameReply (UUID groupLLUID, string GroupName)
		{
			throw new NotImplementedException ();
		}

		public void SendJoinGroupReply (UUID groupID, bool success)
		{
			throw new NotImplementedException ();
		}

		public void SendEjectGroupMemberReply (UUID agentID, UUID groupID, bool success)
		{
			throw new NotImplementedException ();
		}

		public void SendLeaveGroupReply (UUID groupID, bool success)
		{
			throw new NotImplementedException ();
		}

		public void SendCreateGroupReply (UUID groupID, bool success, string message)
		{
			throw new NotImplementedException ();
		}

		public void SendLandStatReply (uint reportType, uint requestFlags, uint resultCount, LandStatReportItem[] lsrpia)
		{
			throw new NotImplementedException ();
		}

		public void SendScriptRunningReply (UUID objectID, UUID itemID, bool running)
		{
			throw new NotImplementedException ();
		}

		public void SendAsset (AssetRequestToClient req)
		{
			throw new NotImplementedException ();
		}

		public void SendTexture (AssetBase TextureAsset)
		{
			throw new NotImplementedException ();
		}

		public byte[] GetThrottlesPacked (float multiplier)
		{
			throw new NotImplementedException ();
		}

		public void SendBlueBoxMessage (UUID FromAvatarID, string FromAvatarName, string Message)
		{
			throw new NotImplementedException ();
		}

		public void SendLogoutPacket ()
		{
			throw new NotImplementedException ();
		}

		public ClientInfo GetClientInfo ()
		{
			throw new NotImplementedException ();
		}

		public void SetClientInfo (ClientInfo info)
		{
			throw new NotImplementedException ();
		}

		public void SetClientOption (string option, string value)
		{
			throw new NotImplementedException ();
		}

		public string GetClientOption (string option)
		{
			throw new NotImplementedException ();
		}

		public void SendSetFollowCamProperties (UUID objectID, System.Collections.Generic.SortedDictionary<int, float> parameters)
		{
			throw new NotImplementedException ();
		}

		public void SendClearFollowCamProperties (UUID objectID)
		{
			throw new NotImplementedException ();
		}

		public void SendRegionHandle (UUID regoinID, ulong handle)
		{
			throw new NotImplementedException ();
		}

		public void SendParcelInfo (RegionInfo info, LandData land, UUID parcelID, uint x, uint y)
		{
			throw new NotImplementedException ();
		}

		public void SendScriptTeleportRequest (string objName, string simName, Vector3 pos, Vector3 lookAt)
		{
			throw new NotImplementedException ();
		}

		public void SendDirPlacesReply (UUID queryID, DirPlacesReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirPeopleReply (UUID queryID, DirPeopleReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirEventsReply (UUID queryID, DirEventsReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirGroupsReply (UUID queryID, DirGroupsReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirClassifiedReply (UUID queryID, DirClassifiedReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirLandReply (UUID queryID, DirLandReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendDirPopularReply (UUID queryID, DirPopularReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendEventInfoReply (EventData info)
		{
			throw new NotImplementedException ();
		}

		public void SendMapItemReply (mapItemReply[] replies, uint mapitemtype, uint flags)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarGroupsReply (UUID avatarID, GroupMembershipData[] data)
		{
			throw new NotImplementedException ();
		}

		public void SendOfferCallingCard (UUID srcID, UUID transactionID)
		{
			throw new NotImplementedException ();
		}

		public void SendAcceptCallingCard (UUID transactionID)
		{
			throw new NotImplementedException ();
		}

		public void SendDeclineCallingCard (UUID transactionID)
		{
			throw new NotImplementedException ();
		}

		public void SendTerminateFriend (UUID exFriendID)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarClassifiedReply (UUID targetID, UUID[] classifiedID, string[] name)
		{
			throw new NotImplementedException ();
		}

		public void SendClassifiedInfoReply (UUID classifiedID, UUID creatorID, uint creationDate, uint expirationDate, uint category, string name, string description, UUID parcelID, uint parentEstate, UUID snapshotID, string simName, Vector3 globalPos, string parcelName, byte classifiedFlags, int price)
		{
			throw new NotImplementedException ();
		}

		public void SendAgentDropGroup (UUID groupID)
		{
			throw new NotImplementedException ();
		}

		public void RefreshGroupMembership ()
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarNotesReply (UUID targetID, string text)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarPicksReply (UUID targetID, System.Collections.Generic.Dictionary<UUID, string> picks)
		{
			throw new NotImplementedException ();
		}

		public void SendPickInfoReply (UUID pickID, UUID creatorID, bool topPick, UUID parcelID, string name, string desc, UUID snapshotID, string user, string originalName, string simName, Vector3 posGlobal, int sortOrder, bool enabled)
		{
			throw new NotImplementedException ();
		}

		public void SendAvatarClassifiedReply (UUID targetID, System.Collections.Generic.Dictionary<UUID, string> classifieds)
		{
			throw new NotImplementedException ();
		}

		public void SendParcelDwellReply (int localID, UUID parcelID, float dwell)
		{
			throw new NotImplementedException ();
		}

		public void SendUserInfoReply (bool imViaEmail, bool visible, string email)
		{
			throw new NotImplementedException ();
		}

		public void SendUseCachedMuteList ()
		{
			throw new NotImplementedException ();
		}

		public void SendMuteListUpdate (string filename)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupActiveProposals (UUID groupID, UUID transactionID, GroupActiveProposals[] Proposals)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupVoteHistory (UUID groupID, UUID transactionID, GroupVoteHistory[] Votes)
		{
			throw new NotImplementedException ();
		}

		public bool AddGenericPacketHandler (string MethodName, GenericMessage handler)
		{
			throw new NotImplementedException ();
		}

		public void SendRebakeAvatarTextures (UUID textureID)
		{
			log("aarlistener sendrebaketextures");
			throw new NotImplementedException ();
		}

		public void SendAvatarInterestsReply (UUID avatarID, uint wantMask, string wantText, uint skillsMask, string skillsText, string languages)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupAccountingDetails (IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupAccountingSummary (IClientAPI sender, UUID groupID, uint moneyAmt, int totalTier, int usedTier)
		{
			throw new NotImplementedException ();
		}

		public void SendGroupTransactionsSummaryDetails (IClientAPI sender, UUID groupID, UUID transactionID, UUID sessionID, int amt)
		{
			throw new NotImplementedException ();
		}

		public void SendChangeUserRights (UUID agentID, UUID friendID, int rights)
		{
			throw new NotImplementedException ();
		}

		public void SendTextBoxRequest (string message, int chatChannel, string objectname, UUID ownerID, string ownerFirstName, string ownerLastName, UUID objectId)
		{
			throw new NotImplementedException ();
		}

		public void SendAgentTerseUpdate (ISceneEntity presence)
		{
			log("aarlistener sendagentterseupdate");
			throw new NotImplementedException ();
		}

		public void SendPlacesReply (UUID queryID, UUID transactionID, PlacesReplyData[] data)
		{
			throw new NotImplementedException ();
		}

		public Vector3 StartPos {
			get {throw new NotImplementedException ();}
			set {throw new NotImplementedException ();}
		}

		public UUID AgentId {
			get {throw new NotImplementedException ();}
		}

		public ISceneAgent SceneAgent {
			get {throw new NotImplementedException ();}
			set {throw new NotImplementedException ();}
		}

		public UUID SessionId {
			get {throw new NotImplementedException ();}
		}

		public UUID SecureSessionId {
			get {throw new NotImplementedException ();}
		}

		public UUID ActiveGroupId {
			get {throw new NotImplementedException ();}
		}

		public string ActiveGroupName {
			get {throw new NotImplementedException ();}
		}

		public ulong ActiveGroupPowers {
			get {throw new NotImplementedException ();}
		}

		public string FirstName {
			get {
				throw new NotImplementedException ();
			}
		}

		public string LastName {
			get {
				throw new NotImplementedException ();
			}
		}

		public int NextAnimationSequenceNumber {
			get {throw new NotImplementedException ();}
		}

		public string Name {
			get {throw new NotImplementedException ();}
		}

		public bool IsActive {
			get {throw new NotImplementedException ();}
			set {throw new NotImplementedException ();}
		}

		public bool IsLoggingOut {
			get {throw new NotImplementedException ();}
			set {throw new NotImplementedException ();}
		}

		public bool SendLogoutPacketWhenClosing {
			set {throw new NotImplementedException ();}
		}
		public System.Net.IPEndPoint RemoteEndPoint {
			get {throw new NotImplementedException ();}
		}
		public int DebugPacketLevel {
			get {throw new NotImplementedException ();}
			set {throw new NotImplementedException ();}
		}

		#endregion


	}
}