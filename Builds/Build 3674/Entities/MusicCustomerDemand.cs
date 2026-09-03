using System.Collections.Generic;
using BigAmbitions.Items;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "MusicCustomerDemand", menuName = "BigAmbitions/CustomerDemands/Music")]
public class MusicCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		foreach (Item item in items)
		{
			if ((ItemsGetter.GetByName(item.itemName).type & ItemType.RadioSource) != 0)
			{
				return true;
			}
		}
		return false;
	}
}
