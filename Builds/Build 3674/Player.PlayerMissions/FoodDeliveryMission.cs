using System.Collections.Generic;
using BigAmbitions.Items;
using Player.FoodDeliveryJob;

namespace Player.PlayerMissions;

public class FoodDeliveryMission : PlayerMission
{
	public Address destinationAddress;

	public List<ItemAmountTarget> items;

	public float deliveryReward;

	public override bool TryDeliverToAddress(Address address)
	{
		return FoodDeliveryJobHelper.TryDeliverToAddress(address);
	}
}
