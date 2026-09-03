using GleyTrafficSystem;
using Helpers;
using UI;
using UnityEngine;
using Vehicles.Taxis;

public class TaxiController : EntityController, ITaxi
{
	private float _lastActionValue;

	private SpecialDriveActionTypes _lastDriveAction;

	private VehicleComponent _vehicleComponent;

	protected override int DefaultLayer => LayerHelper.AiVehiclesLayerIndex;

	public override void Awake()
	{
		if (!InstanceBehavior<UIs>.IsInitialized)
		{
			Object.Destroy(this);
		}
		else
		{
			base.Awake();
		}
	}

	public override void Start()
	{
		base.Start();
		_vehicleComponent = GetComponent<VehicleComponent>();
	}

	public void OnClickToUseTaxi()
	{
		if (!VehicleHelper.IsInsideVehicle())
		{
			RequestVehicleStop();
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
			{
				InstanceBehavior<CityManager>.Instance.cityMap.SetTaxiMode(this);
			});
		}
	}

	private void RequestVehicleStop()
	{
		(_lastActionValue, _lastDriveAction) = TrafficManager.Instance.GetCurrentDrivingState(_vehicleComponent.GetIndex());
		AIEvents.TriggerChangeDrivingStateEvent(_vehicleComponent.GetIndex(), SpecialDriveActionTypes.StopNow, 60f);
	}

	public void DriveAway()
	{
		AIEvents.TriggerChangeDrivingStateEvent(_vehicleComponent.GetIndex(), _lastDriveAction, _lastActionValue);
	}

	public VehicleComponent GetVehiclePrefab()
	{
		return _vehicleComponent.prefab.GetComponent<VehicleComponent>();
	}

	public VehicleComponent InstantiateVehicle(Waypoint waypoint)
	{
		Manager.RemoveVehicle(base.gameObject);
		return TrafficManager.Instance.LoadVehicle(_vehicleComponent.prefab, waypoint);
	}

	public string GetHappinessModifierName()
	{
		return "ba:happinessmodifier_taxi";
	}
}
