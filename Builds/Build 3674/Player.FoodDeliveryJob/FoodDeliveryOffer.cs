using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;

namespace Player.FoodDeliveryJob;

public class FoodDeliveryOffer
{
	public Address pickupAddress;

	public Address destinationAddress;

	public List<ItemAmountTarget> items;

	public Timestamp expireTime;

	public float deliveryReward;

	public int timeLimitMinutes;

	public bool IsExpired()
	{
		return expireTime.IsInThePast();
	}
}
