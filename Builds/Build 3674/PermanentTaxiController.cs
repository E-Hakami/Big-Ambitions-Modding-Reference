using GleyTrafficSystem;
using Helpers;
using UnityEngine;
using Vehicles.Taxis;

public class PermanentTaxiController : EntityController, ITaxi
{
	[SerializeField]
	private VehicleComponent vehiclePrefab;

	public void OnClickToUseTaxi()
	{
		if (!VehicleHelper.IsInsideVehicle())
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
			{
				InstanceBehavior<CityManager>.Instance.cityMap.SetTaxiMode(this);
			});
		}
	}

	public void DriveAway()
	{
	}

	public VehicleComponent GetVehiclePrefab()
	{
		return vehiclePrefab;
	}

	public VehicleComponent InstantiateVehicle(Waypoint waypoint)
	{
		return TrafficManager.Instance.LoadVehicle(vehiclePrefab.gameObject, waypoint);
	}

	public string GetHappinessModifierName()
	{
		return "ba:happinessmodifier_taxi";
	}
}
