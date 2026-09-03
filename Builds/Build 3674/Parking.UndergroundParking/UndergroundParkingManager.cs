using System;
using System.Collections;
using BigAmbitions.DayNightCycle;
using Blueprints;
using Cinemachine;
using Helpers;
using JimmysUnityUtilities;
using Streets;
using UI;
using UI.Guiders;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Parking.UndergroundParking;

public static class UndergroundParkingManager
{
	public const string ParkingBuildingSize = "ba:buildingsize_parking";

	public static Action onEnter;

	public static Action onExit;

	public static UndergroundParkingEntrance currentParkingEntrance;

	public static int previousParkingNumber;

	private static bool EnteringParking;

	private static bool ExitingParking;

	public static bool IsInsideParking => SaveGameManager.Current?.CurrentStreetName == "ba:street_parking";

	public static bool EnterParking(int parkingNumber)
	{
		UndergroundParkingEntrance undergroundParkingEntrance = InstanceBehavior<CityManager>.Instance.GetParkingCbc(parkingNumber)?.undergroundParkingEntrance;
		if (undergroundParkingEntrance == null)
		{
			Debug.LogError($"CBC not found for parking with number {parkingNumber}");
			return false;
		}
		EnterParking(undergroundParkingEntrance);
		return true;
	}

	public static void EnterParking(UndergroundParkingEntrance entrance, int entranceNumber = 0, bool playFadeAnimation = true)
	{
		CoroutineUtility.Run(EnterParkingCoroutine(entrance, entranceNumber, playFadeAnimation));
	}

	public static IEnumerator EnterParkingCoroutine(UndergroundParkingEntrance entrance, int entranceNumber = 0, bool playFadeAnimation = true, bool onlyFadeOut = false)
	{
		if (currentParkingEntrance != null || EnteringParking)
		{
			yield break;
		}
		currentParkingEntrance = entrance;
		EnteringParking = true;
		if (playFadeAnimation && !onlyFadeOut)
		{
			yield return UiFader.Fade(0.2f);
			yield return null;
		}
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo("ba:buildingsize_parking", entrance.parkingVersion);
		AsyncOperationHandle<GameObject> asyncOperationHandle = InstanceBehavior<BuildingManager>.Instance.BuildingSizeResolver.LoadBuildingAsync(sizeInfo);
		if (asyncOperationHandle.IsValid())
		{
			yield return asyncOperationHandle;
		}
		Transform transform = InstanceBehavior<BuildingManager>.Instance.ToggleLayout(sizeInfo, state: true);
		if (!transform)
		{
			currentParkingEntrance = null;
			EnteringParking = false;
			if (playFadeAnimation)
			{
				yield return UiFader.UnFade(0.2f);
			}
			yield break;
		}
		ExitZone exitZone = transform.GetComponentsInChildren<ExitZone>()[entranceNumber];
		if (GameManager.IsDrivingVehicle())
		{
			VehicleHelper.TeleportCurrentVehicle(exitZone.vehicleSpawnPoint);
		}
		else
		{
			PlayerHelper.Teleport(exitZone.playerSpawnPoint);
		}
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(0);
		InstanceBehavior<SfxManager>.Instance.indoorSnapshot.TransitionTo((!InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning) ? 1 : 0);
		InstanceBehavior<BuildingManager>.Instance.allVehicleControllers = InstanceBehavior<GameManager>.Instance.SpawnPlayerVehicles(entrance.Address);
		AddressHelper.UpdateCurrentAddress(entrance.Address);
		InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(entrance.parentCbc.entranceDoors[0].doorTransform);
		PlayerController.SetNavAgentTypeId(0);
		GuidersManager.UpdateGuidersVisibility();
		if (entrance.parkingNumber != previousParkingNumber)
		{
			ResetParkedCars(transform);
		}
		VehicleHelper.DestroyBlockingParkedVehicles();
		previousParkingNumber = entrance.parkingNumber;
		if (GameManager.IsDrivingVehicle() && InstanceBehavior<GameManager>.Instance.selectedVehicle.ShouldUseVehicleCam())
		{
			InstanceBehavior<GameManager>.Instance.selectedVehicle.UpdateCamera();
		}
		else
		{
			CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.indoorCamera);
		}
		RainHelper.OnEnterBuilding();
		if (playFadeAnimation)
		{
			yield return UiFader.UnFade(0.2f);
		}
		EnteringParking = false;
		onEnter?.Invoke();
	}

	public static void ExitParking(bool playFadeAnimation = true)
	{
		CoroutineUtility.Run(ExitParkingCoroutine(playFadeAnimation));
	}

	public static IEnumerator ExitParkingCoroutine(bool playFadeAnimation = true)
	{
		if (!ExitingParking)
		{
			ExitingParking = true;
			if (playFadeAnimation)
			{
				yield return UiFader.Fade(0.2f);
				yield return null;
			}
			CinemachineVirtualCameraBase camera;
			if (GameManager.IsDrivingVehicle())
			{
				camera = InstanceBehavior<GameManager>.Instance.vehicleCamera;
				VehicleHelper.TeleportCurrentVehicle(currentParkingEntrance.vehicleExitPoint);
				VehicleHelper.DestroyBlockingVehicles(InstanceBehavior<GameManager>.Instance.selectedVehicle.gameObject, InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType);
			}
			else
			{
				camera = InstanceBehavior<GameManager>.Instance.pedestrianCamera;
				PlayerHelper.Teleport(currentParkingEntrance.playerExitPoint);
			}
			MultipleHeightsBuildingController.SetGlobalHeightShaderValue(99);
			AddressHelper.UpdateCurrentAddress(null);
			GlobalEvents.onExitBuilding?.Invoke(currentParkingEntrance.Address);
			InstanceBehavior<CityManager>.Instance.SetTrafficSpawnDistanceTarget(InstanceBehavior<GameManager>.Instance.playerController.transform);
			PlayerController.SetNavAgentTypeId(1479372276);
			CameraHelper.SetCamera(camera);
			InstanceBehavior<BuildingManager>.Instance.ToggleLayout(new BuildingSizeInfo("ba:buildingsize_parking", currentParkingEntrance.parkingVersion), state: false);
			GuidersManager.UpdateGuidersVisibility();
			if (playFadeAnimation)
			{
				yield return UiFader.UnFade(0.2f);
			}
			currentParkingEntrance = null;
			ExitingParking = false;
			onExit?.Invoke();
		}
	}

	private static void ResetParkedCars(Transform layout)
	{
		ParkingLaneGenerator[] componentsInChildren = layout.GetComponentsInChildren<ParkingLaneGenerator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ProcessParkingLane();
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		currentParkingEntrance = null;
		previousParkingNumber = 0;
		EnteringParking = false;
		ExitingParking = false;
		onEnter = null;
		onExit = null;
	}
}
