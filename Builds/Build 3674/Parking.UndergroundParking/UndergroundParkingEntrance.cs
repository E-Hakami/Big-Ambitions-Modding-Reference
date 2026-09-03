using System;
using System.Linq;
using Helpers;
using UI;
using UnityEngine;

namespace Parking.UndergroundParking;

public class UndergroundParkingEntrance : MonoBehaviour
{
	public int parkingNumber;

	public int parkingVersion;

	public Transform playerExitPoint;

	public Transform vehicleExitPoint;

	public CityBuildingController parentCbc;

	public PointOfInterest poi;

	public Address Address => new Address("ba:street_parking", parkingNumber);

	private void Awake()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(Init);
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(UpdateParkingState));
	}

	private void Init()
	{
		poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(base.transform, InstanceBehavior<UIs>.Instance.mapFilters.undergroundParkingIcon, InstanceBehavior<UIs>.Instance.mapFilters.undergroundParkingFilterColor);
	}

	private void UpdateParkingState(bool cityMapOpen)
	{
		poi.SetHidden(!cityMapOpen || SaveGameManager.Current.VehicleInstances.All((VehicleInstance x) => x.Address != Address));
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.CompareTag("Player") || other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex)
		{
			VehicleController component = other.attachedRigidbody.GetComponent<VehicleController>();
			if (other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex || (!(component != InstanceBehavior<GameManager>.Instance.selectedVehicle) && !(InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.Address == Address)))
			{
				UndergroundParkingManager.EnterParking(this);
			}
		}
	}
}
