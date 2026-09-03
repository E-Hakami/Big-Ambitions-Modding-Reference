using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Entities;

[Serializable]
public class FurnitureDeliveryContract
{
	public Address fromAddress;

	public Address toAddress;

	public List<FurnitureDeliveryItem> itemsToDeliver;

	public int dayOfDelivery;

	public int hourOfDelivery;

	public float deliveryFee;

	[IgnoreDataMember]
	public float TotalDeliveryPrice => itemsToDeliver.Sum((FurnitureDeliveryItem x) => (float)x.amount * x.pricePerUnit) + deliveryFee;
}
