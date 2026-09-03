using System;
using System.Collections.Generic;
using Buildings;
using Helpers;
using Vehicles.DeliveryDriverJob;

namespace Player.PlayerMissions;

public class DeliveryDriverMission : PlayerMission
{
	public string vehicleId;

	public Address startAddress;

	public List<DeliveryJobDestination> destinations;

	public float earnings;

	public float deliveryReward;

	public float tips;

	public float damageFees;

	public bool shownFinishNotification;

	public Address pinnedAddress;

	public static Action onPinnedAddressChanged;

	public override bool TryDeliverToAddress(Address address)
	{
		return DeliveryJobHelper.TryDeliverToAddress(address);
	}

	public bool AreAllDestinationsCompleted()
	{
		foreach (DeliveryJobDestination destination in destinations)
		{
			if (!destination.IsCompleted())
			{
				return false;
			}
		}
		return true;
	}

	public int GetCompletedDeliveries()
	{
		int num = 0;
		foreach (DeliveryJobDestination destination in destinations)
		{
			if (destination.IsCompleted())
			{
				num++;
			}
		}
		return num;
	}

	private DeliveryJobStartLocation GetStartLocation()
	{
		Building building = BuildingHelper.GetBuilding(startAddress);
		if (!building)
		{
			return null;
		}
		return building.deliveryJobStartLocation;
	}

	public bool WasFastDelivery()
	{
		if (timeLimitMinutes <= 0 || !AreAllDestinationsCompleted())
		{
			return false;
		}
		DeliveryJobStartLocation startLocation = GetStartLocation();
		DeliveryJobTipsConfig deliveryJobTipsConfig = (startLocation ? startLocation.tipsConfig : null);
		if (!deliveryJobTipsConfig)
		{
			return false;
		}
		return deliveryJobTipsConfig.IsFastDelivery(endTime.GetDifferenceInMinutes(startTime), timeLimitMinutes);
	}

	public void CalculateTips()
	{
		DeliveryJobStartLocation startLocation = GetStartLocation();
		DeliveryJobTipsConfig deliveryJobTipsConfig = (startLocation ? startLocation.tipsConfig : null);
		if (!deliveryJobTipsConfig || deliveryJobTipsConfig.tipChances == null || deliveryJobTipsConfig.tipChances.Length == 0)
		{
			tips = 0f;
			return;
		}
		bool wasFastDelivery = WasFastDelivery();
		foreach (DeliveryJobDestination destination in destinations)
		{
			if (destination.IsCompleted())
			{
				tips += deliveryJobTipsConfig.RollTip(wasFastDelivery);
			}
		}
	}

	public void SetPinnedAddress(Address address)
	{
		pinnedAddress = address;
		onPinnedAddressChanged?.Invoke();
	}
}
