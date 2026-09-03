using System.Collections.Generic;
using BigAmbitions.Tags;
using Buildings;
using Extensions;
using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace Streets.Pedestrians;

public static class PedestrianBuildingPositionProvider
{
	private static readonly List<CityBuildingController> EligibleBuildings = new List<CityBuildingController>();

	private static readonly List<CityBuildingController> EligibleBuildingsClosed = new List<CityBuildingController>();

	public static (Vector3 position, Quaternion rotation) GetRandomBuildingTarget(Vector3 fromPosition, float minimumDistance, float maximumDistance)
	{
		EligibleBuildings.Clear();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			if (!cityBuildingController.blockPedestrianSpawn)
			{
				float num = MathHelper.DistanceSqr(cityBuildingController.entranceDoors[0].doorTransform.transform.position, fromPosition);
				if (num > minimumDistance * minimumDistance && num < maximumDistance * maximumDistance)
				{
					EligibleBuildings.Add(cityBuildingController);
				}
			}
		}
		if (EligibleBuildings.Count > 0 && (bool)TryGetRandomBuildingTarget(EligibleBuildings, out var position, out var rotation))
		{
			return (position: position, rotation: rotation);
		}
		return default((Vector3, Quaternion));
	}

	public static (Vector3, Quaternion) GetRandomAvailableBuildingTarget(Vector3 fromPosition, float minimumDistance, float maximumDistance, out BuildingRegistration buildingRegistration)
	{
		EligibleBuildings.Clear();
		EligibleBuildingsClosed.Clear();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			if (cityBuildingController.blockPedestrianSpawn || !BuildingTypeHelper.GetData(cityBuildingController.building).HasTag(TagRef.Buildingtypetag.ispedestriangoal))
			{
				continue;
			}
			if (!(cityBuildingController.building.BuildingType == "ba:buildingtype_residential") && !BusinessHelper.IsBusinessOpen(cityBuildingController.buildingRegistration))
			{
				EligibleBuildingsClosed.Add(cityBuildingController);
				continue;
			}
			float num = MathHelper.DistanceSqr(cityBuildingController.entranceDoors[0].doorTransform.transform.position, fromPosition);
			if (num > minimumDistance * minimumDistance && num < maximumDistance * maximumDistance)
			{
				EligibleBuildings.Add(cityBuildingController);
			}
		}
		buildingRegistration = null;
		Vector3 position;
		Quaternion rotation;
		if (EligibleBuildings.Count > 0)
		{
			CityBuildingController cityBuildingController2 = TryGetRandomBuildingTarget(EligibleBuildings, out position, out rotation);
			if (cityBuildingController2 != null)
			{
				buildingRegistration = cityBuildingController2.buildingRegistration;
				return (position, rotation);
			}
		}
		if (EligibleBuildingsClosed.Count >= 0)
		{
			CityBuildingController cityBuildingController2 = TryGetRandomBuildingTarget(EligibleBuildingsClosed, out position, out rotation);
			if (cityBuildingController2 != null)
			{
				buildingRegistration = cityBuildingController2.buildingRegistration;
				return (position, rotation);
			}
		}
		return default((Vector3, Quaternion));
	}

	private static CityBuildingController TryGetRandomBuildingTarget(List<CityBuildingController> cbcs, out Vector3 position, out Quaternion rotation)
	{
		position = default(Vector3);
		rotation = Quaternion.identity;
		CityBuildingController random = cbcs.GetRandom();
		if (TryGetSampleTarget(random, out var randomBuildingPosition, out var lookRotation))
		{
			position = randomBuildingPosition;
			rotation = lookRotation;
			return random;
		}
		cbcs.Shuffle();
		foreach (CityBuildingController cbc in cbcs)
		{
			if (TryGetSampleTarget(cbc, out var randomBuildingPosition2, out var lookRotation2))
			{
				position = randomBuildingPosition2;
				rotation = lookRotation2;
				return cbc;
			}
		}
		return null;
	}

	private static bool TryGetSampleTarget(CityBuildingController cbc, out Vector3 randomBuildingPosition, out Quaternion lookRotation)
	{
		randomBuildingPosition = default(Vector3);
		lookRotation = Quaternion.identity;
		Vector3 position = cbc.entranceDoors[0].doorTransform.position;
		if (NavMesh.SamplePosition(position + cbc.entranceDoors[0].doorTransform.forward * 0.5f, out var hit, 2f, NavMeshHelper.NpcNavMeshFilter))
		{
			randomBuildingPosition = hit.position;
			Vector3 normalized = (position - hit.position).normalized;
			normalized.y = 0f;
			if (normalized != Vector3.zero)
			{
				lookRotation = Quaternion.LookRotation(normalized);
			}
			return true;
		}
		return false;
	}
}
