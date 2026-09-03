using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "SinkCustomerDemand", menuName = "BigAmbitions/CustomerDemands/Sink")]
public class SinkCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		foreach (Item item in items)
		{
			if ((item.type & ItemType.Sink) != 0)
			{
				return true;
			}
		}
		return false;
	}
}
