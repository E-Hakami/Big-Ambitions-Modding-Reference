using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Entities;

[Serializable]
public class FoodDeliveryContract
{
	public Address toAddress;

	public int dayOfDelivery;

	public int hourOfDelivery;

	public float deliveryFee;

	public List<FurnitureDeliveryItem> itemsToDeliver;

	[IgnoreDataMember]
	public float TotalDeliveryPrice
	{
		get
		{
			float num = deliveryFee;
			foreach (FurnitureDeliveryItem item in itemsToDeliver)
			{
				num += (float)item.amount * item.pricePerUnit;
			}
			return num;
		}
	}
}
