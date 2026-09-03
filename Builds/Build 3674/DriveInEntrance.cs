using System;
using Extensions;
using Helpers;
using UnityEngine;
using Vehicles.Components;

public class DriveInEntrance : GarageDoor
{
	public int doorID;

	[HideInInspector]
	public bool areThereVehiclesInEnterTriggerArea;

	[SerializeField]
	private bool keepOpenWithBusinessOpen;

	[SerializeField]
	private CityBuildingController cityBuildingController;

	[SerializeField]
	private BoxCollider proximityDetectionCollider;

	[SerializeField]
	private BoxCollider garageDoorClosedCollider;

	[SerializeField]
	private Transform vehicleSpawnPositionInsideGarageDoor;

	[SerializeField]
	private float businessPartlyOpenValue = 0.55f;

	private void Start()
	{
		if (cityBuildingController == null)
		{
			Debug.LogError("DriveInEntrance has no CBC assigned! Disabling component...", base.gameObject);
			base.enabled = false;
			return;
		}
		garageDoorClosedCollider.enabled = true;
		if (keepOpenWithBusinessOpen)
		{
			GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(UpdateGarageDoorStatusOnNewHourIfBusinessIsOpen));
			UpdateGarageDoorStatus(instant: true);
		}
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnteredVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitedVehicle));
	}

	private void OnDestroy()
	{
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(UpdateGarageDoorStatusOnNewHourIfBusinessIsOpen));
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnteredVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitedVehicle));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(UpdateGarageDoorStatusOnBuildingRegistrationChange));
	}

	private void OnEnteredVehicle(VehicleController vehicleController)
	{
		if (vehicleController is ScooterController)
		{
			garageDoorClosedCollider.enabled = true;
		}
	}

	private void OnExitedVehicle(VehicleController vehicleController)
	{
		if (vehicleController is ScooterController && BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address) && (keepOpenWithBusinessOpen || vehiclesInside.Count > 0))
		{
			garageDoorClosedCollider.enabled = false;
		}
	}

	public bool TryToEnterWithCar(CarController carController)
	{
		if (cityBuildingController == null)
		{
			return false;
		}
		if (!BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address))
		{
			return false;
		}
		Vector3 frontPoint = carController.FrontPoint;
		Vector3 backPoint = carController.BackPoint;
		Transform transform = base.transform;
		Vector3 b = transform.position - transform.forward * 10f;
		float num = MathHelper.DistanceSqr(frontPoint, b);
		float num2 = MathHelper.DistanceSqr(backPoint, b);
		bool inverseVehicleRotation = num > num2;
		int vehicleSlot = doorID + 1;
		return InstanceBehavior<BuildingManager>.Instance.EnterBuildingWithVehicle(cityBuildingController, inverseVehicleRotation, vehicleSlot);
	}

	public void InstantlyOpenGarageDoor()
	{
		garageDoorClosedCollider.enabled = false;
		VisuallyChangeGarageDoorState(1f, instant: true);
	}

	protected override void OnVehicleEnter()
	{
		OpenGarageDoorIfNeeded();
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(UpdateGarageDoorStatusOnBuildingRegistrationChange));
	}

	protected override void OnVehicleExit()
	{
		CloseGarageDoorIfNeeded();
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(UpdateGarageDoorStatusOnBuildingRegistrationChange));
	}

	private void UpdateGarageDoorStatusOnNewHourIfBusinessIsOpen()
	{
		UpdateGarageDoorStatus(instant: false);
	}

	private void UpdateGarageDoorStatus(bool instant)
	{
		if (BusinessHelper.IsBusinessOpen(cityBuildingController.buildingRegistration))
		{
			OpenGarageDoorIfNeeded(instant);
		}
		else
		{
			CloseGarageDoorIfNeeded(instant);
		}
	}

	private void UpdateGarageDoorStatusOnBuildingRegistrationChange(Address address)
	{
		if (!(address != cityBuildingController.building.Address))
		{
			if (BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address))
			{
				OpenGarageDoorIfNeeded();
			}
			else
			{
				CloseGarageDoorIfNeeded();
			}
		}
	}

	private void OpenGarageDoorIfNeeded(bool instant = false)
	{
		if ((keepOpenWithBusinessOpen || vehiclesInside.Count == 1) && BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address))
		{
			if (!(VehicleHelper.GetCurrentVehicleBase() is ScooterController))
			{
				garageDoorClosedCollider.enabled = false;
			}
			VisuallyChangeGarageDoorState((vehiclesInside.Count == 0 && keepOpenWithBusinessOpen) ? businessPartlyOpenValue : 1f, instant);
		}
	}

	private void CloseGarageDoorIfNeeded(bool instant = false)
	{
		if (vehiclesInside.Count == 0 && !areThereVehiclesInEnterTriggerArea)
		{
			if (keepOpenWithBusinessOpen && BuildingHelper.CanEnterBuilding(cityBuildingController.building.Address))
			{
				VisuallyChangeGarageDoorState(businessPartlyOpenValue, instant);
				return;
			}
			garageDoorClosedCollider.enabled = true;
			VisuallyChangeGarageDoorState(0f, instant);
		}
	}

	public Vector3 GetSpawnPositionInFrontOfGarageDoor(MeshCollider vehicleMeshCollider)
	{
		Transform transform = base.transform;
		return transform.position + transform.forward * (vehicleMeshCollider.sharedMesh.bounds.size.z * 0.5f - proximityDetectionCollider.size.z * 0.5f);
	}

	public Vector3 GetSpawnPositionInsideOfGarageDoor(MeshCollider vehicleMeshCollider)
	{
		return vehicleSpawnPositionInsideGarageDoor.position + vehicleSpawnPositionInsideGarageDoor.forward * (vehicleMeshCollider.sharedMesh.bounds.size.z * 0.5f);
	}

	public void SetUpReferences(CityBuildingController cbc, MeshRenderer meshRenderer)
	{
		cityBuildingController = cbc;
		garageDoorRenderer = meshRenderer;
	}
}
