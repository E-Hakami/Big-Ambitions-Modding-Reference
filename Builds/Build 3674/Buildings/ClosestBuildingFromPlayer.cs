using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Parking.UndergroundParking;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Buildings;

public static class ClosestBuildingFromPlayer
{
	private static bool ClosestCbcPositionJobInitialized;

	private static bool IsRunningClosestCbcPositionJob;

	private static ClosestPositionJob ClosestCbcPositionJob;

	private static JobHandle ClosestCbcPositionJobHandle;

	private static Building DefaultBuildingOnErrorCached;

	private static Building DefaultBuildingOnError => DefaultBuildingOnErrorCached ?? (DefaultBuildingOnErrorCached = BuildingHelper.GetBuilding(new Address("ba:street_thirdstreet", 45)));

	public static void Init()
	{
		Application.quitting += OnDestroy;
	}

	public static Building Get()
	{
		if (!TryGet(out var building))
		{
			return DefaultBuildingOnError;
		}
		return building;
	}

	public static bool TryGet(out Building building)
	{
		building = null;
		if (GameManager.isCitySceneBeingUnloaded)
		{
			return false;
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			if (!UndergroundParkingManager.currentParkingEntrance)
			{
				return false;
			}
			building = UndergroundParkingManager.currentParkingEntrance.parentCbc.building;
			return building;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			building = InstanceBehavior<BuildingManager>.Instance.building;
			return building;
		}
		if (!ClosestCbcPositionJobInitialized)
		{
			TryToInitializeClosestPositionJob();
		}
		if (!ClosestCbcPositionJobInitialized)
		{
			return false;
		}
		if (!IsRunningClosestCbcPositionJob)
		{
			RunJob();
		}
		TryToCompleteClosestPositionJob();
		int closestIndex = ClosestCbcPositionJob.GetClosestIndex();
		if (closestIndex == -1)
		{
			return false;
		}
		RunJob();
		building = InstanceBehavior<CityManager>.Instance.cityBuildingControllers[closestIndex].building;
		return building;
	}

	private static void RunJob()
	{
		ClosestCbcPositionJob.playerPosition = PlayerHelper.GetPosition();
		IsRunningClosestCbcPositionJob = true;
		ClosestCbcPositionJobHandle = ClosestCbcPositionJob.Schedule(InstanceBehavior<CityManager>.Instance.cityBuildingControllers.Length, 64);
	}

	private static void TryToInitializeClosestPositionJob()
	{
		if ((bool)InstanceBehavior<CityManager>.Instance && InstanceBehavior<CityManager>.Instance.cityBuildingControllers != null && InstanceBehavior<CityManager>.Instance.cityBuildingControllers.Length != 0)
		{
			ClosestCbcPositionJob = new ClosestPositionJob
			{
				positions = new NativeArray<float3>(((IEnumerable<CityBuildingController>)InstanceBehavior<CityManager>.Instance.cityBuildingControllers).Select((Func<CityBuildingController, float3>)((CityBuildingController x) => x.entranceDoors[0].doorTransform.position)).ToArray(), Allocator.Persistent),
				distances = new NativeArray<float>(InstanceBehavior<CityManager>.Instance.cityBuildingControllers.Length, Allocator.Persistent),
				playerPosition = PlayerHelper.GetPosition()
			};
			ClosestCbcPositionJobInitialized = true;
			GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(OnDestroy));
		}
	}

	private static void TryToCompleteClosestPositionJob()
	{
		if (IsRunningClosestCbcPositionJob)
		{
			ClosestCbcPositionJobHandle.Complete();
			IsRunningClosestCbcPositionJob = false;
		}
	}

	private static void OnDestroy()
	{
		if (ClosestCbcPositionJobInitialized)
		{
			TryToCompleteClosestPositionJob();
			ClosestCbcPositionJob.distances.Dispose();
			ClosestCbcPositionJob.positions.Dispose();
			ClosestCbcPositionJobInitialized = false;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ClosestCbcPositionJobInitialized = false;
		IsRunningClosestCbcPositionJob = false;
		ClosestCbcPositionJob = default(ClosestPositionJob);
		ClosestCbcPositionJobHandle = default(JobHandle);
		DefaultBuildingOnErrorCached = null;
	}
}
