using Helpers;
using UnityEngine;
using UnityEngine.AI;
using Vehicles.Components;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverGarageDoor : GarageDoor
{
	[SerializeField]
	private Collider entranceBlocker;

	[SerializeField]
	private NavMeshObstacle entranceObstacle;

	protected override void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Vehicle") && !(other.GetComponentInParent<VehicleController>() == null))
		{
			base.OnTriggerEnter(other);
		}
	}

	protected override void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			TryCloseDoor();
		}
		base.OnTriggerExit(other);
	}

	protected override void OnVehicleEnter()
	{
		OpenDoor();
	}

	protected override void OnVehicleExit()
	{
		TryCloseDoor();
	}

	private void OpenDoor()
	{
		entranceBlocker.enabled = false;
		entranceObstacle.enabled = false;
		VisuallyChangeGarageDoorState(1f);
	}

	private void TryCloseDoor()
	{
		vehiclesInside.RemoveAll((VehicleController vehicle) => vehicle == null);
		if (vehiclesInside.Count <= 0 && !IsPlayerInside())
		{
			VisuallyChangeGarageDoorState(0f);
			entranceBlocker.enabled = true;
			entranceObstacle.enabled = true;
		}
	}

	private bool IsPlayerInside()
	{
		return Physics.CheckBox(entranceBlocker.transform.TransformPoint(entranceBlocker.bounds.center), entranceBlocker.bounds.extents, entranceBlocker.transform.rotation, 1 << LayerHelper.PlayerLayerIndex);
	}

	public void InstantCloseDoor()
	{
		VisuallyChangeGarageDoorState(0f, instant: true);
		entranceBlocker.enabled = true;
		entranceObstacle.enabled = true;
	}
}
