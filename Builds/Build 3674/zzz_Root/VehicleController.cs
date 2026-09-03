using System;
using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings;
using Character;
using Extensions;
using GamePrompt.Runtime.Scripts;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using NWH.Common.CoM;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using Streets;
using UI;
using UI.ItemPanel;
using UI.Notification;
using UI.Purchase;
using UnityEngine;
using UnityEngine.AI;
using Vehicles.VehicleTypes;

public class VehicleController : EntityController
{
	private const float MinimumDistanceToWarpIfNoPath = 2.5f;

	public VehicleType vehicleType;

	[SerializeField]
	public VehicleInstance vehicleInstance;

	public bool controlledByPlayer;

	public bool showCargoInItemPanel;

	public PointOfInterest poi;

	public NavMeshObstacle navMeshObstacle;

	public Collider vehicleCollider;

	public Collider additionalCollider;

	public Transform vehicleLoadingPosition;

	public SleepEnvironment sleepEnvironment;

	[BoxGroup("Enter Exit Sounds")]
	public bool hasEnterSound;

	[BoxGroup("Enter Exit Sounds")]
	[ShowIf("hasEnterSound")]
	public SoundType EnterSound;

	[BoxGroup("Enter Exit Sounds")]
	public bool hasExitSound;

	[BoxGroup("Enter Exit Sounds")]
	[ShowIf("hasExitSound")]
	public SoundType ExitSound;

	private bool _wasVehicleVisibleLastFrame;

	private Vector3 _lastFoundNavMeshTargetPosition;

	private float _lastFoundNavMeshTargetPositionTime;

	private readonly List<Vector3> _navMeshTargetInitialPositions = new List<Vector3>();

	private Rigidbody _rigidbody;

	private VariableCenterOfMass _variableCenterOfMass;

	public Vector3 FrontPoint => base.transform.position + base.transform.forward * (vehicleCollider.bounds.size.z * 0.5f);

	public Vector3 BackPoint => base.transform.position - base.transform.forward * (vehicleCollider.bounds.size.z * 0.5f);

	protected bool ShouldLightsBeOn
	{
		get
		{
			if ((bool)InstanceBehavior<GameManager>.Instance && !InstanceBehavior<GameManager>.Instance.IsUIDevScene && (RainHelper.AreRainDropsFalling || InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn()))
			{
				return controlledByPlayer;
			}
			return false;
		}
	}

	public bool IsVisible => renderers[0].isVisible;

	protected override int DefaultLayer => LayerHelper.VehiclesLayerIndex;

	protected override Renderer[] OutlineRenderers
	{
		get
		{
			if (CarFeatures != null)
			{
				Renderer[] bodyMeshes = CarFeatures.bodyMeshes;
				if (bodyMeshes != null && bodyMeshes.Length > 0)
				{
					return CarFeatures.bodyMeshes;
				}
			}
			return base.OutlineRenderers;
		}
	}

	public virtual float CurrentSpeed => 0f;

	public CarFeatures CarFeatures { get; private set; }

	public LanguageChangeEventDataHolder GetName()
	{
		return vehicleInstance.vehicleTypeName.Localize();
	}

	public override void Awake()
	{
		base.Awake();
		SetVehicleInstance(vehicleInstance);
		VehicleHelper.RegisterPlayerVehicle(this);
		CarFeatures = GetComponent<CarFeatures>();
		TryGetComponent<Rigidbody>(out _rigidbody);
		TryGetComponent<VariableCenterOfMass>(out _variableCenterOfMass);
	}

	private void OnCurrentHeightChanged()
	{
		if (!BuildingManager.IsInsideBuilding || vehicleInstance.Address != InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address)
		{
			return;
		}
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if (!(multipleHeightsBuildingController == null))
		{
			if (multipleHeightsBuildingController.GetPositionVisible(base.transform.position))
			{
				Show();
			}
			else
			{
				Hide();
			}
		}
	}

	public virtual void UpdateCamera()
	{
		if (ShouldUseVehicleCam())
		{
			CameraHelper.SetCamera((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCamera : InstanceBehavior<GameManager>.Instance.vehicleCamera);
		}
	}

	public bool ShouldUseVehicleCam()
	{
		if (!vehicleType.spawnInPlayerObject)
		{
			return !vehicleType.usePedestrianCam;
		}
		return false;
	}

	public virtual void Reset()
	{
	}

	public override void Start()
	{
		base.Start();
		SetNavMeshTargetInitialPositions();
		GlobalEvents.onSaveGame = (Action)Delegate.Combine(GlobalEvents.onSaveGame, new Action(SavePosition));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Combine(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
		vehicleInstance.AddCallToOnItemsInCargoUpdated(OnItemAddedToCargo);
		SetDirtiness(vehicleInstance.dirtiness);
	}

	private void OnExitBuilding(Address address)
	{
		if (vehicleInstance.Address != address)
		{
			return;
		}
		Building building = BuildingHelper.GetBuildingRegistration(address)?.BuildingCached;
		if (!(building != null) || !building.IsHamptonsHouse())
		{
			bool flag = vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle);
			if ((!flag || vehicleInstance.cargoInstances.Count <= 0) && flag)
			{
				vehicleInstance.SetStreetData(string.Empty, 0);
			}
			UnityEngine.Object.Destroy(base.gameObject);
			vehicleInstance.lastSeen = TimeHelper.Now();
		}
	}

	private void OnItemAddedToCargo()
	{
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.HandTruckAddBox, InstanceBehavior<GameManager>.Instance.playerController.transform.position);
		UpdateCargoCount();
	}

	protected virtual void Update()
	{
		if (controlledByPlayer)
		{
			BridgeTriggerController.UpdateStatusPeriodically();
		}
		if (_wasVehicleVisibleLastFrame != IsVisible)
		{
			_wasVehicleVisibleLastFrame = IsVisible;
			if (_wasVehicleVisibleLastFrame)
			{
				vehicleInstance.lastSeen = null;
			}
			else
			{
				vehicleInstance.lastSeen = TimeHelper.Now();
			}
		}
	}

	private void OnDisable()
	{
		if (!PlayerHelper.IsUsingVehicle || !(SaveGameManager.Current.ActiveVehicleId == vehicleInstance.id))
		{
			VehicleHelper.UnregisterPlayerVehicle(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)poi)
		{
			UnityEngine.Object.Destroy(poi.gameObject);
		}
		VehicleHelper.UnregisterPlayerVehicle(this);
		if (vehicleInstance != null)
		{
			vehicleInstance.RemoveCallFromOnItemsInCargoUpdated(OnItemAddedToCargo);
		}
		GlobalEvents.onSaveGame = (Action)Delegate.Remove(GlobalEvents.onSaveGame, new Action(SavePosition));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onCurrentHeightChanged = (Action)Delegate.Remove(GlobalEvents.onCurrentHeightChanged, new Action(OnCurrentHeightChanged));
	}

	public virtual void UpdateCargoCount()
	{
	}

	private void SetNavMeshTargetInitialPositions()
	{
		Transform[] array = navMeshTargets;
		foreach (Transform transform in array)
		{
			_navMeshTargetInitialPositions.Add(transform.localPosition);
		}
	}

	private void ResetNavMeshTargetPositionsToInitial()
	{
		if (_navMeshTargetInitialPositions.Count != 0)
		{
			for (int i = 0; i < navMeshTargets.Length; i++)
			{
				navMeshTargets[i].localPosition = _navMeshTargetInitialPositions[i];
			}
		}
	}

	public override void OnIoEnter()
	{
		if (CityMap.IsOpen || !(VehicleHelper.GetCurrentVehicleBase() == this))
		{
			base.OnIoEnter();
		}
	}

	public override bool OnIoLeftClick()
	{
		UpdateNavMeshTargets();
		return base.OnIoLeftClick();
	}

	public override void UpdateNavMeshTargets()
	{
		if ((bool)this && base.isActiveAndEnabled)
		{
			ResetNavMeshTargetPositionsToInitial();
			base.UpdateNavMeshTargets();
		}
	}

	public virtual void EnterVehicle()
	{
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		if (InstanceBehavior<OverlayManager>.Instance.IsDetailedOverlayActive)
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		}
		if (PlayerHelper.IsHoldingItem && !vehicleType.HasTag(TagRef.Vehicletag.isscooter))
		{
			Item itemInHands = PlayerHelper.ItemInHands;
			if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbox))
			{
				if (!ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(PlayerHelper.ItemInstanceInHands, vehicleInstance) || PlayerHelper.IsHoldingItem)
				{
					return;
				}
			}
			else
			{
				if (!vehicleInstance.TryToAddToCargo(PlayerHelper.ItemInstanceInHands.ConvertToCargoInstance()))
				{
					return;
				}
				PlayerHelper.ItemInstanceInHands = null;
			}
			OnItemAddedToCargo();
		}
		OnIoExit();
		PlayerDances.Disable();
		InstanceBehavior<GameManager>.Instance.playerController.HideAccessoryVisuals(AccessoryType.Hand);
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = LayerHelper.PlayerVehiclesLayerIndex;
		}
		SaveGameManager.Current.ActiveVehicleId = vehicleInstance.id;
		controlledByPlayer = true;
		if (!vehicleType.spawnInPlayerObject && !vehicleType.usePedestrianCam)
		{
			UpdateCamera();
			base.gameObject.layer = LayerHelper.VehiclesLayerIndex;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			string text = InstanceBehavior<BuildingManager>.Instance?.cityBuildingController?.building.StreetName ?? string.Empty;
			int valueOrDefault = (InstanceBehavior<BuildingManager>.Instance?.cityBuildingController?.building.StreetNumber).GetValueOrDefault();
			vehicleInstance.SetStreetData(text, valueOrDefault);
		}
		else if (UndergroundParkingManager.IsInsideParking)
		{
			vehicleInstance.SetStreetData("ba:street_parking", UndergroundParkingManager.previousParkingNumber);
		}
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVehicle(this);
		GlobalEvents.onEnterVehicle?.Invoke(this);
		GameEvent.Invoke("ba:gameevent_enteredvehicle");
		InstanceBehavior<GameManager>.Instance.playerController.Character.LookTarget = Vector3.zero;
		InstanceBehavior<GameManager>.Instance.playerController.Character.UpdateZombieHoldingBoxState();
		poi?.SetHidden(hide: true);
		if (hasEnterSound)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(EnterSound, base.transform.position);
		}
		navMeshObstacle.carving = false;
		GameAnalytics.TrackEnterVehicle(vehicleInstance.vehicleTypeName);
		if (poi != null)
		{
			poi.SetBackground(InstanceBehavior<GlobalReferences>.Instance.vehiclePOIBackgroundColor);
		}
		vehicleInstance.AddCallToOnItemsInCargoUpdated(InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetupCargoSlots);
		ParkingLaneGenerator.RegenerateAutoParkSpotsNear(base.transform.position);
	}

	public virtual void ExitVehicle()
	{
		SetParkingData();
		UpdateNavMeshTargets();
		SaveGameManager.Current.ActiveVehicleId = null;
		controlledByPlayer = false;
		SavePosition();
		GlobalEvents.onExitVehicle?.Invoke(this);
		poi?.SetHidden(hide: false);
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = DefaultLayer;
		}
		CameraHelper.SetCamera((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorCamera : InstanceBehavior<GameManager>.Instance.pedestrianCamera);
		if (BuildingManager.IsInsideBuilding)
		{
			Building building = InstanceBehavior<BuildingManager>.Instance?.building;
			string text = building?.StreetName ?? string.Empty;
			int number = building?.StreetNumber ?? 0;
			vehicleInstance.SetStreetData(text, number);
			if (building != null && building.IsHamptonsHouse())
			{
				poi?.SetHidden(hide: true);
			}
		}
		else if (UndergroundParkingManager.IsInsideParking)
		{
			string currentStreetName = SaveGameManager.Current.CurrentStreetName;
			int currentStreetNumber = SaveGameManager.Current.CurrentStreetNumber;
			vehicleInstance.SetStreetData(currentStreetName, currentStreetNumber);
		}
		if (hasExitSound)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(ExitSound, base.transform.position);
		}
		navMeshObstacle.carving = true;
		InstanceBehavior<GameManager>.Instance.playerController.Character.UpdateZombieHoldingBoxState();
		PlayerDances.Enable();
		InstanceBehavior<GameManager>.Instance.playerController.ShowHandAccessoryVisualsIfRequired();
		if (poi != null && vehicleInstance.parkingState == ParkingState.Illegal)
		{
			poi.SetBackground(InstanceBehavior<GlobalReferences>.Instance.vehicleIllegalParkingPOIBackgroundColor);
		}
		if (PurchaseUI.IsPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close(false);
		}
		vehicleInstance.RemoveCallFromOnItemsInCargoUpdated(InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetupCargoSlots);
		GamePromptManager.Instance.TriggerEvent("EXIT_VEHICLE", vehicleType.name);
		ParkingLaneGenerator.RegenerateAutoParkSpotsNear(base.transform.position);
	}

	private static void SetParkingData()
	{
		if (InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.vehicleInfo.isActiveAndEnabled)
		{
			VehicleInfoPanel vehicleInfo = InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.vehicleInfo;
			if (vehicleInfo.isCommonVehicle)
			{
				InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.parkingState = vehicleInfo.currentParkingState;
				InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.parkingNeighbourhood = vehicleInfo.currentParkingNeighbourhood;
			}
		}
	}

	public virtual void SavePosition()
	{
		SavePosition(base.transform.position, base.transform.rotation);
	}

	private void SavePosition(Vector3 position, Quaternion rotation)
	{
		vehicleInstance.position = position;
		vehicleInstance.rotation = rotation;
	}

	public virtual void SetVehicleInstance(VehicleInstance vehicleInstance)
	{
		if (vehicleInstance == null)
		{
			return;
		}
		this.vehicleInstance = vehicleInstance;
		UnityEngine.Object.Destroy(GetComponent<RandomVehicleColor>());
		if (!string.IsNullOrEmpty(vehicleInstance.vehicleColorName) && CarFeatures != null)
		{
			if (!VehicleHelper.TryGetVehicleColor(vehicleInstance.vehicleColorName, out var resultVehicleColor))
			{
				resultVehicleColor = InstanceBehavior<GlobalReferences>.Instance.vehicleColors[0];
			}
			CarFeatures.SetColor(resultVehicleColor);
		}
		if (vehicleType.autoDestroyAfterMinutes >= 0)
		{
			AutoDestroyVehicle orAddComponent = base.gameObject.GetOrAddComponent<AutoDestroyVehicle>();
			orAddComponent.vehicleController = this;
			orAddComponent.minimumMinutesBeforeDestroy = vehicleType.autoDestroyAfterMinutes;
			orAddComponent.Update();
		}
		UpdateCargoCount();
	}

	public override void SecondaryInteract()
	{
	}

	public override Vector3 GetClosestNavMeshTargetPosition(Vector3 entityPosition)
	{
		if ((bool)vehicleLoadingPosition && (((bool)InstanceBehavior<GameManager>.Instance.selectedVehicle && InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.spawnInPlayerObject) || PlayerHelper.ItemInstanceInHands != null) && NavMesh.SamplePosition(vehicleLoadingPosition.position, out var hit, 3f, -1))
		{
			return hit.position;
		}
		return base.GetClosestNavMeshTargetPosition(entityPosition);
	}

	public virtual float GetCurrentFuel()
	{
		return 0f;
	}

	public virtual float GetCurrentCondition()
	{
		return 0f;
	}

	public virtual void AddFuel(float value)
	{
	}

	public virtual void SetFuel(float value)
	{
	}

	public virtual void Repair()
	{
		vehicleInstance.damage = 0f;
		vehicleInstance.deformations.Clear();
	}

	public void SetDirtiness(float dirtiness)
	{
		vehicleInstance.dirtiness = dirtiness;
		if (CarFeatures != null)
		{
			CarFeatures.SetDirtiness(vehicleInstance.dirtiness);
		}
	}

	public virtual void SetFreeze(bool isFrozen)
	{
		if (!(_rigidbody == null))
		{
			_rigidbody.constraints = (isFrozen ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None);
			if (!isFrozen && _variableCenterOfMass != null)
			{
				_variableCenterOfMass.UpdateAllProperties();
			}
		}
	}

	private static bool IsPositionReachable(Vector3 position)
	{
		if (position != Vector3.zero)
		{
			return InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(position);
		}
		return false;
	}

	private bool CanWarpToVehicle()
	{
		return Vector3.Distance(PlayerHelper.GetPosition(), base.transform.position) <= 2.5f;
	}

	public void MoveAndAddHeldItemToStorage()
	{
		Vector3 manageStoragePosition = GetManageStoragePosition();
		if (!IsPositionReachable(manageStoragePosition) && CanWarpToVehicle())
		{
			AddHeldItemToStorage();
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(manageStoragePosition, AddHeldItemToStorage);
		}
	}

	private void AddHeldItemToStorage()
	{
		if (!PlayerHelper.IsHoldingItem)
		{
			return;
		}
		if (PlayerHelper.IsHoldingShoppingBasket || PlayerHelper.IsHoldingAMop)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "fromname", vehicleType.vehicleTypeName },
				{
					"toname",
					PlayerHelper.ItemInstanceInHands.itemName
				}
			};
			Notifications.Show(NotificationType.Warning, "notification_cant_interact_with_item_in_hand", notificationData);
			return;
		}
		Item itemInHands = PlayerHelper.ItemInHands;
		if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbox))
		{
			ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(PlayerHelper.ItemInstanceInHands, vehicleInstance);
		}
		else if (vehicleInstance.TryToAddToCargo(PlayerHelper.ItemInstanceInHands.ConvertToCargoInstance()))
		{
			PlayerHelper.ItemInstanceInHands = null;
		}
	}

	public void MoveAndAddHandTruckItemsToStorage()
	{
		Vector3 manageStoragePosition = GetManageStoragePosition();
		if (!IsPositionReachable(manageStoragePosition) && CanWarpToVehicle())
		{
			AddHandTruckItemsToStorage();
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(manageStoragePosition, AddHandTruckItemsToStorage);
		}
	}

	private void AddHandTruckItemsToStorage()
	{
		if (PlayerHelper.IsUsingVehicle)
		{
			ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(VehicleHelper.GetCurrentVehicle(), vehicleInstance);
		}
	}

	public void MoveAndAddCartToStorage()
	{
		Vector3 manageStoragePosition = GetManageStoragePosition();
		if (!IsPositionReachable(manageStoragePosition) && CanWarpToVehicle())
		{
			AddCartToStorage();
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(manageStoragePosition, AddCartToStorage);
		}
	}

	private void AddCartToStorage()
	{
		HandTruck componentInChildren = InstanceBehavior<GameManager>.Instance.playerController.GetComponentInChildren<HandTruck>();
		if ((bool)componentInChildren && !string.IsNullOrEmpty(componentInChildren.vehicleType.itemVersion) && ((componentInChildren.vehicleType.HasTag(TagRef.Vehicletag.canbestoredbig) && vehicleType.fitsFlatbed) || (componentInChildren.vehicleType.HasTag(TagRef.Vehicletag.canbestoredsmall) && vehicleType.fitsHandTruck)))
		{
			CargoInstance cargoInstance = new CargoInstance(componentInChildren.vehicleType.itemVersion, 1, 0f);
			if (!vehicleInstance.TryToAddToCargo(cargoInstance))
			{
				Notifications.ShowError("notification_storage_is_full");
				return;
			}
			componentInChildren.ExitVehicle();
			SaveGameManager.Current.VehicleInstances.Remove(componentInChildren.vehicleInstance);
			UnityEngine.Object.Destroy(componentInChildren.gameObject);
		}
	}

	public void MoveAndManageStorage()
	{
		Vector3 manageStoragePosition = GetManageStoragePosition();
		if (!IsPositionReachable(manageStoragePosition) && CanWarpToVehicle())
		{
			ManageStorage();
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(manageStoragePosition, ManageStorage);
		}
	}

	private void ManageStorage()
	{
		if (!PurchaseUI.IsPanelOpen)
		{
			if (PlayerHelper.IsHoldingShoppingBasket)
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string>
				{
					{ "fromname", vehicleInstance.vehicleTypeName },
					{
						"toname",
						PlayerHelper.ItemInstanceInHands.itemName
					}
				};
				Notifications.Show(NotificationType.Warning, "notification_cant_use_while_using_item", notificationData);
			}
			else
			{
				InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Show(vehicleInstance);
			}
		}
	}

	public void DriveVehicle()
	{
		Vector3 vector = GetDrivingEntrancePosition();
		if (vector == Vector3.zero)
		{
			if (Vector3.Distance(PlayerHelper.GetPosition(), base.transform.position) <= 2.5f)
			{
				EnterVehicle();
				return;
			}
			vector = navMeshTargets[0].position;
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(vector, EnterVehicle);
	}

	protected virtual Vector3 GetDrivingEntrancePosition()
	{
		Vector3 position = navMeshTargets[0].position;
		Vector3 position2 = PlayerHelper.GetPosition();
		if (NavMesh.SamplePosition(position, out var hit, 3f, -1))
		{
			NavMeshPath navMeshPath = new NavMeshPath();
			NavMesh.CalculatePath(position2, hit.position, PlayerController.navMeshQueryFilter, navMeshPath);
			if (navMeshPath.status == NavMeshPathStatus.PathComplete)
			{
				return hit.position;
			}
		}
		return GetClosestNavMeshTargetPosition(position2);
	}

	public Vector3 GetManageStoragePosition()
	{
		if (!(vehicleLoadingPosition != null))
		{
			return GetClosestNavMeshTargetPosition(InstanceBehavior<GameManager>.Instance.playerController.transform.position);
		}
		return vehicleLoadingPosition.position;
	}
}
