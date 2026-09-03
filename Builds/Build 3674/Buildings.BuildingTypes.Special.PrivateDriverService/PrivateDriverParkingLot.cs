using Helpers;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverParkingLot : MonoBehaviour
{
	[SerializeField]
	private ParkingLaneGenerator[] parkingLaneGenerators;

	public bool TryGetRandomFreeSpotForPlayerVehicle(out Vector3 spotPosition, out Quaternion spotRotation, bool cleanup = false)
	{
		ParkingLaneGenerator[] array = parkingLaneGenerators;
		foreach (ParkingLaneGenerator parkingLaneGenerator in array)
		{
			if (cleanup)
			{
				parkingLaneGenerator.CleanupParkedVehicles(force: true);
			}
			if (parkingLaneGenerator.TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation, LayerHelper.vehicleSpawnPointMask))
			{
				return true;
			}
		}
		if (!cleanup)
		{
			return TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation, cleanup: true);
		}
		spotPosition = Vector3.zero;
		spotRotation = Quaternion.identity;
		return false;
	}

	public bool TryReserveSpots(int requiredCount, out Vector3[] spotPositions, out Quaternion[] spotRotations, bool cleanup = false)
	{
		int i = 0;
		spotPositions = new Vector3[requiredCount];
		spotRotations = new Quaternion[requiredCount];
		if (requiredCount == 0)
		{
			return true;
		}
		ParkingLaneGenerator[] array;
		if (cleanup)
		{
			array = parkingLaneGenerators;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].CleanupParkedVehicles(force: true);
			}
		}
		array = parkingLaneGenerators;
		foreach (ParkingLaneGenerator parkingLaneGenerator in array)
		{
			for (; i < requiredCount; i++)
			{
				if (!parkingLaneGenerator.TryReserveSpot(out var spotPosition, out var spotRotation, LayerHelper.vehicleSpawnPointMask, LayerHelper.PlayerVehiclesLayerIndex))
				{
					break;
				}
				spotPositions[i] = spotPosition;
				spotRotations[i] = spotRotation;
			}
			if (i >= requiredCount)
			{
				break;
			}
		}
		array = parkingLaneGenerators;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].ReleaseReservedSpots();
		}
		if (i >= requiredCount)
		{
			return true;
		}
		if (!cleanup)
		{
			return TryReserveSpots(requiredCount, out spotPositions, out spotRotations, cleanup: true);
		}
		return false;
	}
}
