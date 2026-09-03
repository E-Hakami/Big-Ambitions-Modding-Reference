using System.Collections.Generic;
using BigAmbitions.Items;
using Blueprints;
using BusinessLayoutSets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class FixMissingDeliverySpot : ICompatibilityFix
{
	private readonly Dictionary<string, BusinessLayoutSet> _defaultLayoutCache = new Dictionary<string, BusinessLayoutSet>();

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			foreach (ItemInstance value2 in buildingRegistration.itemInstances.Values)
			{
				if (value2.itemName == "ba:itemname_deliveryspot")
				{
					flag = true;
				}
				else if (value2.itemName == "ba:itemname_handtruckspawner")
				{
					flag2 = true;
				}
				if (flag & flag2)
				{
					break;
				}
			}
			if (flag & flag2)
			{
				continue;
			}
			BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(buildingRegistration);
			string key = buildingSizeInfo.ToString();
			BusinessLayoutSet businessLayoutSet;
			if (_defaultLayoutCache.TryGetValue(key, out var value))
			{
				businessLayoutSet = value;
			}
			else
			{
				businessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet("ba:businesstype_empty", buildingSizeInfo, "default");
				if (businessLayoutSet != null)
				{
					_defaultLayoutCache.Add(key, businessLayoutSet);
				}
			}
			if (businessLayoutSet != null)
			{
				if (!flag)
				{
					AddMissingItems(buildingRegistration, businessLayoutSet, "ba:itemname_deliveryspot");
				}
				if (!flag2)
				{
					AddMissingItems(buildingRegistration, businessLayoutSet, "ba:itemname_handtruckspawner");
				}
			}
		}
		_defaultLayoutCache.Clear();
	}

	private static void AddMissingItems(BuildingRegistration registration, BusinessLayoutSet defaultLayout, string itemName)
	{
		foreach (BusinessLayoutSets.Item item in defaultLayout.Items)
		{
			if (!(item.itemName != itemName))
			{
				ItemInstance itemInstance = ItemHelper.InitializeNewInstance(item.itemName);
				Quaternion quaternion = item.rotation;
				itemInstance.position = item.position;
				itemInstance.yRotation = quaternion.eulerAngles.y;
				itemInstance.customValue = item.customValue;
				itemInstance.dirtSpotsThatAffects = item.dirtSpotsThatAffects;
				itemInstance.customPositions = item.customPositions;
				itemInstance.customColors = item.customColors;
				registration.AddItemInstanceToBuilding(itemInstance);
				break;
			}
		}
	}
}
