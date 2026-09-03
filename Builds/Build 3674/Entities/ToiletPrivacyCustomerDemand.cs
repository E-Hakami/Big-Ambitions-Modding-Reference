using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "ToiletPrivacyCustomerDemand", menuName = "BigAmbitions/CustomerDemands/ToiletPrivacy")]
public class ToiletPrivacyCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		bool flag = false;
		bool flag2 = false;
		foreach (Item item in items)
		{
			if (item.HasTag(TagRef.Itemtag.isprivacytoilet))
			{
				flag = true;
			}
			if ((item.type & ItemType.Toilet) != 0)
			{
				flag2 = true;
			}
			if (flag | flag2)
			{
				break;
			}
		}
		if (!flag)
		{
			return !flag2;
		}
		return true;
	}
}
