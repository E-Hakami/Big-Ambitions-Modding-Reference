using BigAmbitions.Tags;
using UnityEngine;
using Vehicles.Components;

public class GasStationRepairGarageDoor : GarageDoor
{
	public bool isTruckGarage;

	[SerializeField]
	private GameObject collidersBlockingTheEntrance;

	[SerializeField]
	private float partlyOpenValue = 0.55f;

	private void Start()
	{
		VisuallyChangeGarageDoorState(partlyOpenValue, instant: true);
	}

	protected override void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Vehicle"))
		{
			return;
		}
		VehicleController componentInParent = other.GetComponentInParent<VehicleController>();
		if (!(componentInParent == null))
		{
			bool flag = componentInParent.vehicleType.HasTag(TagRef.Vehicletag.istruck);
			if ((!isTruckGarage || flag) && !(!isTruckGarage & flag))
			{
				base.OnTriggerEnter(other);
			}
		}
	}

	protected override void OnVehicleEnter()
	{
		OpenDoor();
	}

	protected override void OnVehicleExit()
	{
		CloseDoor();
	}

	public void OpenDoor()
	{
		collidersBlockingTheEntrance.SetActive(value: false);
		if (vehiclesInside.Count == 1)
		{
			VisuallyChangeGarageDoorState(1f);
		}
	}

	private void CloseDoor()
	{
		if (vehiclesInside.Count == 0)
		{
			VisuallyChangeGarageDoorState(partlyOpenValue);
			collidersBlockingTheEntrance.SetActive(value: true);
		}
	}
}
