using Helpers;
using UI.Overlays;
using UnityEngine;

namespace Parking.UndergroundParking;

public class ParkingElevator : MonoBehaviour
{
	[SerializeField]
	private Floor currentFloor;

	public void OnTriggerEnter(Collider other)
	{
		if ((other.transform.CompareTag("Player") || other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex) && (other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex || !(other.transform.GetComponentInParent<VehicleController>() != InstanceBehavior<GameManager>.Instance.selectedVehicle)))
		{
			if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.cityBuildingController.undergroundParkingEntrance == null)
			{
				InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0);
				return;
			}
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
			ElevatorOverlay.Show(currentFloor, base.transform.position);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((other.transform.CompareTag("Player") || other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex) && (other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex || !(InstanceBehavior<GameManager>.Instance.selectedVehicle != null) || !(other.transform.GetComponentInParent<VehicleController>() != InstanceBehavior<GameManager>.Instance.selectedVehicle)))
		{
			ElevatorOverlay.Hide();
		}
	}
}
