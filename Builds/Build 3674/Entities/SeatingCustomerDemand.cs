using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "SeatingCustomerDemand", menuName = "BigAmbitions/CustomerDemands/Seating")]
public class SeatingCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		foreach (Item item in items)
		{
			if ((ItemsGetter.GetByName(item.itemName).type & ItemType.Seat) != 0)
			{
				return true;
			}
		}
		return false;
	}
}
