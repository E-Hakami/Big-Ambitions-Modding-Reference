using System;
using System.Collections.Generic;

namespace Entities;

[Serializable]
public class DeliveryTransaction
{
	public int dayOfDelivery;

	public readonly List<DeliveryItem> deliveryItems = new List<DeliveryItem>();

	public void SetItems(IEnumerable<DeliveryItem> items)
	{
		dayOfDelivery = SaveGameManager.Current.Day;
		deliveryItems.Clear();
		deliveryItems.AddRange(items);
	}
}
