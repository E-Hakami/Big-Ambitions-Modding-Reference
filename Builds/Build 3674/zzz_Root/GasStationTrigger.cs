using System;
using BigAmbitions.Tags;
using Helpers;
using UnityEngine;
using Vehicles.VehicleTypes;

public class GasStationTrigger : MonoBehaviour
{
	public CityBuildingController cbc;

	public bool isRepairStation;

	public Collider stationCollider;

	public bool isTruckGarage;

	public bool isRefuelStation;

	public Action<GasStationTrigger> onEntered;

	public Action<GasStationTrigger> onExited;

	[SerializeField]
	private SpriteRenderer visuals;

	public void Start()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(UpdateVisuals);
		GlobalEvents.onScreenshotModeToggled = (Action<bool>)Delegate.Combine(GlobalEvents.onScreenshotModeToggled, (Action<bool>)delegate
		{
			UpdateVisuals();
		});
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
	}

	private void UpdateVisuals()
	{
		visuals.enabled = ShouldShowDecal(VehicleHelper.GetCurrentVehicleBase());
	}

	private void OnEnterVehicle(VehicleController vehicle)
	{
		visuals.enabled = ShouldShowDecal(vehicle);
	}

	private void OnExitVehicle(VehicleController vehicle)
	{
		visuals.enabled = ShouldShowDecal(null);
	}

	private bool ShouldShowDecal(VehicleController vehicle)
	{
		if (cbc.building == null || !ScreenshotController.uiIsVisible || ScreenshotController.isInFreeLookMode)
		{
			return false;
		}
		if (isRefuelStation)
		{
			return true;
		}
		if (vehicle == null)
		{
			return false;
		}
		bool flag = vehicle.vehicleType.HasTag(TagRef.Vehicletag.istruck);
		if (isTruckGarage)
		{
			if (!flag)
			{
				return false;
			}
		}
		else if (isRepairStation && flag)
		{
			return false;
		}
		if (!vehicle.vehicleType.spawnInPlayerObject)
		{
			return BuildingHelper.CanEnterBuilding(cbc.building.Address);
		}
		return false;
	}

	public void OnTriggerEnter(Collider other)
	{
		if (IsColliderRelevant(other))
		{
			onEntered?.Invoke(this);
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (IsColliderRelevant(other))
		{
			onExited?.Invoke(this);
		}
	}

	private static bool IsColliderRelevant(Collider other)
	{
		if (!PlayerHelper.IsUsingVehicle)
		{
			return false;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		VehicleType vehicleType = selectedVehicle.vehicleType;
		if (vehicleType.maxCargoCapacity <= 0)
		{
			return false;
		}
		if (!vehicleType.IsMotorVehicle)
		{
			return other.CompareTag("Player");
		}
		return selectedVehicle.vehicleCollider == other;
	}

	public bool IntersectsBounds(Bounds bounds)
	{
		return stationCollider.bounds.Intersects(bounds);
	}
}
