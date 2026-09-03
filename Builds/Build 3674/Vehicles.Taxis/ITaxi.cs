using GleyTrafficSystem;

namespace Vehicles.Taxis;

public interface ITaxi
{
	void DriveAway();

	VehicleComponent GetVehiclePrefab();

	VehicleComponent InstantiateVehicle(Waypoint waypoint);

	float GetTimeMultiplier()
	{
		return 1f;
	}

	void OnTravelFinished()
	{
	}

	string GetHappinessModifierName();
}
