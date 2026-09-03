using Parking.UndergroundParking;
using UnityEngine;

public class RandomVehicleDirtiness : MonoBehaviour
{
	[SerializeField]
	private CarFeatures carFeatures;

	private bool _initialized;

	private void OnEnable()
	{
		if (!UndergroundParkingManager.IsInsideParking || !_initialized)
		{
			_initialized = true;
			carFeatures.SetDirtiness(Random.Range(0f, 1f));
		}
	}
}
