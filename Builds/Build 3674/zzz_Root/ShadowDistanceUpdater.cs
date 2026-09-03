using System;
using System.Collections;
using Parking.UndergroundParking;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowDistanceUpdater : MonoBehaviour
{
	public bool isIndoor;

	public bool isVehicle;

	private Volume _volume;

	private IEnumerator Start()
	{
		_volume = GetComponent<Volume>();
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			UpdateVolume();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			UpdateVolume();
		});
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate
		{
			UpdateVolume();
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			UpdateVolume();
		});
		PurchaseVehicleUI.OutdoorCameraToggled.AddListener(delegate(bool toggled)
		{
			if (!isVehicle)
			{
				if (toggled)
				{
					_volume.enabled = !isIndoor;
				}
				else
				{
					_volume.enabled = isIndoor;
				}
			}
		});
		yield return null;
		GlobalEvents.RegisterOnGameLoadedCallback(UpdateVolume);
	}

	private void UpdateVolume()
	{
		int num;
		if (SaveGameManager.Current?.ActiveVehicleId != null)
		{
			VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
			num = (((object)selectedVehicle == null || !selectedVehicle.vehicleType.spawnInPlayerObject) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		bool flag2 = BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking;
		_volume.enabled = (flag2 && isIndoor) || (flag && !flag2 && isVehicle) || (!flag2 && !flag && !isVehicle && !isIndoor);
	}
}
