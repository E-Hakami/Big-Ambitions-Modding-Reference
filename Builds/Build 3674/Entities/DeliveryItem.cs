using System;

namespace Entities;

[Serializable]
public class DeliveryItem
{
	public string itemName;

	public int amountDelivered;

	public DeliveryItem(string itemName, int amountDelivered)
	{
		this.itemName = itemName;
		this.amountDelivered = amountDelivered;
	}

	public DeliveryItem()
	{
	}
}
