using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Indoors;
using Buildings.Indoors.InteriorDesign;
using Buildings.Outdoors;
using Buildings.Retail.Businesses.Gym;
using BusinessLayoutSets;
using Controllers;
using EmployeeStations;
using Entities;
using Extensions;
using Helpers;
using HorizonBasedAmbientOcclusion.HighDefinition;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Player.HUD.ItemInfoOverlays;
using Seasons;
using Streets;
using UI;
using UI.Guiders;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vehicles;

public class BuildingManager : InstanceBehavior<BuildingManager>
{
	public static readonly Dictionary<BuildingSizeInfo, Dictionary<string, SerializedInteriorDesign>> DefaultInteriorDesigns = new Dictionary<BuildingSizeInfo, Dictionary<string, SerializedInteriorDesign>>();

	public const int NpcAgentTypeId = 0;

	public const int PlayerAgentTypeId = 1479372276;

	public const float BuildingFadeDuration = 0.2f;

	private const int SafeExitPositionChecks = 10;

	private const float SafeExitForwardStep = 0.2f;

	private const float SafeExitOverlapRadius = 0.45f;

	private const float SafeExitOverlapHeight = 0.9f;

	public CustomerSpawner customerSpawner;

	public List<ExitZone> exitZones;

	private List<ItemController> _allItemControllers;

	public List<VehicleController> allVehicleControllers;

	public bool isOpen;

	[HideInInspector]
	public UnityEvent<ItemController> onEmptyProducer = new UnityEvent<ItemController>();

	[NonSerialized]
	[ShowNonSerializedField]
	public BuildingRegistration buildingRegistration;

	public CityBuildingController cityBuildingController;

	public Building building;

	public BusinessType businessType;

	private bool? _isWorking;

	public Transform currentBuildingVersion;

	public MultipleHeightsBuildingController multipleHeightsBuildingController;

	private BuildingContext _cachedActiveBuildingContext;

	private readonly Dictionary<Address, BuildingContext> _buildingContextByAddress = new Dictionary<Address, BuildingContext>();

	[HideInInspector]
	public UnityEvent<DirtSpotObject> onFloorCellClick = new UnityEvent<DirtSpotObject>();

	public UnityEvent<string, string> onUniformChanged = new UnityEvent<string, string>();

	public Transform currentLayout;

	private AudioSource _ambianceAudioSource;

	public List<BuildingInteriorSound> interiorSounds = new List<BuildingInteriorSound>();

	public List<BuildingEnterSound> enterSounds = new List<BuildingEnterSound>();

	public List<BuildingEnterSound> exitSounds = new List<BuildingEnterSound>();

	public Volume indoorVolume;

	public Volume parkingVolume;

	public Volume casinoVolume;

	public Light extraIndoorsDirectionalLight;

	public Light indoorsShadowsDirectionalLight;

	public Bounds interiorBounds;

	public bool enteringBuilding;

	public bool exitingBuilding;

	private bool _hamptonsReloadInProgress;

	public LayoutScreenshotGenerator layoutScreenshotGenerator;

	public Transform visualsContainer;

	[SerializeField]
	private Transform indoorItemContainer;

	private readonly List<DirtSpotObject> _cachedDirtSpotObjects = new List<DirtSpotObject>();

	[HideInInspector]
	public HDAdditionalLightData extraIndoorsDirectionalLightData;

	[HideInInspector]
	public HBAO indoorsHbao;

	private bool _scheduledUpdateAvailableProducers;

	private readonly HashSet<Collider> _colliderResultList = new HashSet<Collider>();

	private readonly Collider[] _colliderResults = new Collider[20];

	public static bool isBuildingTemporarilyEditable;

	public static bool ignoreSeasons;

	public List<ItemController> allItemControllers
	{
		get
		{
			if (building == null)
			{
				return _allItemControllers;
			}
			if (building.IsHamptonsHouse() && !InteriorDesignerHelper.BlueprintCreatorMode)
			{
				return ((HamptonsHouse)multipleHeightsBuildingController).allItemControllers;
			}
			return _allItemControllers;
		}
		private set
		{
			if (building == null)
			{
				_allItemControllers = value;
			}
			if (!building.IsHamptonsHouse() || InteriorDesignerHelper.BlueprintCreatorMode)
			{
				_allItemControllers = value;
			}
			else
			{
				((HamptonsHouse)multipleHeightsBuildingController).allItemControllers = value;
			}
		}
	}

	public bool IsPlayerOwnedBusiness => buildingRegistration?.RentedByPlayer ?? false;

	[field: SerializeField]
	public BuildingSizeResolver BuildingSizeResolver { get; private set; }

	public Transform IndoorItemContainer
	{
		get
		{
			if (multipleHeightsBuildingController != null && multipleHeightsBuildingController is HamptonsHouse hamptonsHouse)
			{
				return hamptonsHouse.itemsContainer;
			}
			return indoorItemContainer;
		}
	}

	public static bool IsInsideBuilding
	{
		get
		{
			if (InstanceBehavior<BuildingManager>.IsInitialized)
			{
				return (object)InstanceBehavior<BuildingManager>.Instance.building != null;
			}
			return false;
		}
	}

	public static bool CanBuildOnCurrentBuilding
	{
		get
		{
			if (IsInsideBuilding)
			{
				if (!InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
				{
					return isBuildingTemporarilyEditable;
				}
				return true;
			}
			return false;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (!base.IsMainInstance)
		{
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		ignoreSeasons = false;
		DefaultInteriorDesigns.Clear();
		BlueprintsFolderLoader.Init();
		if (indoorVolume != null)
		{
			indoorVolume.enabled = false;
			indoorVolume.profile.TryGet<HBAO>(out indoorsHbao);
		}
		else if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			UnityEngine.Debug.LogError("No indoor volume found on BuildingManager");
		}
		if ((bool)extraIndoorsDirectionalLight)
		{
			extraIndoorsDirectionalLight.gameObject.SetActive(value: false);
			extraIndoorsDirectionalLightData = extraIndoorsDirectionalLight.GetComponent<HDAdditionalLightData>();
		}
		if ((bool)indoorsShadowsDirectionalLight)
		{
			indoorsShadowsDirectionalLight.gameObject.SetActive(value: false);
			GlobalEvents.indoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.indoorLightsStatusChanged, (Action<bool>)delegate(bool lightsOn)
			{
				if (IsInsideBuilding && !building.IsHamptonsHouse())
				{
					indoorsShadowsDirectionalLight.gameObject.SetActive(lightsOn);
				}
			});
		}
		_ambianceAudioSource = GetComponent<AudioSource>();
		building = null;
		buildingRegistration = null;
		businessType = null;
		InvalidateActiveBuildingContextCache();
		if (InstanceBehavior<GameManager>.Instance == null || InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			return;
		}
		BuildingCleanlinessHelper.HideDirtinessHighlighting();
		GlobalEvents.onSaveGame = (Action)Delegate.Combine(GlobalEvents.onSaveGame, (Action)delegate
		{
			if (IsInsideBuilding)
			{
				SerializeInteriorDesign();
			}
		});
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, (Action)delegate
		{
			if (IsInsideBuilding)
			{
				RunCurrentBuildingHourly();
			}
		});
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeEnd));
		PlacementSystem.onItemPlaced = (Action)Delegate.Combine(PlacementSystem.onItemPlaced, new Action(ForceUpdateAvailableProducers));
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(buildingRegistration.UpdateSecurityLevel));
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Combine(GlobalEvents.onItemDropped, (Action<ItemController>)delegate
		{
			OnItemChanged();
		});
		GlobalEvents.onItemGrabbed = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemGrabbed, (Action<ItemInstance>)delegate
		{
			OnItemChanged();
		});
		GlobalEvents.onItemDiscarded = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemDiscarded, (Action<ItemInstance>)delegate
		{
			OnItemChanged();
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			parkingVolume.enabled = !toggled;
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			casinoVolume.enabled = !toggled;
		});
		NightclubBusinessHelper.Init();
		GymBusinessHelper.Init();
		CasinoBusinessHelper.Init();
		UnityEngine.Debug.Log($"[Stopwatch] BuildingManager Awake: {stopwatch.ElapsedMilliseconds}ms. ");
		stopwatch.Stop();
	}

	protected override void OnDestroy()
	{
		if (base.IsMainInstance)
		{
			base.OnDestroy();
			PlacementSystem.onPlacementModeEnd = (Action)Delegate.Remove(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeEnd));
			PlacementSystem.onItemPlaced = (Action)Delegate.Remove(PlacementSystem.onItemPlaced, new Action(ForceUpdateAvailableProducers));
			InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(buildingRegistration.UpdateSecurityLevel));
		}
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (IsInsideBuilding)
		{
			SetWorkingState();
			if (address == buildingRegistration.Address)
			{
				_cachedActiveBuildingContext?.RegenerateFieldsOnRegistrationChange();
			}
			_buildingContextByAddress.Remove(address);
		}
	}

	private void InvalidateActiveBuildingContextCache()
	{
		_cachedActiveBuildingContext = null;
	}

	public static BuildingContext CreateBuildingContextFromItemInstance(ItemInstance itemInstance)
	{
		Address addressCached = itemInstance.AddressCached;
		if (InstanceBehavior<BuildingManager>.Instance._buildingContextByAddress.TryGetValue(addressCached, out var value))
		{
			return value;
		}
		Building obj = BuildingHelper.GetBuilding(addressCached);
		BuildingRegistration registration = (InteriorDesignerHelper.BlueprintCreatorMode ? InstanceBehavior<BuildingManager>.Instance.buildingRegistration : BuildingHelper.GetBuildingRegistration(addressCached));
		BusinessType data = BusinessTypeHelper.GetData(registration);
		MultipleHeightsBuildingController multipleHeights = null;
		CityBuildingController cityBuildingController = ((InstanceBehavior<CityManager>.Instance != null) ? InstanceBehavior<CityManager>.Instance.FindCityBuildingController(addressCached) : null);
		if (cityBuildingController != null)
		{
			multipleHeights = cityBuildingController.GetComponent<MultipleHeightsBuildingController>();
		}
		BuildingContext buildingContext = new BuildingContext(obj, registration, data, multipleHeights);
		InstanceBehavior<BuildingManager>.Instance._buildingContextByAddress[addressCached] = buildingContext;
		return buildingContext;
	}

	public static BuildingContext CreateBuildingContextFromActiveManager()
	{
		if (InstanceBehavior<BuildingManager>.Instance == null)
		{
			return null;
		}
		if (InstanceBehavior<BuildingManager>.Instance._cachedActiveBuildingContext != null)
		{
			return InstanceBehavior<BuildingManager>.Instance._cachedActiveBuildingContext;
		}
		InstanceBehavior<BuildingManager>.Instance._cachedActiveBuildingContext = new BuildingContext(InstanceBehavior<BuildingManager>.Instance.building, InstanceBehavior<BuildingManager>.Instance.buildingRegistration, InstanceBehavior<BuildingManager>.Instance.businessType, InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController);
		return InstanceBehavior<BuildingManager>.Instance._cachedActiveBuildingContext;
	}

	public void OnItemChanged(bool forced = false)
	{
		if (IsInsideBuilding && buildingRegistration.RentedByPlayer && !isBuildingTemporarilyEditable)
		{
			ScheduleUpdateAvailableProducers();
			if ((forced || !InteriorDesignerUI.IsOpen) && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.HasValidAddress)
			{
				BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
				BusinessHelper.UpdatePromotion(buildingRegistration);
				GlobalEvents.onBuildingRegistrationChange?.Invoke(buildingRegistration.Address);
				buildingRegistration.UpdateEmployeesAssignedWorkStationItems();
			}
		}
	}

	public bool EnterBuildingWithVehicle(CityBuildingController cbc, bool inverseVehicleRotation, int vehicleSlot)
	{
		GenericPersonalGoal genericPersonalGoal = InstanceBehavior<GameManager>.Instance.personalGoals.Find((GenericPersonalGoal x) => x.rewards.Exists((Reward r) => r is UnlockBuilding unlockBuilding && unlockBuilding.Address == cbc.building.Address));
		if ((bool)genericPersonalGoal && !SaveGameManager.Current.completedPersonalGoals.Contains(genericPersonalGoal.identifier))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "goal", genericPersonalGoal.title } };
			Notifications.Show(NotificationType.Error, "personal_goal_notification_personal_goal_required_to_enter_building", notificationData, 4f, "Goal" + genericPersonalGoal.title + "Required");
			return false;
		}
		if (!BuildingHelper.CanEnterBuilding(cbc.building.Address))
		{
			Notifications.ShowError("buildingmanager_notification_business_closed", "BusinessCurrentlyClosed");
			return false;
		}
		if (IsBuildingBlockedByAnyService(cbc.building.Address))
		{
			Notifications.ShowError("cant_enter_building_while_interior_installation", "cant_enter_building_while_interior_installation");
			return false;
		}
		if (IsWarehouseFull(cbc))
		{
			Notifications.ShowError("buildingmanager_notification_no_free_spot");
			return false;
		}
		if (BuildingHelper.VehicleSlotIsUsed(cbc, vehicleSlot))
		{
			Notifications.ShowError("buildingmanager_notification_warehouse_gate_is_bocked");
			return false;
		}
		if (BuildingHelper.IsAnyCarBlockingTheEntrance(InstanceBehavior<GameManager>.Instance.selectedVehicle, vehicleSlot, cbc.building))
		{
			Notifications.ShowError("buildingmanager_notification_warehouse_gate_is_bocked");
			return false;
		}
		((CarController)InstanceBehavior<GameManager>.Instance.selectedVehicle).Reset();
		return EnterBuilding(cbc.building, useSaveGamePlayerPosition: false, inverseVehicleRotation, vehicleSlot);
	}

	public static bool IsBuildingBlockedByAnyService(Address address)
	{
		if (!IsBuildingUnderInteriorInstallation(address) && !IsVehicleDeliveryInProgress(address))
		{
			return IsAMovingInProgress(address);
		}
		return true;
	}

	private static bool IsAMovingInProgress(Address address)
	{
		foreach (MovingServiceContract movingServiceContract in SaveGameManager.Current.movingServiceContracts)
		{
			if ((movingServiceContract.originMovingAddress == address || movingServiceContract.destinationMovingAddress == address) && movingServiceContract.movingDay - 1 <= SaveGameManager.Current.Day)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsVehicleDeliveryInProgress(Address address)
	{
		foreach (VehicleDeliveryContract vehicleDeliveryContract in SaveGameManager.Current.vehicleDeliveryContracts)
		{
			if (vehicleDeliveryContract.deliveryAddress == address && new Timestamp(vehicleDeliveryContract.deliveryDay, vehicleDeliveryContract.deliveryHour, 0f).IsInTheFuture())
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsBuildingUnderInteriorInstallation(Address address)
	{
		foreach (InteriorInstallationFirmContract interiorInstallationFirmContract in SaveGameManager.Current.interiorInstallationFirmContracts)
		{
			if (interiorInstallationFirmContract.addressToDoTheInstallation == address && interiorInstallationFirmContract.dayOfInstallation - 1 <= SaveGameManager.Current.Day)
			{
				return true;
			}
		}
		return false;
	}

	public static void RefreshHamptonsHouseBlockerCollider(Address address)
	{
		if (BuildingHelper.GetBuildingRegistration(address).BuildingCached.IsHamptonsHouse() && InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address) is CityHamptonsHouseController cityHamptonsHouseController)
		{
			cityHamptonsHouseController.RefreshBlockerCollider();
		}
	}

	public static void RequestHamptonsItemReloadIfLoaded(Address address, bool applyInterior = false, bool withFade = true)
	{
		HamptonsHouse hamptonsHouse = (InstanceBehavior<CityManager>.Instance.FindCityBuildingController(address) as CityHamptonsHouseController).hamptonsHouse;
		if (hamptonsHouse.IsHouseLoaded)
		{
			if (InstanceBehavior<BuildingManager>.Instance._hamptonsReloadInProgress || !withFade)
			{
				InstanceBehavior<BuildingManager>.Instance.StartCoroutine(InstanceBehavior<BuildingManager>.Instance.HamptonsReloadOnlyRoutine(hamptonsHouse, applyInterior));
				return;
			}
			InstanceBehavior<BuildingManager>.Instance._hamptonsReloadInProgress = true;
			InstanceBehavior<BuildingManager>.Instance.StartCoroutine(InstanceBehavior<BuildingManager>.Instance.HamptonsReloadWithFadeRoutine(hamptonsHouse, applyInterior));
		}
	}

	private IEnumerator HamptonsReloadOnlyRoutine(HamptonsHouse hamptonsHouse, bool applyInterior)
	{
		yield return hamptonsHouse.ReloadHouseCoroutine(applyInterior);
	}

	private IEnumerator HamptonsReloadWithFadeRoutine(HamptonsHouse hamptonsHouse, bool applyInterior)
	{
		PlayerController player = InstanceBehavior<GameManager>.Instance.playerController;
		player.ResetNavigation();
		player.SetNavigationBlocker(NavigationBlocker.HamptonsItemReload);
		VehicleController vehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (vehicle != null && vehicle.vehicleType.IsMotorVehicle)
		{
			vehicle.SetFreeze(isFrozen: true);
		}
		yield return UiFader.Fade(0.4f, "black_screen_services_info");
		yield return hamptonsHouse.ReloadHouseCoroutine(applyInterior);
		yield return new WaitForSecondsRealtime(1f);
		if (vehicle != null && vehicle.vehicleType.IsMotorVehicle)
		{
			vehicle.SetFreeze(isFrozen: false);
		}
		player.UnsetNavigationBlocker(NavigationBlocker.HamptonsItemReload);
		yield return UiFader.UnFade();
		_hamptonsReloadInProgress = false;
	}

	public bool EnterBuilding(Building buildingToEnter, bool useSaveGamePlayerPosition = false, bool inverseVehicleRotation = false, int vehicleSlot = 0, int entranceDoorId = -1)
	{
		if (enteringBuilding)
		{
			return true;
		}
		if (buildingToEnter.IsHamptonsHouse())
		{
			EnterHamptonsBuilding(buildingToEnter, useSaveGamePlayerPosition);
		}
		else
		{
			StartCoroutine(EnterBuildingCoroutine(buildingToEnter, useSaveGamePlayerPosition, inverseVehicleRotation, vehicleSlot, entranceDoorId));
		}
		return true;
	}

	private IEnumerator EnterBuildingCoroutine(Building buildingToEnter, bool useSaveGamePlayerPosition = false, bool inverseVehicleRotation = false, int vehicleSlot = 0, int entranceDoorId = -1)
	{
		enteringBuilding = true;
		if (!EnergyHelper.goingToHospital)
		{
			yield return UiFader.Fade(0.2f);
			yield return null;
		}
		if (IsInsideBuilding)
		{
			yield return ExitFromBuildingCoroutine(0, playFadeAnimation: false);
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			yield return UndergroundParkingManager.ExitParkingCoroutine(playFadeAnimation: false);
		}
		InvalidateActiveBuildingContextCache();
		building = buildingToEnter;
		if (cityBuildingController == null || cityBuildingController.building != building)
		{
			cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(building.Address);
		}
		if (cityBuildingController != null)
		{
			InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(cityBuildingController.entranceDoors[0].doorTransform);
		}
		SaveGameManager.Current.CurrentStreetName = building.StreetName;
		SaveGameManager.Current.CurrentStreetNumber = building.StreetNumber;
		PlayerController.SetNavAgentTypeId(0);
		buildingRegistration = BuildingHelper.GetBuildingRegistration(building.Address);
		businessType = BusinessTypeHelper.GetData(buildingRegistration);
		AsyncOperationHandle<GameObject> asyncOperationHandle = BuildingSizeResolver.LoadBuildingAsync(new BuildingSizeInfo(building));
		if (asyncOperationHandle.IsValid())
		{
			yield return asyncOperationHandle;
		}
		if (!LoadBuilding())
		{
			yield return UiFader.UnFade(0.2f);
			enteringBuilding = false;
			yield break;
		}
		LoadItems();
		bool flag = InstanceBehavior<GameManager>.Instance.selectedVehicle != null && !InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.spawnInPlayerObject;
		GameAnalytics.TrackEnterBuilding(building.Address.ToAnalyticsString(), building.BuildingType, flag ? InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.name : null);
		ExitZone firstExitZone = GetFirstExitZone(vehicleSlot, entranceDoorId, flag);
		if (firstExitZone == null && !useSaveGamePlayerPosition)
		{
			UnityEngine.Debug.LogError("No exit zone found.");
			yield return UiFader.UnFade(0.2f);
			enteringBuilding = false;
			yield break;
		}
		GetSpawnPointPositionAndRotation(useSaveGamePlayerPosition, firstExitZone, out var pos, out var rot);
		MovePlayerOrCarToSpawnPoint(inverseVehicleRotation, flag, pos, firstExitZone, rot, out var vehicleRigidbody);
		SetWorkingState();
		PlayInteriorAmbianceSound();
		PlayEnterSound(pos);
		GlobalEvents.onEnterBuilding?.Invoke(building.Address);
		GameEvent.Invoke("ba:gameevent_enteredbuilding");
		InputHelper.playerInput.Player.Interact.Reset();
		yield return DelayedEnterBuildingActions();
		for (int i = 0; i < 4; i++)
		{
			yield return null;
		}
		if (!EnergyHelper.goingToHospital)
		{
			yield return UiFader.UnFade(0.2f);
		}
		if (vehicleRigidbody != null)
		{
			vehicleRigidbody.constraints = RigidbodyConstraints.None;
		}
		enteringBuilding = false;
	}

	private void EnterHamptonsBuilding(Building buildingToEnter, bool useSaveGamePlayerPosition = false)
	{
		if (IsInsideBuilding)
		{
			ExitFromBuilding(0);
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			UndergroundParkingManager.ExitParkingCoroutine(playFadeAnimation: false);
		}
		InvalidateActiveBuildingContextCache();
		building = buildingToEnter;
		if (cityBuildingController == null || cityBuildingController.building != building)
		{
			cityBuildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(building.Address);
		}
		if (cityBuildingController != null)
		{
			InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(cityBuildingController.entranceDoors[0].doorTransform);
		}
		SaveGameManager.Current.CurrentStreetName = building.StreetName;
		SaveGameManager.Current.CurrentStreetNumber = building.StreetNumber;
		PlayerController.SetNavAgentTypeId(0);
		buildingRegistration = BuildingHelper.GetBuildingRegistration(building.Address);
		businessType = null;
		if (!LoadBuilding())
		{
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		bool flag = selectedVehicle != null && !selectedVehicle.vehicleType.spawnInPlayerObject;
		GameAnalytics.TrackEnterBuilding(building.Address.ToAnalyticsString(), building.BuildingType, flag ? selectedVehicle.vehicleType.name : null);
		if (useSaveGamePlayerPosition)
		{
			Vector3 pos = SaveGameManager.Current.LastPlayerPosition;
			Quaternion rot = SaveGameManager.Current.LastPlayerRotation;
			MovePlayerOrCarToSpawnPoint(inverseVehicleRotation: false, flag, pos, null, rot, out var _);
		}
		else if (flag && selectedVehicle.ShouldUseVehicleCam())
		{
			selectedVehicle.UpdateCamera();
			CameraHelper.GetCurrentCamera().PreviousStateIsValid = false;
			if (!selectedVehicle.vehicleType.spawnInPlayerObject)
			{
				selectedVehicle.vehicleInstance.SetStreetData(building.StreetName, building.StreetNumber);
			}
		}
		else
		{
			CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.indoorCamera);
		}
		SetWorkingState();
		GlobalEvents.onEnterBuilding?.Invoke(building.Address);
		GameEvent.Invoke("ba:gameevent_enteredbuilding");
		InputHelper.playerInput.Player.Interact.Reset();
	}

	private static void MovePlayerOrCarToSpawnPoint(bool inverseVehicleRotation, bool isDrivingVehicle, Vector3 pos, ExitZone firstExitZone, Quaternion rot, out Rigidbody vehicleRigidbody)
	{
		vehicleRigidbody = null;
		if (isDrivingVehicle)
		{
			MoveCarToSpawnPoint(inverseVehicleRotation, pos, firstExitZone, rot, out vehicleRigidbody);
			return;
		}
		MovePlayerToPoint(pos, rot);
		CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.indoorCamera);
	}

	private static void GetSpawnPointPositionAndRotation(bool useSaveGamePlayerPosition, ExitZone firstExitZone, out Vector3 pos, out Quaternion rot)
	{
		bool flag = SaveGameManager.Current.LastPlayerPosition.x > 750f;
		if (useSaveGamePlayerPosition & flag)
		{
			pos = SaveGameManager.Current.LastPlayerPosition;
			rot = SaveGameManager.Current.LastPlayerRotation;
		}
		else
		{
			pos = firstExitZone.playerSpawnPoint.position;
			rot = firstExitZone.playerSpawnPoint.rotation;
		}
	}

	private void PlayEnterSound(Vector3 pos)
	{
		PlayBuildingSound(enterSounds, building, buildingRegistration, pos);
	}

	private void PlayExitSound(Vector3 pos)
	{
		PlayBuildingSound(exitSounds, cityBuildingController.building, cityBuildingController.buildingRegistration, pos);
	}

	private static void PlayBuildingSound(List<BuildingEnterSound> sounds, Building targetBuilding, BuildingRegistration targetRegistration, Vector3 pos)
	{
		BuildingEnterSound buildingEnterSound = sounds.FirstOrDefault((BuildingEnterSound x) => x.buildingTypes.Contains(targetBuilding.BuildingType) && x.businessTypes.Contains(targetRegistration.businessTypeName) && x.buildingSizes.Contains(targetBuilding.BuildingSize));
		if (buildingEnterSound != null)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(buildingEnterSound.type, pos, 1f, isPlayerCreatedSound: false, null, 0.25f);
		}
	}

	private void PlayInteriorAmbianceSound()
	{
		AudioClip audioClip = interiorSounds.FirstOrDefault((BuildingInteriorSound x) => x.buildingTypes.Contains(building.BuildingType) && x.businessTypes.Contains(buildingRegistration.businessTypeName) && x.buildingSizes.Contains(building.BuildingSize))?.InteriorSounds.GetRandom();
		if (!audioClip)
		{
			audioClip = interiorSounds[0].InteriorSounds.GetRandom();
		}
		_ambianceAudioSource.clip = audioClip;
		_ambianceAudioSource.Play();
	}

	private static void MovePlayerToPoint(Vector3 pos, Quaternion rot)
	{
		InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(pos);
		InstanceBehavior<GameManager>.Instance.playerController.transform.rotation = rot;
	}

	private static void MoveCarToSpawnPoint(bool inverseVehicleRotation, Vector3 pos, ExitZone firstExitZone, Quaternion rot, out Rigidbody vehicleRigidbody)
	{
		Transform transform = InstanceBehavior<GameManager>.Instance.selectedVehicle.transform;
		MeshCollider componentInChildren = transform.GetComponentInChildren<MeshCollider>();
		vehicleRigidbody = transform.GetComponent<Rigidbody>();
		Vector3 position = ((firstExitZone != null) ? (pos + firstExitZone.playerSpawnPoint.forward * (componentInChildren.sharedMesh.bounds.size.z * 0.5f)) : pos);
		vehicleRigidbody.MovePosition(position);
		if (inverseVehicleRotation)
		{
			rot *= Quaternion.Euler(Vector3.up * 180f);
		}
		vehicleRigidbody.MoveRotation(rot);
		vehicleRigidbody.constraints = (RigidbodyConstraints)96;
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle.ShouldUseVehicleCam())
		{
			InstanceBehavior<GameManager>.Instance.selectedVehicle.UpdateCamera();
			CameraHelper.GetCurrentCamera().PreviousStateIsValid = false;
		}
	}

	private ExitZone GetFirstExitZone(int vehicleSlot, int entranceDoorId, bool isDrivingVehicle)
	{
		ExitZone exitZone = null;
		if (isDrivingVehicle)
		{
			foreach (ExitZone exitZone2 in exitZones)
			{
				if (exitZone2.warehouseSlotController != null && exitZone2.warehouseSlotController.slotIndex == vehicleSlot)
				{
					exitZone = exitZone2;
					break;
				}
			}
			if (exitZone == null)
			{
				int num = Math.Max(vehicleSlot - 1, 0);
				if (num < exitZones.Count)
				{
					exitZone = exitZones[num];
				}
			}
		}
		else
		{
			if (entranceDoorId >= 0)
			{
				exitZone = exitZones.FirstOrDefault(delegate(ExitZone x)
				{
					ExitZoneDespawner despawner = x.despawner;
					return (object)despawner != null && despawner.exitToZoneId == entranceDoorId;
				});
			}
			if (exitZone == null)
			{
				exitZone = (from x in exitZones
					where !isDrivingVehicle || x.warehouseSlotController == null || ((Warehouse)buildingRegistration).vehicleSlots[x.warehouseSlotController.slotIndex - 1]?.vehicleInstanceId == null
					orderby x.isPrimarySpawnPoint descending
					select x).FirstOrDefault();
			}
		}
		return exitZone;
	}

	public bool LoadBuilding(bool isBlueprintCreator = false)
	{
		Stopwatch stopwatch = StartBuildingLoadTimer();
		bool result = LoadBuildingInternal(isBlueprintCreator);
		LogBuildingLoadTime(stopwatch, building.Address.ToString());
		return result;
	}

	private bool LoadBuildingInternal(bool isBlueprintCreator)
	{
		HamptonsHouse hamptonsHouse = null;
		if (cityBuildingController != null && cityBuildingController is CityHamptonsHouseController cityHamptonsHouseController)
		{
			hamptonsHouse = cityHamptonsHouseController.hamptonsHouse;
		}
		currentBuildingVersion = ((hamptonsHouse != null) ? hamptonsHouse.transform : ToggleBuildingLayout(building, state: true));
		if (currentBuildingVersion == null)
		{
			return false;
		}
		multipleHeightsBuildingController = currentBuildingVersion.GetComponent<MultipleHeightsBuildingController>();
		multipleHeightsBuildingController?.OnEnterBuilding();
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(0);
		exitZones = currentBuildingVersion.GetComponentsInChildren<ExitZone>().ToList();
		InteriorElement[] componentsInChildren = currentBuildingVersion.GetComponentsInChildren<InteriorElement>();
		InteriorElementsHelper.InteriorElementsCache.Clear();
		InteriorElement[] array = componentsInChildren;
		foreach (InteriorElement interiorElement in array)
		{
			InteriorElementsHelper.InteriorElementsCache.Add(interiorElement.UUID, interiorElement);
		}
		if (building.BuildingSize == "ba:buildingsize_parking")
		{
			return true;
		}
		InteriorElement interiorElement2 = componentsInChildren.FirstOrDefault((InteriorElement x) => x.IsFloor);
		interiorBounds = ((interiorElement2 == null) ? currentBuildingVersion.Find("SM_GroundPlane").GetComponent<BoxCollider>().bounds : new Bounds(interiorElement2.transform.position, Vector3.zero));
		if (!isBlueprintCreator && hamptonsHouse == null)
		{
			ApplyInteriorDesign(building, componentsInChildren);
		}
		array = componentsInChildren;
		foreach (InteriorElement interiorElement3 in array)
		{
			interiorBounds.Encapsulate(interiorElement3.transform.position);
		}
		if (!isBlueprintCreator)
		{
			FillBuildingDirtSpotObjects(currentBuildingVersion);
			if (buildingRegistration.dirtSpots != null && _cachedDirtSpotObjects != null && buildingRegistration.dirtSpots.Count != _cachedDirtSpotObjects.Count)
			{
				buildingRegistration.dirtSpots = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(building);
			}
			UpdateDirtinessInCurrentBuilding();
		}
		PlacementSystem.currentBuildingGrid = currentBuildingVersion.GetComponent<IBuildingGrid>();
		PlacementSystem.currentBuildingGrid.HideGrid(GridType.Both);
		PlacementSystem.multipleHeightsBuildingController = currentBuildingVersion.GetComponent<IMultipleHeightsBuildingController>();
		if (hamptonsHouse != null)
		{
			PlacementSystem.groundBounds = hamptonsHouse.plotBounds;
			hamptonsHouse.OnEnterPlot();
		}
		if ((IsPlayerOwnedBusiness || buildingRegistration.Layout == null) | isBlueprintCreator)
		{
			return true;
		}
		BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(buildingRegistration.businessTypeName, new BuildingSizeInfo(building), buildingRegistration.Layout, warnIfNotFound: false);
		if (orLoadBusinessLayoutSet != null)
		{
			if (LoadBusinessLayoutSet(orLoadBusinessLayoutSet))
			{
				return true;
			}
		}
		else if (building.SpecialService == null)
		{
			UnityEngine.Debug.LogWarning($"No business layout set found for {buildingRegistration.businessTypeName} {building.BuildingSize} {building.BuildingVersion} {buildingRegistration.Layout}");
		}
		Transform transform = currentBuildingVersion.Find("Layouts");
		if (transform == null)
		{
			UnityEngine.Debug.LogError("Layout: " + buildingRegistration.Layout + " wasn't found. Loading city");
			ExitFromBuilding(0);
			return false;
		}
		Transform transform2 = transform.Find(buildingRegistration.Layout);
		if (transform2 == null)
		{
			UnityEngine.Debug.LogError("Layout: " + buildingRegistration.Layout + " wasn't found. Loading city");
			ExitFromBuilding(0);
			return false;
		}
		currentLayout = transform2;
		transform2.gameObject.SetActive(value: true);
		return true;
	}

	private void FillBuildingDirtSpotObjects(Transform buildingTransform)
	{
		_cachedDirtSpotObjects.Clear();
		if (multipleHeightsBuildingController != null)
		{
			GameObject[] floorsParents = multipleHeightsBuildingController.GetFloorsParents();
			if (floorsParents == null)
			{
				UnityEngine.Debug.LogWarning("No floors found for " + buildingTransform.name);
				return;
			}
			GameObject[] array = floorsParents;
			foreach (GameObject gameObject in array)
			{
				_cachedDirtSpotObjects.AddRange(gameObject.GetComponentsInChildren<DirtSpotObject>());
			}
		}
		else
		{
			Transform transform = buildingTransform.Find("Floors");
			if (transform == null)
			{
				UnityEngine.Debug.LogWarning("No floors found for " + buildingTransform.name);
			}
			else
			{
				_cachedDirtSpotObjects.AddRange(transform.GetComponentsInChildren<DirtSpotObject>());
			}
		}
	}

	public void LoadItems()
	{
		InstantiateInstances(buildingRegistration.itemInstances.Values, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		GetItemControllers();
		SetUpItemControllersParents(allItemControllers);
		WaitingLinesHelper.Init(allItemControllers);
	}

	public void SetUpItemControllersParents(List<ItemController> itemControllers)
	{
		foreach (ItemController itemController in itemControllers)
		{
			if (itemController.ItemInstance == null || itemController.ItemInstance.stackedItems == null || itemController.ItemInstance.stackedItems.Count == 0 || itemController.AttachmentPoints.Length == 0)
			{
				continue;
			}
			for (int num = itemController.ItemInstance.stackedItems.Count - 1; num >= 0; num--)
			{
				AttachableChild attachableChild = itemController.ItemInstance.stackedItems[num];
				ItemController itemControllerByID = ItemHelper.GetItemControllerByID(attachableChild.childId, itemControllers);
				if (itemControllerByID == null)
				{
					itemController.ItemInstance.stackedItems.RemoveAt(num);
				}
				else
				{
					if (attachableChild.attachmentIndex < 0 || attachableChild.attachmentIndex >= itemController.AttachmentPoints.Length)
					{
						attachableChild.attachmentIndex = 0;
					}
					itemControllerByID.SetToParentPlaceableItem(itemController, itemController.AttachmentPoints[attachableChild.attachmentIndex]);
				}
			}
		}
	}

	private void GetItemControllers()
	{
		allItemControllers = (currentLayout ? new List<ItemController>(currentLayout.GetComponentsInChildren<ItemController>()) : new List<ItemController>(IndoorItemContainer.GetComponentsInChildren<ItemController>(includeInactive: true)));
	}

	public void InstantiateInstances(IEnumerable<ItemInstance> instances, Transform itemContainer = null, bool onlyVisual = false)
	{
		foreach (ItemInstance instance in instances)
		{
			InstantiateSingleInstance(instance, itemContainer, onlyVisual);
		}
	}

	public ItemController InstantiateSingleInstance(ItemInstance itemInstance, Transform itemContainer = null, bool onlyVisual = false)
	{
		if (itemContainer == null)
		{
			itemContainer = indoorItemContainer;
		}
		bool seasonalDecorations = PlayerPrefSettings.SeasonalDecorations;
		BigAmbitions.Items.Item itemCached = itemInstance.ItemCached;
		if (itemCached == null)
		{
			return null;
		}
		if (!seasonalDecorations && SeasonHelper.CurrentSeasonName != SeasonName.None && itemCached.season == SeasonHelper.CurrentSeasonName)
		{
			return null;
		}
		string text = itemInstance.itemName;
		if (itemCached.isSeasonalForSale)
		{
			text = itemCached.GetItemNameBySeason(seasonalDecorations ? SeasonHelper.CurrentSeasonName : SeasonName.None);
			if (string.IsNullOrEmpty(text))
			{
				text = itemCached.GetItemNameBySeason(SeasonName.None);
				if (string.IsNullOrEmpty(text))
				{
					if (!ignoreSeasons)
					{
						return null;
					}
					text = itemCached.itemsBySeason[0].itemName;
				}
			}
		}
		ItemController itemController = PrefabHelper.CreatePrefabItem(text, itemContainer);
		if ((bool)itemController)
		{
			ItemController component = itemController.GetComponent<ItemController>();
			component.ItemInstance = itemInstance;
			if (itemInstance.playerItemPurchaserSettings != null)
			{
				component.playerItemPurchaserSettings = itemInstance.playerItemPurchaserSettings;
			}
			if (!string.IsNullOrEmpty(itemInstance.customValue))
			{
				component.customValue = itemInstance.customValue;
			}
			component.TogglePhysics(physicsEnabled: true);
			if (onlyVisual)
			{
				component.enabled = false;
				component.SetCustomColors(itemInstance.customColors);
			}
			itemController.transform.position = itemInstance.position;
			itemController.transform.rotation = itemInstance.Rotation;
			return component;
		}
		UnityEngine.Debug.LogWarning("Item prefab not found: " + text);
		return null;
	}

	public IEnumerator DelayedEnterBuildingActions()
	{
		yield return null;
		if (!IsInsideBuilding)
		{
			yield break;
		}
		if (!building.Address.IsUndefined())
		{
			allVehicleControllers = InstanceBehavior<GameManager>.Instance.SpawnPlayerVehicles(building.Address);
		}
		else
		{
			UnityEngine.Debug.LogError("Building.Address is Undefined!!!");
		}
		if (!IsInsideBuilding)
		{
			yield break;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle != null && !selectedVehicle.vehicleType.spawnInPlayerObject)
		{
			selectedVehicle.vehicleInstance.SetStreetData(building.StreetName, building.StreetNumber);
			selectedVehicle.SavePosition();
		}
		if ((bool)currentLayout)
		{
			foreach (ItemController item in allItemControllers.Where((ItemController x) => x.playerItemPurchaserSettings?.enabled ?? false))
			{
				item.PlayerItemPurchaser?.UpdatePriceInfo();
			}
		}
		SetupAiEmployeeStations();
		GlobalEvents.onEnterBuildingDelayed?.Invoke(building.Address);
	}

	private void OnPlacementModeEnd()
	{
		foreach (ItemController allItemController in allItemControllers)
		{
			allItemController.ForceUpdateNavMesh();
		}
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			foreach (ItemController allItemController2 in allItemControllers)
			{
				if (!allItemController2.parentItemController)
				{
					allItemController2.UpdateNavMeshTargets();
				}
			}
		}, 3);
		ScheduleUpdateAvailableProducers();
	}

	public void ScheduleUpdateAvailableProducers()
	{
		if (!_scheduledUpdateAvailableProducers)
		{
			_scheduledUpdateAvailableProducers = true;
			CoroutineUtility.RunAfterOneFrame(ForceUpdateAvailableProducers);
		}
	}

	public void ForceUpdateAvailableProducers()
	{
		_scheduledUpdateAvailableProducers = false;
		GetItemControllers();
		WaitingLinesHelper.Init(allItemControllers);
	}

	private void RunCurrentBuildingHourly()
	{
		SetWorkingState();
		BusinessHelper.RestockCurrentBusinessIfNeeded(buildingRegistration);
	}

	private void SetWorkingState()
	{
		isOpen = BusinessHelper.IsBusinessOpen(buildingRegistration);
		InstanceBehavior<OverlayManager>.Instance?.UpdateDynamicComponents(null, DynamicOverlayUpdateType.CtaUpdate);
	}

	public static void ApplyInteriorDesign(Building building, InteriorElement[] selectableInteriorElements)
	{
		BuildingRegistration registration = building.GetRegistration();
		BuildingSizeInfo key = new BuildingSizeInfo(building);
		if (!DefaultInteriorDesigns.ContainsKey(key))
		{
			Dictionary<string, SerializedInteriorDesign> dictionary = new Dictionary<string, SerializedInteriorDesign>();
			dictionary.EnsureCapacity(selectableInteriorElements.Length);
			foreach (SerializedInteriorDesign item in selectableInteriorElements.Select((InteriorElement x) => x.Serialize()))
			{
				dictionary.Add(item.UUID, item);
			}
			DefaultInteriorDesigns.Add(key, dictionary);
		}
		Dictionary<string, SerializedInteriorDesign> interiorDesignerLookup = registration.GetInteriorDesignerLookup();
		foreach (InteriorElement interiorElement in selectableInteriorElements)
		{
			if (!interiorDesignerLookup.TryGetValue(interiorElement.UUID, out var value))
			{
				value = DefaultInteriorDesigns[key][interiorElement.UUID];
			}
			interiorElement.Deserialize(value);
		}
	}

	public static void ApplyInteriorDesign(List<SerializedInteriorDesign> interiorDesigns, InteriorElement[] selectableInteriorElements)
	{
		Dictionary<string, SerializedInteriorDesign> dictionary = CreateInteriorDesignLookup(interiorDesigns);
		foreach (InteriorElement interiorElement in selectableInteriorElements)
		{
			if (dictionary.TryGetValue(interiorElement.UUID, out var value))
			{
				interiorElement.Deserialize(value);
			}
		}
	}

	private static Dictionary<string, SerializedInteriorDesign> CreateInteriorDesignLookup(List<SerializedInteriorDesign> interiorDesigns)
	{
		Dictionary<string, SerializedInteriorDesign> dictionary = new Dictionary<string, SerializedInteriorDesign>(interiorDesigns.Count);
		foreach (SerializedInteriorDesign interiorDesign in interiorDesigns)
		{
			dictionary.TryAdd(interiorDesign.UUID, interiorDesign);
		}
		return dictionary;
	}

	public Transform ToggleBuildingLayout(Building toggledBuilding, bool state)
	{
		return ToggleLayout(new BuildingSizeInfo(toggledBuilding), state);
	}

	public Transform ToggleLayout(BuildingSizeInfo sizeInfo, bool state)
	{
		BuildingStructureController instantiatedBuilding = BuildingSizeResolver.GetInstantiatedBuilding(sizeInfo);
		if (!instantiatedBuilding)
		{
			UnityEngine.Debug.LogError("Building not found: " + sizeInfo.ToString());
			return null;
		}
		instantiatedBuilding.gameObject.SetActive(state);
		indoorVolume.enabled = state;
		if ((bool)extraIndoorsDirectionalLight)
		{
			extraIndoorsDirectionalLight.gameObject.SetActive(state);
		}
		return instantiatedBuilding.transform;
	}

	public Transform GetBuildingTransform()
	{
		return GetBuildingTransform(new BuildingSizeInfo(building));
	}

	public Transform GetBuildingTransform(BuildingSizeInfo sizeInfo)
	{
		return BuildingSizeResolver.GetInstantiatedBuilding(sizeInfo).transform;
	}

	public void UpdateDirtinessOnHeightChange()
	{
		if (_cachedDirtSpotObjects == null)
		{
			return;
		}
		foreach (DirtSpotObject cachedDirtSpotObject in _cachedDirtSpotObjects)
		{
			cachedDirtSpotObject.SetDirtinessVisibilityBasedOnHeight();
			cachedDirtSpotObject.SetDirtiness();
		}
	}

	public void UpdateDirtinessInCurrentBuilding()
	{
		if (_cachedDirtSpotObjects == null)
		{
			return;
		}
		foreach (DirtSpotObject cachedDirtSpotObject in _cachedDirtSpotObjects)
		{
			cachedDirtSpotObject.SetDirtiness();
		}
	}

	private void HideDirtInCurrentBuilding()
	{
		if (_cachedDirtSpotObjects == null)
		{
			return;
		}
		foreach (DirtSpotObject cachedDirtSpotObject in _cachedDirtSpotObjects)
		{
			cachedDirtSpotObject.HideDirt();
		}
	}

	public void UpdateDirtinessInSpecificSpot(int spotIndex)
	{
		if (spotIndex >= 0 && spotIndex < _cachedDirtSpotObjects.Count)
		{
			_cachedDirtSpotObjects[spotIndex].SetDirtiness();
		}
	}

	private void SetupAiEmployeeStations()
	{
		if (IsPlayerOwnedBusiness || BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.ignorespawnaibusinessemployees))
		{
			return;
		}
		List<EmployeeStationController> list = IndoorItemContainer.GetComponentsInChildren<EmployeeStationController>().ToList();
		if ((bool)currentLayout)
		{
			list.AddRange(currentLayout.GetComponentsInChildren<EmployeeStationController>());
		}
		foreach (EmployeeStationController item in list)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = item.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled)
			{
				if (!(buildingRegistration.BusinessName == "IKA BOHAG") || !(item is ComputerController computerController) || !item.gameObject.activeInHierarchy)
				{
					continue;
				}
				ItemController itemController = computerController.FindChair();
				if (itemController == null)
				{
					continue;
				}
				ThirdPersonCharacter thirdPersonCharacter = itemController.transform.Find("Random IKA customer using stuff they shouldn't")?.GetComponent<ThirdPersonCharacter>();
				if (UnityEngine.Random.Range(0, 100) <= 3)
				{
					if (thirdPersonCharacter == null)
					{
						thirdPersonCharacter = PrefabHelper.CreatePrefab<ThirdPersonCharacter>("Characters/HumanDefinitionLow", item.transform);
						thirdPersonCharacter.name = "Random IKA customer using stuff they shouldn't";
						thirdPersonCharacter.SitOnChair(itemController.transform);
						CharacterAnimationPlayer characterAnimationPlayer = thirdPersonCharacter.AddComponent<CharacterAnimationPlayer>();
						characterAnimationPlayer.characterAnimator = thirdPersonCharacter.animator;
						characterAnimationPlayer.minTimeBetweenAnims = 2f;
						characterAnimationPlayer.maxTimeBetweenAnims = 6f;
						characterAnimationPlayer.animationToPlay = ItemsGetter.GetByName(item.itemName).usingAnimation;
						computerController.PlayVideoOnScreen(VideoClipData.VideoType.Game);
					}
					thirdPersonCharacter.appearanceSetter.SetRandomAppearance();
				}
				else
				{
					UnityEngine.Object.Destroy(thirdPersonCharacter);
					computerController.StopVideoOnScreen();
				}
			}
			else if (ShouldAssignAI(item))
			{
				AssignAiToEmployeeStation(item);
			}
		}
	}

	private static bool ShouldAssignAI(EmployeeStationController station)
	{
		if (!(station.parentItemController is BusinessEmployeeController))
		{
			if (station is ComputerController computerController)
			{
				return computerController.FindChair() != null;
			}
			return true;
		}
		return false;
	}

	public void AssignAiToEmployeeStation(EmployeeStationController stationController)
	{
		ThirdPersonCharacter thirdPersonCharacter = stationController.transform.Find("Employee")?.GetComponent<ThirdPersonCharacter>() ?? PrefabHelper.CreatePrefab<ThirdPersonCharacter>("Characters/HumanDefinitionLow", stationController.transform);
		thirdPersonCharacter.name = "Employee";
		stationController.AssignEmployee(thirdPersonCharacter, stationController.GetAIEmployeeInstance());
	}

	public void SerializeInteriorDesign()
	{
		if (buildingRegistration.RentedByPlayer)
		{
			buildingRegistration.interiorDesigns = InteriorElementsHelper.InteriorElementsCache.Select((KeyValuePair<string, InteriorElement> x) => x.Value.Serialize()).ToList();
		}
	}

	public void InteractFloorCell(DirtSpotObject cell)
	{
		onFloorCellClick.Invoke(cell);
	}

	public List<int> GetDirtAffectedCells(IPlaceableItem placeableItem)
	{
		if (buildingRegistration.dirtSpots == null)
		{
			return new List<int>();
		}
		ItemController itemController = placeableItem as ItemController;
		if (!itemController || itemController.Colliders == null || itemController.Colliders.Length == 0)
		{
			return new List<int>();
		}
		_colliderResultList.Clear();
		Quaternion rotation = itemController.transform.rotation;
		Vector3 halfExtents = itemController.Colliders[0].bounds.size / 2f;
		halfExtents.x = Mathf.Max(halfExtents.x, 0.25f);
		halfExtents.y = Mathf.Max(halfExtents.y, 0.25f);
		halfExtents.z = Mathf.Max(halfExtents.z, 0.25f);
		for (int i = 0; i < itemController.GetNavMeshTargetCount(); i++)
		{
			int num = Physics.OverlapBoxNonAlloc(itemController.GetNavMeshTargetPosition(i), halfExtents, _colliderResults, rotation, LayerHelper.groundLayerMask);
			for (int j = 0; j < num; j++)
			{
				_colliderResultList.Add(_colliderResults[j]);
			}
		}
		if (itemController is IWaitingLineHolder waitingLineHolder)
		{
			int num2 = Physics.OverlapBoxNonAlloc(waitingLineHolder.GetFirstQueuePosition(), halfExtents, _colliderResults, rotation, LayerHelper.groundLayerMask);
			for (int k = 0; k < num2; k++)
			{
				_colliderResultList.Add(_colliderResults[k]);
			}
		}
		List<Vector3Int> possibleDirtSpotsPositions = _colliderResultList.Select((Collider x) => new Vector3(x.transform.position.x, 0f, x.transform.position.z).RoundToInts()).Distinct().ToList();
		return (from spot in buildingRegistration.dirtSpots
			where possibleDirtSpotsPositions.Any((Vector3Int possibleSpot) => spot.x == possibleSpot.x && spot.z == possibleSpot.z)
			select buildingRegistration.dirtSpots.IndexOf(spot)).ToList();
	}

	public void ExitFromBuilding(int targetExitId, bool playFadeAnimation = true)
	{
		StartCoroutine(ExitFromBuildingCoroutine(targetExitId, playFadeAnimation));
	}

	public IEnumerator ExitFromBuildingCoroutine(int targetExitId, bool playFadeAnimation = true, bool onlyFadeIn = false)
	{
		if (multipleHeightsBuildingController != null && multipleHeightsBuildingController is HamptonsHouse hamptonsHouse)
		{
			ExitFromHamptonsBuilding(hamptonsHouse);
		}
		else
		{
			if (exitingBuilding)
			{
				yield break;
			}
			exitingBuilding = true;
			if (playFadeAnimation)
			{
				yield return UiFader.Fade(0.2f);
				yield return null;
			}
			VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
			if (cityBuildingController != null)
			{
				Transform exitEntranceTransform = GetExitEntranceTransform(targetExitId);
				PlayExitSound(exitEntranceTransform.position);
				InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(InstanceBehavior<GameManager>.Instance.playerController.transform);
				if (selectedVehicle == null || selectedVehicle.vehicleType.spawnInPlayerObject)
				{
					MovePlayerToSafeExit(exitEntranceTransform);
				}
			}
			if (selectedVehicle != null && cityBuildingController != null)
			{
				if (!selectedVehicle.vehicleType.spawnInPlayerObject)
				{
					DriveInEntrance driveInEntrance = cityBuildingController.driveInEntrances.FirstOrDefault((DriveInEntrance x) => x.doorID == targetExitId) ?? cityBuildingController.driveInEntrances[0];
					bool inverseVehicleRotation = DetermineInverseVehicleRotation(targetExitId);
					MeshCollider componentInChildren = selectedVehicle.GetComponentInChildren<MeshCollider>();
					bool flag = BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address);
					GetVehicleSpawnPositionAndRotationOnExitBuilding(flag, driveInEntrance, componentInChildren, inverseVehicleRotation, out var vehicleSpawnPosition, out var vehicleSpawnRotation);
					if (AnyPlayerVehiclesBlocking(vehicleSpawnPosition, componentInChildren, vehicleSpawnRotation))
					{
						Notifications.ShowError("buildingmanager_notification_warehouse_gate_is_bocked");
						exitingBuilding = false;
						yield return UiFader.UnFade(0.2f);
						yield break;
					}
					if (flag)
					{
						driveInEntrance.InstantlyOpenGarageDoor();
					}
					CheckIfVehicleShouldBeRemovedFromWarehouseSlotOnExit();
					selectedVehicle.vehicleInstance.SetStreetData(string.Empty, 0);
					selectedVehicle.transform.position = vehicleSpawnPosition;
					InstanceBehavior<GameManager>.Instance.playerController.transform.localRotation = Quaternion.identity;
					selectedVehicle.transform.rotation = vehicleSpawnRotation;
					VehicleHelper.DestroyBlockingVehicles(selectedVehicle.gameObject, selectedVehicle.vehicleInstance.VehicleType);
					if (selectedVehicle is CarController carController)
					{
						carController.Reset();
					}
				}
				else
				{
					selectedVehicle.vehicleInstance.SetStreetData(string.Empty, 0);
				}
			}
			PlayerController.SetNavAgentTypeId(1479372276);
			allItemControllers = null;
			Address address = building.Address;
			ResetIndoors();
			GlobalEvents.onExitBuilding?.Invoke(address);
			if (selectedVehicle != null && selectedVehicle.ShouldUseVehicleCam())
			{
				selectedVehicle.UpdateCamera();
				CameraHelper.GetCurrentCamera().PreviousStateIsValid = false;
			}
			else
			{
				CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.pedestrianCamera);
			}
			if (playFadeAnimation && !onlyFadeIn)
			{
				yield return UiFader.UnFade(0.2f);
			}
			exitingBuilding = false;
		}
	}

	private static void MovePlayerToSafeExit(Transform exitEntranceTransform)
	{
		Vector3 position = exitEntranceTransform.position;
		Quaternion rotation = exitEntranceTransform.rotation;
		for (int i = 0; i < 10; i++)
		{
			Vector3 vector = position + exitEntranceTransform.forward * ((float)i * 0.2f);
			if (IsExitPositionFree(vector))
			{
				MovePlayerToPoint(vector, rotation);
				return;
			}
		}
		MovePlayerToPoint(position, rotation);
	}

	private static bool IsExitPositionFree(Vector3 position)
	{
		return !Physics.CheckSphere(position + Vector3.up * 0.9f, 0.45f, LayerHelper.buildingsLayerMask, QueryTriggerInteraction.Ignore);
	}

	private void ExitFromHamptonsBuilding(HamptonsHouse hamptonsHouse)
	{
		hamptonsHouse.OnExitBuilding();
		InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(InstanceBehavior<GameManager>.Instance.playerController.transform);
		PlayerController.SetNavAgentTypeId(1479372276);
		Address address = building.Address;
		ClearBuildingReferences();
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(99);
		ResetCurrentAddress();
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null && InstanceBehavior<GameManager>.Instance.selectedVehicle.ShouldUseVehicleCam())
		{
			InstanceBehavior<GameManager>.Instance.selectedVehicle.UpdateCamera();
			CameraHelper.GetCurrentCamera().PreviousStateIsValid = false;
			InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.SetStreetData(null, 0);
		}
		else
		{
			CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.pedestrianCamera);
		}
		GuidersManager.UpdateGuidersVisibility();
		GlobalEvents.onExitBuilding?.Invoke(address);
		RefreshHamptonsHouseBlockerCollider(address);
	}

	private static bool AnyPlayerVehiclesBlocking(Vector3 vehicleSpawnPosition, MeshCollider vehicleMeshCollider, Quaternion vehicleSpawnRotation)
	{
		return Physics.CheckBox(vehicleSpawnPosition, vehicleMeshCollider.sharedMesh.bounds.size / 2f, vehicleSpawnRotation, LayerHelper.VehiclesLayerIndex);
	}

	private static void GetVehicleSpawnPositionAndRotationOnExitBuilding(bool canEnterBuilding, DriveInEntrance driveInEntrance, MeshCollider vehicleMeshCollider, bool inverseVehicleRotation, out Vector3 vehicleSpawnPosition, out Quaternion vehicleSpawnRotation)
	{
		vehicleSpawnPosition = (canEnterBuilding ? driveInEntrance.GetSpawnPositionInsideOfGarageDoor(vehicleMeshCollider) : driveInEntrance.GetSpawnPositionInFrontOfGarageDoor(vehicleMeshCollider));
		vehicleSpawnRotation = driveInEntrance.transform.rotation;
		if (inverseVehicleRotation)
		{
			vehicleSpawnRotation *= Quaternion.Euler(0f, 180f, 0f);
		}
	}

	private bool DetermineInverseVehicleRotation(int targetExitId)
	{
		Transform transform = exitZones.FirstOrDefault((ExitZone x) => x.despawner.exitToZoneId == targetExitId)?.transform;
		bool result = false;
		if (transform != null)
		{
			Vector3 frontPoint = InstanceBehavior<GameManager>.Instance.selectedVehicle.FrontPoint;
			Vector3 backPoint = InstanceBehavior<GameManager>.Instance.selectedVehicle.BackPoint;
			Vector3 b = transform.position - transform.forward * 10f;
			float num = MathHelper.DistanceSqr(frontPoint, b);
			float num2 = MathHelper.DistanceSqr(backPoint, b);
			result = num > num2;
		}
		return result;
	}

	private Transform GetExitEntranceTransform(int targetExitId)
	{
		Transform transform = cityBuildingController.entranceDoors.FirstOrDefault((BuildingEntranceDoor x) => x.doorId == targetExitId)?.doorTransform;
		if (transform == null)
		{
			transform = cityBuildingController.entranceDoors[0].doorTransform;
		}
		return transform;
	}

	private void ResetIndoors()
	{
		indoorVolume.enabled = false;
		extraIndoorsDirectionalLight.gameObject.SetActive(value: false);
		indoorsShadowsDirectionalLight.gameObject.SetActive(value: false);
		SerializeInteriorDesign();
		HideDirtInCurrentBuilding();
		indoorItemContainer.ClearChildren();
		multipleHeightsBuildingController?.OnExitBuilding();
		ClearBuildingReferences();
		ResetCurrentAddress();
		BuildingSizeResolver.DisableAllSizesAndLayouts();
		GuidersManager.UpdateGuidersVisibility();
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(99);
	}

	private static void ResetCurrentAddress()
	{
		SaveGameManager.Current.CurrentStreetName = null;
		SaveGameManager.Current.CurrentStreetNumber = 0;
	}

	private void ClearBuildingReferences()
	{
		InvalidateActiveBuildingContextCache();
		cityBuildingController = null;
		multipleHeightsBuildingController = null;
		building = null;
		buildingRegistration = null;
		businessType = null;
		currentLayout = null;
		SaveGameManager.Current.CurrentStreetName = string.Empty;
		SaveGameManager.Current.CurrentStreetNumber = 0;
		PlacementSystem.currentBuildingGrid = null;
		PlacementSystem.multipleHeightsBuildingController = null;
		PlacementSystem.groundBounds = null;
	}

	private void CheckIfVehicleShouldBeRemovedFromWarehouseSlotOnExit()
	{
		if (!IsPlayerOwnedBusiness || building.BuildingType != "ba:buildingtype_warehouse")
		{
			return;
		}
		foreach (VehicleSlot vehicleSlot in ((Warehouse)buildingRegistration).vehicleSlots)
		{
			if (!(vehicleSlot.vehicleInstanceId != InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.id))
			{
				vehicleSlot.vehicleInstanceId = null;
				Dictionary<string, string> notificationData = new Dictionary<string, string>
				{
					{
						"type",
						InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.vehicleTypeName
					},
					{
						"warehouseName",
						((Warehouse)buildingRegistration).BusinessName
					}
				};
				Notifications.Show(NotificationType.Success, "carcontroller_notification_warehouse_vehicle_unassigned", notificationData);
			}
		}
	}

	private static bool IsWarehouseFull(CityBuildingController cbc)
	{
		if (!cbc.buildingRegistration.RentedByPlayer)
		{
			return false;
		}
		if (!(cbc.buildingRegistration is Warehouse warehouse))
		{
			return false;
		}
		int numberOfVehicleSlots = BuildingSizeHelper.GetData(cbc.building).numberOfVehicleSlots;
		return CountParkedVehicles(warehouse, cbc.building.Address) >= numberOfVehicleSlots;
	}

	private static int CountParkedVehicles(Warehouse warehouse, Address warehouseAddress)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
		{
			if (!vehicleSlot.vehicleInstanceId.IsNullOrEmpty() && BuildingHelper.IsVehicleAtAddress(vehicleSlot.vehicleInstanceId, warehouseAddress))
			{
				hashSet.Add(vehicleSlot.vehicleInstanceId);
			}
		}
		return hashSet.Count;
	}

	public bool LoadBusinessLayoutSet(BusinessLayoutSet businessLayoutSet)
	{
		if (businessLayoutSet == null)
		{
			return false;
		}
		if (businessLayoutSet.BusinessType != buildingRegistration.businessTypeName || businessLayoutSet.BuildingSize != building.BuildingSize || businessLayoutSet.BuildingVersion != building.BuildingVersion)
		{
			UnityEngine.Debug.LogError("BusinessLayoutSet does not match building registration.");
			return false;
		}
		Dictionary<string, Tuple<ItemController, List<AttachableChild>>> dictionary = new Dictionary<string, Tuple<ItemController, List<AttachableChild>>>();
		bool seasonalDecorations = PlayerPrefSettings.SeasonalDecorations;
		foreach (BusinessLayoutSets.Item item in businessLayoutSet.Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.itemName);
			if ((!seasonalDecorations && SeasonHelper.CurrentSeasonName != SeasonName.None && byName.season == SeasonHelper.CurrentSeasonName) || (!byName.IsAvailableInCurrentSeason() && !ignoreSeasons))
			{
				continue;
			}
			string text = item.itemName;
			if (byName.isSeasonalForSale)
			{
				text = byName.GetItemNameBySeason(seasonalDecorations ? SeasonHelper.CurrentSeasonName : SeasonName.None);
				if (string.IsNullOrEmpty(text))
				{
					text = byName.GetItemNameBySeason(SeasonName.None);
					if (string.IsNullOrEmpty(text))
					{
						if (!ignoreSeasons)
						{
							continue;
						}
						text = byName.itemsBySeason[0].itemName;
					}
				}
			}
			ItemController itemController = PrefabHelper.CreatePrefabItem(text, indoorItemContainer);
			dictionary.Add(item.id, new Tuple<ItemController, List<AttachableChild>>(itemController, item.stackedItems));
			itemController.playerItemPurchaserSettings = new PlayerItemPurchaserSettings
			{
				enabled = (byName.isSeasonalForSale || item.playerItemPurchaserSettings.enabled),
				itemName = (byName.isSeasonalForSale ? text : item.playerItemPurchaserSettings.itemName),
				itemQuantity = (byName.isSeasonalForSale ? 1 : item.playerItemPurchaserSettings.itemQuantity)
			};
			itemController.itemName = text;
			itemController.customPositions = item.customPositions;
			itemController.customColors = item.customColors;
			itemController.customValue = item.customValue;
			itemController.transform.position = item.position;
			itemController.transform.rotation = item.rotation;
			itemController.ItemInstance = ItemHelper.InitializeNewInstance(text);
			itemController.ItemInstance.id = item.id;
			itemController.ItemInstance.worldSpaceTextValue = item.worldSpaceTextValue;
			itemController.ItemInstance.linkedItemName = item.linkedItemName;
			itemController.gameObject.SetActive(value: true);
		}
		foreach (var (_, (itemController3, list2)) in dictionary)
		{
			foreach (AttachableChild item2 in list2)
			{
				if (dictionary.ContainsKey(item2.childId) && itemController3.AttachmentPoints.Length > item2.attachmentIndex)
				{
					dictionary[item2.childId]?.Item1.SetToParentPlaceableItem(itemController3, itemController3.AttachmentPoints[item2.attachmentIndex]);
				}
			}
		}
		Dictionary<string, SerializedInteriorDesign> dictionary2 = businessLayoutSet.interiorDesigns.ToDictionary((SerializedInteriorDesign design) => design.UUID);
		foreach (KeyValuePair<string, InteriorElement> item3 in InteriorElementsHelper.InteriorElementsCache)
		{
			if (dictionary2.TryGetValue(item3.Value.UUID, out var value))
			{
				item3.Value.Deserialize(value);
			}
		}
		return true;
	}

	public ItemController FindOptimalItemController(string itemName, Vector3 agentPosition = default(Vector3), bool onlyStocked = true)
	{
		return FindOptimalItemControllerInternal((ItemController x) => x.GetProducedItemName() == itemName, agentPosition, onlyStocked);
	}

	public ItemController FindOptimalItemControllerWithTag(int itemTag, Vector3 agentPosition = default(Vector3), bool onlyStocked = true)
	{
		return FindOptimalItemControllerInternal(delegate(ItemController x)
		{
			string producedItemName = x.GetProducedItemName();
			if (string.IsNullOrEmpty(producedItemName))
			{
				return false;
			}
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(producedItemName);
			return byName != null && byName.HasTag(itemTag);
		}, agentPosition, onlyStocked);
	}

	private ItemController FindOptimalItemControllerInternal(Func<ItemController, bool> matches, Vector3 agentPosition, bool onlyStocked)
	{
		var list = (from x in allItemControllers
			where matches(x) && (x.ItemInstance == null || !ItemHelper.HasAnyMissingRequirements(x.ItemInstance))
			select new
			{
				ItemController = x,
				InStock = ((IsPlayerOwnedBusiness && x.Item.producerSettings.ResourcesRequired) ? x.ItemInstance.GetStockInstance().amount : 99)
			}).ToList();
		if (onlyStocked)
		{
			list = list.Where(x => x.InStock > 0).ToList();
		}
		if (list.Count == 0)
		{
			return null;
		}
		if (agentPosition != default(Vector3))
		{
			list = list.OrderBy(x => Vector3.SqrMagnitude(x.ItemController.transform.position - agentPosition)).ToList();
		}
		if (!(agentPosition == default(Vector3)))
		{
			return list.FirstOrDefault()?.ItemController;
		}
		return list.GetRandom()?.ItemController;
	}

	public VehicleSpawnerController FindNearestVehicleSpawner(string vehicleTypeName, Vector3 agentPosition = default(Vector3))
	{
		List<VehicleSpawnerController> list = (from x in allItemControllers.OfType<VehicleSpawnerController>()
			where x.vehicleType == vehicleTypeName
			select x).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		if (agentPosition != default(Vector3))
		{
			list = list.OrderBy((VehicleSpawnerController x) => Vector3.SqrMagnitude(x.transform.position - agentPosition)).ToList();
		}
		if (!(agentPosition == default(Vector3)))
		{
			return list.FirstOrDefault();
		}
		return list.GetRandom();
	}

	public IEnumerable<CashRegisterController> FindCashRegisters(bool requireEmployee = false)
	{
		IEnumerable<ItemController> source = allItemControllers.Where(delegate(ItemController x)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = x.playerItemPurchaserSettings;
			return (playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled) && (ItemsGetter.GetByName(x.itemName).type & ItemType.PointOfSale) != 0 && !ItemHelper.HasAnyMissingRequirements(x.ItemInstance);
		});
		if (requireEmployee)
		{
			source = source.Where((ItemController x) => x.GetComponent<CashRegisterController>().employee);
		}
		return source.Select((ItemController x) => x.GetComponent<CashRegisterController>());
	}

	public List<CashRegisterController> GetAvailableCashRegisters()
	{
		return FindCashRegisters(requireEmployee: true)?.Where((CashRegisterController x) => x.ItemInstance == null || !ItemHelper.HasAnyMissingRequirements(x.ItemInstance)).ToList();
	}

	public bool IsThereItemByName(string itemName)
	{
		return allItemControllers.Any((ItemController x) => x.itemName == itemName);
	}

	public bool AreThereItemsByName(string[] itemNames)
	{
		return allItemControllers.Any((ItemController x) => itemNames.Contains(x.itemName));
	}

	public ItemController FindClosestItemByName(Vector3 originPosition, string[] itemNames)
	{
		return (from x in allItemControllers
			where itemNames.Contains(x.itemName)
			orderby MathHelper.DistanceSqr(originPosition, x.transform.position)
			select x).First();
	}

	public List<ItemController> GetItemControllersByName(string[] itemNames)
	{
		return allItemControllers.Where((ItemController x) => itemNames.Contains(x.itemName)).ToList();
	}

	public List<EmployeeStationController> GetEmployeeStationControllersWithAssignedEmployee(string[] itemNames, List<EmployeeStationController> resultsList = null)
	{
		if (resultsList == null)
		{
			resultsList = new List<EmployeeStationController>();
		}
		foreach (ItemController allItemController in allItemControllers)
		{
			if (itemNames.Contains(allItemController.itemName) && allItemController is EmployeeStationController employeeStationController && employeeStationController.employee != null)
			{
				resultsList.Add(employeeStationController);
			}
		}
		return resultsList;
	}

	public List<ItemController> GetAllTablesWithSeatsAvailable()
	{
		return allItemControllers.Where((ItemController x) => (x.Item.type & ItemType.Table) != 0 && x.SeatSpots.Any((SeatSpot y) => y.IsAvailable)).ToList();
	}

	public bool IsEmployeeStationBeingUsed(string itemName, BuildingRegistration registration)
	{
		return allItemControllers.Where((ItemController x) => x.itemName == itemName).Any((ItemController x) => EmployeeHelper.IsEmployeeStationEmployedAtHour(registration, x.ItemInstance.id, SaveGameManager.Current.Hour));
	}

	[ConsoleMethod("StartEditingBusiness", "Enables adding/moving/removing items from current non-owned business. Useful to update layouts together with SaveBusinessLayout command", new string[] { })]
	public static void EditBusiness()
	{
		PlayerAction.Confirm.Reset();
		if (!ignoreSeasons)
		{
			LanguageChangeEventDataHolder bodyData = "start_editing_seasons_warning".Localize();
			Action onConfirmAction = OnConfirmEditBusiness;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			OnConfirmEditBusiness();
		}
	}

	private static void OnConfirmEditBusiness()
	{
		isBuildingTemporarilyEditable = true;
		UnityEngine.Debug.Log("Current building items are editable!");
	}

	[ConsoleMethod("StopEditingBusiness", "Disables adding/moving/removing items from current non-owned business", new string[] { })]
	public static void StopEditingBusiness()
	{
		isBuildingTemporarilyEditable = false;
		UnityEngine.Debug.Log("Current building items are no longer editable");
	}

	[ConsoleMethod("ToggleIgnoreSeasons", "Toggles the option to make all items available regardless of the current season", new string[] { })]
	public static void ToggleIgnoreSeasons()
	{
		ignoreSeasons = !ignoreSeasons;
		UnityEngine.Debug.Log(ignoreSeasons ? "Seasons are ignored!" : "Seasons are not ignored!");
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DefaultInteriorDesigns.Clear();
		isBuildingTemporarilyEditable = false;
		ignoreSeasons = false;
	}

	private static Stopwatch StartBuildingLoadTimer()
	{
		return null;
	}

	private static void LogBuildingLoadTime(Stopwatch stopwatch, string buildingAddress)
	{
	}
}
