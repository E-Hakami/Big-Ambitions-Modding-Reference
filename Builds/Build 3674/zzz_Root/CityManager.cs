using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boats;
using Buildings;
using Buildings.BuildingTypes.Special.GasStation;
using Buildings.Outdoors;
using BusinessLayoutSets;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Player.SaveSystem.CompatibilityFixes;
using Streets;
using UI;
using UI.Guiders;
using UI.Load;
using UnityEngine;
using UnityEngine.AI;

public class CityManager : InstanceBehavior<CityManager>
{
	private const float MaxDistanceFromPlayer = 100f;

	public static float defaultAiTrafficVehiclesSpawnRadius;

	public static float defaultAiTrafficVehiclesDeSpawnRadius;

	public CityMap cityMap;

	public PedestrianSpawner pedestrianSpawner;

	public BuildingOutsideMusicSpawner buildingOutsideMusicSpawner;

	public BuildingOutsideHangoutZoneSpawner buildingOutsideHangoutZone;

	public SubwaySystem subwaySystem;

	public TrafficComponent trafficComponent;

	public Transform parkedVehiclesStorage;

	[HideInInspector]
	public CityBuildingController[] cityBuildingControllers;

	[HideInInspector]
	public List<SubwayStation> subwayStations;

	[ReadOnly]
	public Transform trafficSpawnDistanceTarget;

	private readonly Dictionary<Address, CityBuildingController> _cityBuildingControllersDictionary = new Dictionary<Address, CityBuildingController>();

	private BillboardAd[] _billboardAds;

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			SetTrafficSpawnDistanceTarget(InstanceBehavior<GameManager>.Instance.playerController.transform);
			PermanentPointsOfInterest.UpdateIgnoredAddresses();
			PermanentPointsOfInterest.UpdatePermanentPointsOfInterest();
			GlobalEvents.RegisterOnGameLoadedCallback(OnScenesLoaded);
			GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(OnGameUnloaded));
		}
	}

	private void OnScenesLoaded()
	{
		InitTrafficComponent();
		InitCityBuildingControllers();
		InitTimeOfDayController();
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(99);
		if (!SaveGameManager.Current.CityInitialized)
		{
			CityGenerator.InitializeCity();
			SaveGameManager.Current.CityInitialized = true;
		}
		subwayStations = UnityEngine.Object.FindObjectsByType<SubwayStation>(FindObjectsSortMode.None).ToList();
		_billboardAds = UnityEngine.Object.FindObjectsByType<BillboardAd>(FindObjectsSortMode.None);
		InstanceBehavior<UIs>.Instance.cityMapSubwayStations.LoadStations();
		CityBuildingController[] array = cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in array)
		{
			if (_cityBuildingControllersDictionary.ContainsKey(cityBuildingController.building.Address))
			{
				Debug.LogError("CityBuildingController with Duplicated Address: " + cityBuildingController.building.Address, cityBuildingController);
				CityBuildingController cityBuildingController2 = _cityBuildingControllersDictionary[cityBuildingController.building.Address];
				Debug.LogError("CityBuildingController with Duplicated Address: " + cityBuildingController2.building.Address, cityBuildingController2);
			}
			else
			{
				_cityBuildingControllersDictionary.Add(cityBuildingController.building.Address, cityBuildingController);
			}
		}
		for (int j = 0; j < cityBuildingControllers.Length; j++)
		{
			cityBuildingControllers[j].UpdateSign();
		}
		for (int k = 0; k < _billboardAds.Length; k++)
		{
			_billboardAds[k].DoUpdate(0f);
		}
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			pedestrianSpawner.Init();
			buildingOutsideMusicSpawner?.Init();
			buildingOutsideHangoutZone?.Init();
			if (SaveGameManager.Current.customDestination != null)
			{
				GuidersManager.SetGuiderTarget(SaveGameManager.Current.customDestination, DirectionGuiderType.Destination);
			}
			array = cityBuildingControllers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].UpdateIndoorOutdoorLight();
			}
		}
		InstanceBehavior<GameManager>.Instance.SpawnPlayerVehicles(null);
		SpawnVehiclesParkedOnHamptonsHouses();
		PositionPlayerInScene();
		InstanceBehavior<BoatManager>.Instance?.LoadBoats();
		StartCoroutine(PlayerCameraTransition());
	}

	private void SpawnVehiclesParkedOnHamptonsHouses()
	{
		HashSet<Address> hashSet = new HashSet<Address>();
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			Address address = vehicleInstance.Address;
			if (!(address == null) && !address.IsUndefined())
			{
				Building building = BuildingHelper.GetBuilding(address);
				if (!(building == null) && building.IsHamptonsHouse() && hashSet.Add(address))
				{
					InstanceBehavior<GameManager>.Instance.SpawnPlayerVehicles(address);
				}
			}
		}
	}

	private static void InitTimeOfDayController()
	{
		InstanceBehavior<GameManager>.Instance.timeOfDayController = UnityEngine.Object.FindAnyObjectByType<TimeOfDayController>();
		InstanceBehavior<GameManager>.Instance.timeOfDayController?.Init();
	}

	private void InitCityBuildingControllers()
	{
		cityBuildingControllers = (from x in UnityEngine.Object.FindObjectsByType<CityBuildingController>(FindObjectsSortMode.None)
			where x.building != null
			select x).ToArray();
	}

	private void InitTrafficComponent()
	{
		trafficComponent = UnityEngine.Object.FindAnyObjectByType<TrafficComponent>();
		if (trafficComponent != null)
		{
			trafficComponent.player = InstanceBehavior<GameManager>.Instance.playerController.transform;
			int[] vehicleGroupTypePercentages = InstanceBehavior<GameManager>.Instance.timeOfDayController?.GetVehicleGroupTypeProbabilities(SaveGameManager.Current.Hour) ?? new int[3] { 33, 34, 33 };
			defaultAiTrafficVehiclesSpawnRadius = trafficComponent.minDistanceToAdd;
			defaultAiTrafficVehiclesDeSpawnRadius = trafficComponent.distanceToRemove;
			int numberOfVehicles = trafficComponent.vehiclePool.GetNumberOfVehicles();
			Manager.Initialize(trafficComponent.player, numberOfVehicles, trafficComponent.vehiclePool, trafficComponent.minDistanceToAdd, trafficComponent.distanceToRemove, trafficComponent.greenLightTime, trafficComponent.yellowLightTime, vehicleGroupTypePercentages, trafficComponent.blinkerTurnLookaheadDistance, trafficComponent.blinkerStopDelay);
		}
	}

	private void OnGameUnloaded()
	{
		TreeController.AllTrees.Clear();
		GasStationPartController.AllGasStationPartControllers.Clear();
	}

	private static IEnumerator PlayerCameraTransition()
	{
		InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning = false;
		if (!BuildingManager.IsInsideBuilding && SaveGameManager.Current.CurrentStreetName != "ba:street_parking" && SaveGameManager.Current.ActiveVehicleId == "")
		{
			yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.pedestrianCamera);
		}
		InstanceBehavior<SfxManager>.Instance.SetSoundSnapshot(SaveGameManager.Current?.LastPlayerPause ?? false);
		MainMenuMusic.Stop();
	}

	public void SetTrafficSpawnDistanceTarget(Transform target)
	{
		trafficSpawnDistanceTarget = target;
		if ((bool)trafficComponent)
		{
			Manager.SetCamera(target);
		}
	}

	private void PositionPlayerInScene()
	{
		if (InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			if (!TryToTeleportPlayerToLastPosition())
			{
				TeleportPlayerToSavePoint();
			}
			InstanceBehavior<GameManager>.Instance.playerController.transform.rotation = SaveGameManager.Current.LastPlayerRotation;
		}
		else if (UndergroundParkingManager.IsInsideParking)
		{
			UndergroundParkingManager.previousParkingNumber = SaveGameManager.Current.CurrentStreetNumber;
			if (UndergroundParkingManager.EnterParking(SaveGameManager.Current.CurrentStreetNumber))
			{
				TryToTeleportPlayerToLastPosition();
			}
			else
			{
				TeleportPlayerToSavePoint();
			}
		}
		else if (!string.IsNullOrEmpty(SaveGameManager.Current.CurrentStreetName))
		{
			Address address = new Address(SaveGameManager.Current.CurrentStreetName, SaveGameManager.Current.CurrentStreetNumber);
			Building building = BuildingHelper.GetBuilding(address);
			if (building == null)
			{
				Debug.LogError("Building at " + address.ToFormattedString() + " not found");
				TeleportPlayerToSavePoint();
			}
			else if (BusinessLayoutSetHelper.loadingLayouts && !building.GetRegistration().RentedByPlayer)
			{
				StartCoroutine(DelayEnterBuilding(building));
			}
			else
			{
				LoadIndoors(building);
			}
		}
		else if (!VehicleHelper.IsInsideVehicle())
		{
			if (!TryToTeleportPlayerToLastPosition())
			{
				TeleportPlayerToSavePoint();
			}
			InstanceBehavior<GameManager>.Instance.playerController.transform.rotation = SaveGameManager.Current.LastPlayerRotation;
		}
	}

	private void TeleportPlayerToSavePoint()
	{
		if (NavMesh.SamplePosition(Vector3.zero, out var hit, 500f, -1))
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(hit.position);
		}
	}

	private bool TryToTeleportPlayerToLastPosition()
	{
		if (!NavMesh.SamplePosition(SaveGameManager.Current.LastPlayerPosition, out var hit, 5f, -1))
		{
			return false;
		}
		InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(hit.position);
		return true;
	}

	private IEnumerator DelayEnterBuilding(Building building)
	{
		LoadingSpinner.Show();
		yield return new WaitUntil(() => !BusinessLayoutSetHelper.loadingLayouts);
		LoadIndoors(building);
		LoadingSpinner.Hide();
	}

	private void LoadIndoors(Building building)
	{
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building, useSaveGamePlayerPosition: true);
		if (SaveGameCompatibilityFixes.forcePlayerExitForCompatibility)
		{
			CoroutineUtility.RunAfterFrameDelay(delegate
			{
				InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0);
			}, 2);
		}
	}

	public CityBuildingController FindCityBuildingController(Address address)
	{
		return _cityBuildingControllersDictionary.GetValueOrDefault(address);
	}

	private void Update()
	{
		if (!LoadScene.isLoading && !InstanceBehavior<UIs>.Instance.timeMachine.isRunning && _billboardAds != null)
		{
			float currentTimeInMinutes = TimeHelper.NowInMinutes();
			for (int i = 0; i < _billboardAds.Length; i++)
			{
				_billboardAds[i].DoUpdate(currentTimeInMinutes);
			}
		}
	}

	public void UpdateBillboardsFromBusiness(string businessName)
	{
		foreach (BillboardAd item in _billboardAds.Where((BillboardAd x) => x.currentAdSettings?.businessName == businessName))
		{
			item.ShowNextAd(item.currentAdSettings);
		}
	}

	public void UpdatePointOfInterests()
	{
		PermanentPointsOfInterest.UpdatePermanentPointsOfInterest();
		cityMap?.UpdateNonePermanentPointOfInterests();
	}

	public CityBuildingController GetParkingCbc(int parkingNumber)
	{
		return cityBuildingControllers.FirstOrDefault((CityBuildingController x) => x.undergroundParkingEntrance != null && x.undergroundParkingEntrance.parkingNumber == parkingNumber);
	}

	public bool IsOutsidePlayerRange(Vector3 position)
	{
		return MathHelper.DistanceSqr(trafficSpawnDistanceTarget.position, position) >= 10000f;
	}
}
