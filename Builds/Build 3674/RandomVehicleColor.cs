using Helpers;
using Parking.UndergroundParking;
using UnityEngine;

public class RandomVehicleColor : MonoBehaviour
{
	[SerializeField]
	private CarFeatures carFeatures;

	private bool _initialized;

	private void OnEnable()
	{
		if (!UndergroundParkingManager.IsInsideParking || !_initialized)
		{
			_initialized = true;
			carFeatures.SetColor(VehicleHelper.GetRandomVehicleColor());
		}
	}
}
