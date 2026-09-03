using System;
using System.Collections.Generic;
using System.Linq;
using BAModAPI.Services;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Data.VehicleColors;
using Extensions;
using GleyTrafficSystem;
using IngameDebugConsole;
using NWH.Common.Utility;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Damage;
using NWH.VehiclePhysics2.Modules.SpeedLimiter;
using NWH.VehiclePhysics2.Powertrain;
using NWH.WheelController3D;
using Parking.UndergroundParking;
using UI;
using UI.Load;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using Vehicles;
using Vehicles.Components;
using Vehicles.VehicleTypes;

namespace Helpers;

public static class VehicleHelper
{
	public const string BodyColliderPath = "CarHolder/Colliders/BodyCollider";

	private const float DriverSideSpaceRaycastDistance = 5f;

	private const float DriverSideSpaceRaycastHeight = 1f;

	private const float GroundSnapRaycastHeight = 2f;

	private const float GroundSnapMaxDrop = 0.5f;

	private const string AngleAssistSettingsAddressableLabel = "Vehicles";

	public static readonly List<VehicleController> AllPlayerVehicles = new List<VehicleController>();

	public static UnityEvent<VehicleController> onVehicleDestroyed = new UnityEvent<VehicleController>();

	public static List<VehicleLightsToggle> allLightsToggles = new List<VehicleLightsToggle>();

	private static bool _isInitialized;

	private static readonly Dictionary<string, (Vector3, Vector3)> _vehiclesColliderCenterAndSize = new Dictionary<string, (Vector3, Vector3)>();

	private static readonly Dictionary<string, VehicleInstance> VehiclesCache = new Dictionary<string, VehicleInstance>();

	private static readonly Collider[] OverlapColliders = new Collider[32];

	private static AngleAssistSettings angleAssistSettings;

	public static AngleAssistSettings AngleAssistSettings
	{
		get
		{
			if (angleAssistSettings == null)
			{
				angleAssistSettings = GetAngleAssistSettings();
			}
			return angleAssistSettings;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		_isInitialized = false;
		_vehiclesColliderCenterAndSize.Clear();
		VehiclesCache.Clear();
		AllPlayerVehicles.Clear();
		onVehicleDestroyed = new UnityEvent<VehicleController>();
		angleAssistSettings = null;
	}

	private static AngleAssistSettings GetAngleAssistSettings()
	{
		return Addressables.LoadAssetsAsync<AngleAssistSettings>("Vehicles", null).WaitForCompletion().FirstOrDefault();
	}

	public static void Init(GameManager gameManager)
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		VehiclesCache.Clear();
		InitAllLightToggles();
		angleAssistSettings = GetAngleAssistSettings();
		AngleAssist.ResetRoadAngles();
		SkipBridgeHelper.DisableSkipBridge();
		if (!_isInitialized)
		{
			CommandHelper.AddCommand("ToggleVehicleDamage", "Allow or disallow this vehicle to take damage", delegate
			{
				SaveGameManager.Current.gameVariables.disableVehicleDamage = !SaveGameManager.Current.gameVariables.disableVehicleDamage;
				SaveGameManager.Current.gameVariables.disableVehicleFuel = !SaveGameManager.Current.gameVariables.disableVehicleFuel;
			});
			_isInitialized = true;
		}
	}

	private static void InitAllLightToggles()
	{
		if (allLightsToggles == null)
		{
			allLightsToggles = new List<VehicleLightsToggle>();
		}
		else
		{
			allLightsToggles.Clear();
		}
	}

	public static void ToggleAllVehicleLights(bool toggle)
	{
		foreach (VehicleLightsToggle allLightsToggle in allLightsToggles)
		{
			allLightsToggle.ToggleLights(toggle);
		}
	}

	public static bool TryGetVehicleColor(string vehicleColorName, out VehicleColor resultVehicleColor)
	{
		resultVehicleColor = null;
		if (string.IsNullOrEmpty(vehicleColorName))
		{
			return false;
		}
		VehicleColor[] vehicleColors = InstanceBehavior<GlobalReferences>.Instance.vehicleColors;
		foreach (VehicleColor vehicleColor in vehicleColors)
		{
			if (!(vehicleColor.name != vehicleColorName))
			{
				resultVehicleColor = vehicleColor;
				return true;
			}
		}
		return false;
	}

	private static (Vector3, Vector3) GetColliderCenterAndSize(GameObject gameObject)
	{
		VehicleController component = gameObject.GetComponent<VehicleController>();
		return (component.navMeshObstacle.center, component.navMeshObstacle.size);
	}

	private static void OnEnterBuilding(Address address)
	{
		TrafficManager.Instance.SetPause(isPaused: true);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration.RentedByPlayer)
		{
			AddHandTruckSpawnersToBuildingIfNeeded(buildingRegistration, isInsideBuilding: true);
			AddFlatbedSpawnersToBuildingIfNeeded(buildingRegistration, isInsideBuilding: true);
		}
	}

	private static void OnExitBuilding(Address address)
	{
		TrafficManager.Instance.SetPause(isPaused: false);
	}

	public static void AddHandTruckSpawnersToBuildingIfNeeded(BuildingRegistration registration, bool isInsideBuilding = false)
	{
		if (registration.itemInstances.All((KeyValuePair<string, ItemInstance> x) => x.Value.itemName != "ba:itemname_handtruckspawner"))
		{
			CreateVehicleSpawners(registration, isInsideBuilding, "ba:itemname_handtruckspawner");
		}
	}

	public static void AddFlatbedSpawnersToBuildingIfNeeded(BuildingRegistration registration, bool isInsideBuilding = false)
	{
		if (!(registration.GetBuildingType() != "ba:buildingtype_warehouse") && registration.itemInstances.All((KeyValuePair<string, ItemInstance> x) => x.Value.itemName != "ba:itemname_flatbedspawner"))
		{
			CreateVehicleSpawners(registration, isInsideBuilding, "ba:itemname_flatbedspawner");
		}
	}

	public static void CreateVehicleSpawners(BuildingRegistration registration, bool isInsideBuilding, string spawnerName)
	{
		List<Transform> vehicleSpawnerTransformsInBuilding = GetVehicleSpawnerTransformsInBuilding(registration, spawnerName);
		if (vehicleSpawnerTransformsInBuilding == null)
		{
			return;
		}
		foreach (Transform item in vehicleSpawnerTransformsInBuilding)
		{
			ItemInstance itemInstance = ItemHelper.InitializeNewInstance(spawnerName);
			itemInstance.position = item.position;
			itemInstance.yRotation = item.eulerAngles.y;
			registration.AddItemInstanceToBuilding(itemInstance);
			if (isInsideBuilding)
			{
				InstanceBehavior<BuildingManager>.Instance.InstantiateSingleInstance(itemInstance);
			}
		}
	}

	public static List<Transform> GetVehicleSpawnerTransformsInBuilding(BuildingRegistration registration, string spawnerItemName)
	{
		return (registration.BuildingCached.IsHamptonsHouse() ? InstanceBehavior<CityManager>.Instance.FindCityBuildingController(registration.Address).transform : InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(registration)))?.GetComponentsInChildren<VehicleSpawnerController>(includeInactive: true).Where((VehicleSpawnerController x) => x.itemName == spawnerItemName && !x.gameObject.activeSelf).Select((VehicleSpawnerController x) => x.transform)
			.ToList();
	}

	public static (Vector3, Vector3) GetVehicleColliderCenterAndSize(string vehicleTypeName)
	{
		if (_vehiclesColliderCenterAndSize.TryGetValue(vehicleTypeName, out var value))
		{
			return value;
		}
		Debug.LogWarning("Vehicle " + vehicleTypeName + " collider data not loaded, loading it now, this may cause a lag spike");
		GameObject gameObject = PrefabHelper.LoadPrefabAssetByName("Vehicles/PlayerVehicles/" + vehicleTypeName.GetIdWithoutType());
		value = ((gameObject != null) ? GetColliderCenterAndSize(gameObject) : default((Vector3, Vector3)));
		_vehiclesColliderCenterAndSize[vehicleTypeName] = value;
		return value;
	}

	public static VehicleController CreateAndSpawnVehicle(VehicleInstance vehicleInstance, Vector3 spawnPosition, Quaternion spawnRotation)
	{
		string idWithoutType = vehicleInstance.vehicleTypeName.GetIdWithoutType();
		GameObject gameObject = PrefabHelper.CreatePrefab("Vehicles/PlayerVehicles/" + idWithoutType, InstanceBehavior<GameManager>.Instance.itemsContainer);
		if (VehicleTypeHelper.IsModVehicleType(vehicleInstance.vehicleTypeName))
		{
			AssetService.RemapShaders(gameObject);
		}
		VehicleController component = gameObject.GetComponent<VehicleController>();
		component.UpdateNavMeshTargets();
		component.SetVehicleInstance(vehicleInstance);
		component.SetDirtiness(vehicleInstance.dirtiness);
		gameObject.transform.position = (vehicleInstance.position = spawnPosition);
		gameObject.transform.rotation = (vehicleInstance.rotation = spawnRotation);
		SaveGameManager.Current.VehicleInstances.Add(vehicleInstance);
		if (!(component is ScooterController))
		{
			PointOfInterest pointOfInterest = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(gameObject.transform, InstanceBehavior<GlobalReferences>.Instance.vehiclePOIIcon, InstanceBehavior<GlobalReferences>.Instance.vehiclePOIBackgroundColor);
			if ((bool)pointOfInterest)
			{
				pointOfInterest.SetPermanent();
				component.poi = pointOfInterest;
				if (vehicleInstance.parkingState == ParkingState.Illegal)
				{
					pointOfInterest.SetBackground(InstanceBehavior<GlobalReferences>.Instance.vehicleIllegalParkingPOIBackgroundColor);
				}
			}
		}
		DestroyBlockingVehicles(gameObject, vehicleInstance.VehicleType);
		return component;
	}

	public static void DestroyBlockingVehicles(GameObject prefab, VehicleType vehicleType, bool onlyParkedVehicles = false)
	{
		DestroyBlockingVehicles(prefab, vehicleType, prefab.transform, onlyParkedVehicles);
	}

	public static void DestroyBlockingVehicles(GameObject prefab, VehicleType vehicleType, Transform placedTransform, bool onlyParkedVehicles = false)
	{
		Physics.SyncTransforms();
		Bounds localBounds;
		Collider bodyCollider;
		if (vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			Bounds bounds = prefab.GetComponent<BoxCollider>().bounds;
			localBounds = new Bounds(bounds.center, bounds.size);
		}
		else if (!TryGetBodyColliderBounds(prefab.transform, out localBounds, out bodyCollider))
		{
			Debug.LogError(prefab.name + " has no body collider at CarHolder/Colliders/BodyCollider", prefab);
			return;
		}
		int num = Physics.OverlapBoxNonAlloc(placedTransform.transform.TransformPoint(localBounds.center), localBounds.size * 0.6f, OverlapColliders, placedTransform.transform.rotation, LayerHelper.vehicleConflictMask);
		for (int i = 0; i < num; i++)
		{
			Collider collider = OverlapColliders[i];
			if (collider.transform.IsChildOf(placedTransform.transform))
			{
				continue;
			}
			if (collider.gameObject.layer == LayerHelper.ParkedVehiclesLayerIndex)
			{
				ParkingSimulator.ReleaseParkedVehicle(collider.gameObject);
			}
			else if (collider.gameObject.layer == LayerHelper.AiVehiclesLayerIndex && !onlyParkedVehicles)
			{
				GleyTrafficSystem.VehicleComponent componentInParent = collider.GetComponentInParent<GleyTrafficSystem.VehicleComponent>();
				if ((bool)componentInParent)
				{
					Manager.RemoveVehicle(componentInParent.gameObject);
				}
			}
		}
	}

	public static void DestroyBlockingParkedVehicles(bool skipPlayerMountedVehicles = false)
	{
		foreach (VehicleController allPlayerVehicle in AllPlayerVehicles)
		{
			if (!skipPlayerMountedVehicles || !(allPlayerVehicle.vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId))
			{
				DestroyBlockingVehicles(allPlayerVehicle.gameObject, allPlayerVehicle.vehicleType, onlyParkedVehicles: true);
			}
		}
	}

	public static bool TryGetBodyColliderBounds(Transform vehicleRoot, out Bounds localBounds, out Collider bodyCollider)
	{
		localBounds = default(Bounds);
		bodyCollider = null;
		Transform transform = vehicleRoot.Find("CarHolder/Colliders/BodyCollider");
		if (!transform || !transform.TryGetComponent<Collider>(out bodyCollider))
		{
			return false;
		}
		if (bodyCollider is MeshCollider meshCollider)
		{
			localBounds = meshCollider.sharedMesh.bounds;
			return true;
		}
		if (bodyCollider is BoxCollider boxCollider)
		{
			localBounds = new Bounds(boxCollider.center, boxCollider.size);
			return true;
		}
		return false;
	}

	[ConsoleMethod("getcar", "Spawn a player car in front of the player", new string[] { }, AutoCompleteMap = new string[] { "vehicleTypeName=VehicleTypes" })]
	public static void Command_GetCar(string vehicleTypeName)
	{
		Command_GetCar(vehicleTypeName, InstanceBehavior<GlobalReferences>.Instance.vehicleColors.GetRandom().name);
	}

	[ConsoleMethod("getcar", "Spawn a player car in front of the player", new string[] { }, AutoCompleteMap = new string[] { "vehicleTypeName=VehicleTypes", "VehicleColorName=VehicleColors" })]
	public static void Command_GetCar(string vehicleTypeName, string vehicleColorName)
	{
		Vector3 position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
		position += InstanceBehavior<GameManager>.Instance.playerController.transform.forward * 2f;
		CreateAndSpawnVehicle(new VehicleInstance(vehicleTypeName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = vehicleColorName,
			fuel = VehicleTypeHelper.GetVehicleType(vehicleTypeName).maxFuel
		}, position, Quaternion.identity);
	}

	public static void Delete(this VehicleInstance vehicleInstance, VehicleController vehicleController = null)
	{
		if (vehicleController != null)
		{
			onVehicleDestroyed?.Invoke(vehicleController);
			UnityEngine.Object.Destroy(vehicleController.gameObject);
		}
		if (vehicleInstance.Address.streetName != "ba:street_parking")
		{
			vehicleInstance.UnAssignFromWarehouse(showNotification: true);
		}
		SaveGameManager.Current.VehicleInstances.Remove(vehicleInstance);
	}

	[ConsoleMethod("Tow", "Tow a vehicle", new string[] { }, AutoCompleteMap = new string[] { "towType=TowDestinations" })]
	public static void Command_TowVehicle(string towType)
	{
		VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == SaveGameManager.Current.ActiveVehicleId);
		if (vehicleInstance == null)
		{
			Debug.LogError("No vehicle found");
		}
		else
		{
			TowVehicle(vehicleInstance, towType);
		}
	}

	public static Address TowVehicle(VehicleInstance instance, string towType)
	{
		if (InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: false);
		}
		VehicleController vehicleController = AllPlayerVehicles.First((VehicleController v) => v.vehicleInstance.id == instance.id);
		Vector3 position = (BuildingManager.IsInsideBuilding ? InstanceBehavior<BuildingManager>.Instance.cityBuildingController.entranceDoors[0].doorTransform.position : (UndergroundParkingManager.IsInsideParking ? UndergroundParkingManager.currentParkingEntrance.transform.position : vehicleController.transform.position));
		bool flag = VehicleTypeHelper.GetVehicleType(instance.vehicleTypeName).HasTag(TagRef.Vehicletag.istruck);
		bool truckOnly = (towType == "ba:towdestination_autorepairshop") & flag;
		IOrderedEnumerable<CityBuildingController> orderedEnumerable = from ctrl in InstanceBehavior<CityManager>.Instance.cityBuildingControllers
			where ctrl.building.BuildingType == "ba:buildingtype_special" && ctrl.building.SpecialService.settings is GasStationSettings { truckOnly: var truckOnly2 } && truckOnly2 == truckOnly
			orderby MathHelper.DistanceSqr(ctrl.entranceDoors[0].doorTransform.position, position)
			select ctrl;
		Collider[] results = new Collider[1];
		foreach (CityBuildingController item in orderedEnumerable)
		{
			GasStationController component = item.GetComponent<GasStationController>();
			GasStationTrigger[] array = ((!(towType == "ba:towdestination_autorepairshop")) ? component.gasStationTriggers : component.repairStationTriggers);
			GasStationTrigger[] array2 = array;
			if (array2 == null)
			{
				continue;
			}
			GasStationTrigger gasStationTrigger = null;
			foreach (GasStationTrigger gasStationTrigger2 in array2)
			{
				Bounds bounds = gasStationTrigger2.stationCollider.bounds;
				if (Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, results, Quaternion.identity, LayerHelper.vehicleAndHumanConflictMask) == 0)
				{
					gasStationTrigger = gasStationTrigger2;
					break;
				}
			}
			if ((bool)gasStationTrigger)
			{
				if (BuildingManager.IsInsideBuilding)
				{
					InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0, playFadeAnimation: false);
				}
				else if (UndergroundParkingManager.IsInsideParking)
				{
					UndergroundParkingManager.ExitParking(playFadeAnimation: false);
				}
				Quaternion towSpotRotation = GetTowSpotRotation(towType, gasStationTrigger.transform, instance.vehicleTypeName);
				TeleportVehicleToGround(vehicleController, gasStationTrigger.transform.position, towSpotRotation);
				GasStationController.RefreshActiveTriggers(vehicleController.vehicleCollider);
				if ((bool)component.gasStationRepairGarageDoor)
				{
					component.gasStationRepairGarageDoor.OpenDoor();
				}
				vehicleController.SavePosition();
				instance.SetStreetData(string.Empty, 0);
				return item.building.Address;
			}
		}
		return null;
	}

	private static Quaternion GetTowSpotRotation(string towType, Transform spotTransform, string vehicleTypeName)
	{
		Quaternion quaternion = Quaternion.LookRotation(spotTransform.up);
		if (towType != "ba:towdestination_autorepairshop")
		{
			return quaternion;
		}
		float halfVehicleWidth = GetVehicleColliderCenterAndSize(vehicleTypeName).Item2.x * 0.5f;
		Quaternion quaternion2 = quaternion * Quaternion.Euler(0f, 180f, 0f);
		float driverSideSpace = GetDriverSideSpace(spotTransform.position, quaternion, halfVehicleWidth);
		if (!(GetDriverSideSpace(spotTransform.position, quaternion2, halfVehicleWidth) > driverSideSpace))
		{
			return quaternion;
		}
		return quaternion2;
	}

	private static float GetDriverSideSpace(Vector3 position, Quaternion rotation, float halfVehicleWidth)
	{
		Vector3 vector = rotation * Vector3.left;
		Vector3 origin = position + Vector3.up * 1f + vector * halfVehicleWidth;
		int layerMask = (int)LayerHelper.wallsLayerMask | (int)LayerHelper.buildingsLayerMask | (int)LayerHelper.vehicleAndHumanConflictMask;
		if (!Physics.Raycast(origin, vector, out var hitInfo, 5f, layerMask, QueryTriggerInteraction.Ignore))
		{
			return 5f;
		}
		return hitInfo.distance;
	}

	public static void RegisterPlayerVehicle(VehicleController vehicleController)
	{
		if (!AllPlayerVehicles.Contains(vehicleController))
		{
			AllPlayerVehicles.Add(vehicleController);
		}
	}

	public static void UnregisterPlayerVehicle(VehicleController vehicleController)
	{
		AllPlayerVehicles.Remove(vehicleController);
	}

	public static VehicleInstance GetCurrentVehicle()
	{
		if (SaveGameManager.Current == null)
		{
			return null;
		}
		string activeVehicleId = SaveGameManager.Current.ActiveVehicleId;
		if (string.IsNullOrEmpty(activeVehicleId))
		{
			return null;
		}
		if (VehiclesCache.TryGetValue(activeVehicleId, out var value))
		{
			return value;
		}
		value = SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == SaveGameManager.Current.ActiveVehicleId);
		if (value != null)
		{
			VehiclesCache.Add(activeVehicleId, value);
		}
		return value;
	}

	public static VehicleController GetCurrentVehicleBase()
	{
		foreach (VehicleController allPlayerVehicle in AllPlayerVehicles)
		{
			if (allPlayerVehicle.vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId)
			{
				return allPlayerVehicle;
			}
		}
		return null;
	}

	public static bool IsInsideVehicle()
	{
		VehicleInstance currentVehicle = GetCurrentVehicle();
		if (currentVehicle != null)
		{
			return !VehicleTypeHelper.GetVehicleType(currentVehicle.vehicleTypeName).spawnInPlayerObject;
		}
		return false;
	}

	public static bool IsInsideMotorVehicle()
	{
		VehicleInstance currentVehicle = GetCurrentVehicle();
		if (currentVehicle != null)
		{
			return VehicleTypeHelper.GetVehicleType(currentVehicle.vehicleTypeName).IsMotorVehicle;
		}
		return false;
	}

	public static void SaveAllVehiclePositions()
	{
		foreach (VehicleController allPlayerVehicle in AllPlayerVehicles)
		{
			allPlayerVehicle.SavePosition();
		}
	}

	public static void TeleportCurrentVehicle(Transform point)
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		float z = selectedVehicle.GetComponentInChildren<MeshCollider>().sharedMesh.bounds.size.z;
		Vector3 targetPosition = point.position + point.forward * (z * 0.5f);
		TeleportVehicle(selectedVehicle, targetPosition, point.rotation);
		InstanceBehavior<GameManager>.Instance.playerController.transform.localRotation = Quaternion.identity;
	}

	public static void TeleportVehicle(VehicleController vehicle, Vector3 targetPosition, Quaternion targetRotation)
	{
		Vector3 position = vehicle.transform.position;
		if (!vehicle.TryGetComponent<NWH.VehiclePhysics2.VehicleController>(out var component))
		{
			vehicle.transform.SetPositionAndRotation(targetPosition, targetRotation);
			Physics.SyncTransforms();
		}
		else
		{
			Rigidbody vehicleRigidbody = component.vehicleRigidbody;
			vehicleRigidbody.position = targetPosition;
			vehicleRigidbody.rotation = targetRotation;
			vehicleRigidbody.angularVelocity = Vector3.zero;
			vehicleRigidbody.velocity = Vector3.zero;
			vehicle.Reset();
			Physics.SyncTransforms();
			component.input.Throttle = 0f;
			component.input.Brakes = 0f;
			foreach (WheelComponent wheel in component.powertrain.wheels)
			{
				((WheelController)wheel.wheelUAPI).ResetSimulationState();
			}
			component.powertrain.engine.outputAngularVelocity = UnitConverter.RPMToAngularVelocity(component.powertrain.engine.idleRPM);
		}
		ParkingLaneGenerator.RegenerateAutoParkSpotsNear(position);
		ParkingLaneGenerator.RegenerateAutoParkSpotsNear(targetPosition);
	}

	public static void TeleportVehicleToGround(VehicleController vehicle, Vector3 targetPosition, Quaternion targetRotation)
	{
		if (!Physics.Raycast(targetPosition + Vector3.up * 2f, Vector3.down, out var hitInfo, 2.5f, LayerHelper.groundLayerMask, QueryTriggerInteraction.Ignore))
		{
			Debug.LogError(string.Format("{0}: No ground found below {1}", "TeleportVehicleToGround", targetPosition));
			TeleportVehicle(vehicle, targetPosition, targetRotation);
		}
		else
		{
			Quaternion targetRotation2 = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * targetRotation;
			TeleportVehicle(vehicle, hitInfo.point, targetRotation2);
		}
	}

	public static bool IsColliderFromCurrentVehicle(Collider collider)
	{
		if (LoadScene.isLoading || !PlayerHelper.IsUsingVehicle)
		{
			return false;
		}
		return GetCurrentVehicleBase().vehicleCollider == collider;
	}

	[ConsoleMethod("GetMaxSpeed", "Gets the current max speed of a vehicle", new string[] { })]
	public static void GetMaxSpeed()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		SpeedLimiterModuleWrapper component = currentVehicleBase.GetComponent<SpeedLimiterModuleWrapper>();
		if (component == null)
		{
			Debug.LogError("This vehicle doesn't have the speed limiter module");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		float speedLimit = component.module.speedLimit;
		Debug.Log($"Max speed of {vehicleTypeName}: {speedLimit}");
	}

	[ConsoleMethod("SetMaxSpeed", "Sets the max speed of a vehicle", new string[] { })]
	public static void SetMaxSpeed(int maxSpeed)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		SpeedLimiterModuleWrapper component = currentVehicleBase.GetComponent<SpeedLimiterModuleWrapper>();
		if (component == null)
		{
			Debug.LogError("This vehicle doesn't have the speed limiter module");
			return;
		}
		component.module.speedLimit = maxSpeed;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Max speed of {vehicleTypeName} set to {maxSpeed}");
	}

	[ConsoleMethod("GetEnginePower", "Gets the current engine power of a vehicle", new string[] { })]
	public static void GetEnginePower()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		float maxPower = currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().powertrain.engine.maxPower;
		Debug.Log($"Engine power of {vehicleTypeName}: {maxPower}");
	}

	[ConsoleMethod("SetEnginePower", "Sets the engine power of a vehicle", new string[] { })]
	public static void SetEnginePower(int enginePower)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().powertrain.engine.maxPower = enginePower;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Engine power of {vehicleTypeName} set to {enginePower}");
	}

	[ConsoleMethod("GetBrakeForce", "Gets the current brake force of a vehicle", new string[] { })]
	public static void GetBrakeForce()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		float maxTorque = currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().brakes.maxTorque;
		Debug.Log($"Brake force of {vehicleTypeName}: {maxTorque}");
	}

	[ConsoleMethod("SetBrakeForce", "Sets the brake force of a vehicle", new string[] { })]
	public static void SetBrakeForce(int brakeForce)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().brakes.maxTorque = brakeForce;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Brake force of {vehicleTypeName} set to {brakeForce}");
	}

	[ConsoleMethod("GetMaxTurnRadius", "Gets the current max turn radius of a vehicle", new string[] { })]
	public static void GetMaxTurnRadius()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		float maximumSteerAngle = currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().steering.maximumSteerAngle;
		Debug.Log($"Max turn radius of {vehicleTypeName}: {maximumSteerAngle}");
	}

	[ConsoleMethod("SetMaxTurnRadius", "Sets the max turn radius of a vehicle", new string[] { })]
	public static void SetMaxTurnRadius(int maxTurnRadius)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().steering.maximumSteerAngle = maxTurnRadius;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Max turn radius of {vehicleTypeName} set to {maxTurnRadius}");
	}

	[ConsoleMethod("GetForceApplicationPointDistance", "Gets the current force app. point distance of a vehicle", new string[] { })]
	public static void GetForceApplicationPointDistance()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		float forceApplicationPointDistance = currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().powertrain.wheels[0].wheelUAPI.gameObject.GetComponent<WheelController>().forceApplicationPointDistance;
		Debug.Log($"Force app. point distance of {vehicleTypeName}: {forceApplicationPointDistance}");
	}

	[ConsoleMethod("SetForceApplicationPointDistance", "Sets the force app. point distance of a vehicle", new string[] { })]
	public static void SetForceApplicationPointDistance(int forceApplicationPointDistance)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		List<WheelComponent> wheels = currentVehicleBase.GetComponent<NWH.VehiclePhysics2.VehicleController>().powertrain.wheels;
		wheels[0].wheelUAPI.gameObject.GetComponent<WheelController>().forceApplicationPointDistance = forceApplicationPointDistance;
		wheels[1].wheelUAPI.gameObject.GetComponent<WheelController>().forceApplicationPointDistance = forceApplicationPointDistance;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Force app. point distance of {vehicleTypeName} set to {forceApplicationPointDistance}");
	}

	[ConsoleMethod("GetDamageIntensity", "Gets the current damage intensity of a vehicle", new string[] { })]
	public static void GetDamageIntensity()
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		DamageHandler component = currentVehicleBase.GetComponent<DamageHandler>();
		if (component == null)
		{
			Debug.LogError("This vehicle doesn't have the damage module");
			return;
		}
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Damage intensity of {vehicleTypeName}: {component.damageIntensity}");
	}

	[ConsoleMethod("SetDamageIntensity", "Sets the damage intensity of a vehicle", new string[] { })]
	public static void SetDamageIntensity(int damageIntensity)
	{
		VehicleController currentVehicleBase = GetCurrentVehicleBase();
		if (currentVehicleBase == null)
		{
			Debug.LogError("You need to be inside a vehicle to run this command");
			return;
		}
		DamageHandler component = currentVehicleBase.GetComponent<DamageHandler>();
		if (component == null)
		{
			Debug.LogError("This vehicle doesn't have the damage module");
			return;
		}
		component.damageIntensity = damageIntensity;
		string vehicleTypeName = currentVehicleBase.vehicleType.vehicleTypeName;
		Debug.Log($"Damage intensity of {vehicleTypeName} set to {damageIntensity}");
	}

	public static VehicleColor GetRandomVehicleColor()
	{
		return InstanceBehavior<GlobalReferences>.Instance.vehicleColors.GetRandomWeighted(InstanceBehavior<GlobalReferences>.Instance.VehicleColorsRandomWeights());
	}

	public static VehicleController GetVehicleController(VehicleInstance vehicleInstance)
	{
		foreach (VehicleController allPlayerVehicle in AllPlayerVehicles)
		{
			if (allPlayerVehicle.vehicleInstance.id == vehicleInstance.id)
			{
				return allPlayerVehicle;
			}
		}
		return null;
	}

	public static string GetVehicleTypeById(string id)
	{
		return SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == id).vehicleTypeName;
	}

	[ConsoleMethod("RepairVehicle", "Repairs the Vehicle the Player is currently in", new string[] { })]
	public static void RepairVehicle()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
			return;
		}
		if (selectedVehicle.vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			Debug.LogWarning("The current vehicle can't be repaired");
			return;
		}
		selectedVehicle.Repair();
		Debug.Log("Repaired " + selectedVehicle.vehicleType.vehicleTypeName);
	}

	[ConsoleMethod("SetCondition", "Sets an specific amount of condition to the current vehicle (between 0 and 100)", new string[] { })]
	public static void SetCondition(int conditionAmount)
	{
		if ((float)conditionAmount >= 100f)
		{
			RepairVehicle();
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
			return;
		}
		if (!(selectedVehicle is CarController carController))
		{
			Debug.LogWarning("The current vehicle can't be repaired");
			return;
		}
		float num = (float)Mathf.Clamp(conditionAmount, 0, 100) / 100f;
		carController.SetDamage(1f - num);
		Debug.Log($"Updated {selectedVehicle.vehicleType.vehicleTypeName} condition to {num}");
	}

	[ConsoleMethod("RefuelVehicle", "Refuels the Vehicle the Player is currently in", new string[] { })]
	public static void RefuelVehicle()
	{
		SetFuel(100);
	}

	[ConsoleMethod("SetFuel", "Sets an specific amount of fuel to the current vehicle (between 0 and max fuel amount)", new string[] { })]
	public static void SetFuel(int fuelAmount)
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
			return;
		}
		float num = Mathf.Clamp(fuelAmount, 0f, selectedVehicle.vehicleType.maxFuel);
		selectedVehicle.SetFuel(num);
		Debug.Log($"Updated {selectedVehicle.vehicleType.vehicleTypeName} fuel to {num}");
	}

	[ConsoleMethod("GetMaxFuel", "Shows the maximum fuel capacity of current vehicle", new string[] { })]
	public static void GetMaxFuel()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
		}
		else
		{
			Debug.Log($"The max fuel of {selectedVehicle.vehicleType.vehicleTypeName} is {selectedVehicle.vehicleType.maxFuel}");
		}
	}

	[ConsoleMethod("GetVehicleDirtiness", "Shows the dirtiness of the current vehicle", new string[] { })]
	public static void GetVehicleDirtiness()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
		}
		else
		{
			Debug.Log($"{selectedVehicle.vehicleType.vehicleTypeName} is {selectedVehicle.vehicleInstance.dirtiness * 100f}% dirty");
		}
	}

	[ConsoleMethod("SetVehicleDirtiness", "Sets the dirtiness of the current vehicle (between 0 and 100)", new string[] { })]
	public static void SetVehicleDirtiness(int dirtinessAmount)
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			Debug.LogWarning("You need to be inside a vehicle to use this command");
			return;
		}
		float num = (float)Mathf.Clamp(dirtinessAmount, 0, 100) / 100f;
		selectedVehicle.SetDirtiness(num);
		Debug.Log($"Updated {selectedVehicle.vehicleType.vehicleTypeName} dirtiness to {num} ({dirtinessAmount}%)");
	}
}
