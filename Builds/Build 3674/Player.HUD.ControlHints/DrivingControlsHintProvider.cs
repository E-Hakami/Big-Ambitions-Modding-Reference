using System;
using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class DrivingControlsHintProvider : ConfigurableControlsHintProvider
{
	[AutocompleteDropdown("VehicleTypes")]
	[SerializeField]
	private List<string> vehicleTypeBlacklist = new List<string>();

	protected override void OnEnable()
	{
		base.OnEnable();
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
	}

	protected override void OnDisable()
	{
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		base.OnDisable();
	}

	private void OnEnterVehicle(VehicleController vehicleController)
	{
		SetActive(!vehicleTypeBlacklist.Contains(vehicleController.vehicleType.vehicleTypeName));
	}

	private void OnExitVehicle(VehicleController _)
	{
		SetActive(active: false);
	}
}
