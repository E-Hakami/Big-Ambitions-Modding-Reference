using System.Collections.Generic;
using Helpers;
using JimmysUnityUtilities;
using UI.ItemPanel;
using UnityEngine;

public class DriveInEntranceEnterTrigger : MonoBehaviour
{
	[SerializeField]
	private DriveInEntrance driveInEntrance;

	private List<string> _vehiclesInside = new List<string>();

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerHelper.VehiclesLayerIndex && !other.CompareTag("AiVehicleHalt") && other.TryGetComponentInParent<CarController>(out var component) && !(InstanceBehavior<GameManager>.Instance == null) && !(component != InstanceBehavior<GameManager>.Instance.selectedVehicle) && !driveInEntrance.TryToEnterWithCar(component))
		{
			ItemPanelUI.VehiclesThatCanNotBeParked.Add(component.vehicleInstance.id);
			_vehiclesInside.Add(component.vehicleInstance.id);
			driveInEntrance.areThereVehiclesInEnterTriggerArea = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponentInParent<CarController>(out var component))
		{
			ItemPanelUI.VehiclesThatCanNotBeParked.Remove(component.vehicleInstance.id);
			_vehiclesInside.Remove(component.vehicleInstance.id);
			if (_vehiclesInside.Count == 0)
			{
				driveInEntrance.areThereVehiclesInEnterTriggerArea = false;
			}
		}
	}
}
