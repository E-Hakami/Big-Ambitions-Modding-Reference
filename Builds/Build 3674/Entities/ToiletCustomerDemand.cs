using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "ToiletCustomerDemand", menuName = "BigAmbitions/CustomerDemands/Toilet")]
public class ToiletCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		foreach (Item item in items)
		{
			if ((item.type & ItemType.Toilet) != 0)
			{
				return true;
			}
		}
		return false;
	}
}
