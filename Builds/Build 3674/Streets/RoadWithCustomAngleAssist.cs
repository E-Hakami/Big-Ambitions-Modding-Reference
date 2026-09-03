using Helpers;
using UnityEngine;

namespace Streets;

public class RoadWithCustomAngleAssist : MonoBehaviour
{
	private const string PlayerTag = "Player";

	[SerializeField]
	private float[] anglesAssists;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			AddCustomAnglesAssists();
		}
		else if (VehicleHelper.IsColliderFromCurrentVehicle(other))
		{
			AddCustomAnglesAssists();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			RemoveCustomAnglesAssists();
		}
		else if (VehicleHelper.IsColliderFromCurrentVehicle(other))
		{
			RemoveCustomAnglesAssists();
		}
	}

	private void AddCustomAnglesAssists()
	{
		float[] array = anglesAssists;
		foreach (float item in array)
		{
			AngleAssist.ValidRoadAngles.Add(item);
		}
	}

	private void RemoveCustomAnglesAssists()
	{
		float[] array = anglesAssists;
		foreach (float item in array)
		{
			AngleAssist.ValidRoadAngles.Remove(item);
		}
	}
}
