using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPurchasedItem")]
public class HasPurchasedItem : QuestRequirement
{
	[AutocompleteDropdown("Items")]
	public string[] itemNames;

	public string[] itemTags;

	public int minimumQuantity = 1;

	public bool checkNestedCargoInstances;

	[Tooltip("Special to check on all buildings")]
	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType = "ba:buildingtype_special";

	public override bool CheckIfCompleted()
	{
		if (minimumQuantity <= 0)
		{
			return true;
		}
		int num = 0;
		if (PlayerHelper.IsHoldingItem)
		{
			if (QuestRequirement.ItemMatches(PlayerHelper.ItemInstanceInHands.itemName, itemNames, itemTags))
			{
				num++;
			}
			if (num >= minimumQuantity)
			{
				return true;
			}
			num += ItemQuantityInCargo(itemNames, itemTags, PlayerHelper.ItemInstanceInHands.cargoInstances, minimumQuantity - num);
			if (num >= minimumQuantity)
			{
				return true;
			}
		}
		else if (PlayerHelper.IsUsingVehicle)
		{
			num += ItemQuantityInCargo(itemNames, itemTags, VehicleHelper.GetCurrentVehicle().cargoInstances, minimumQuantity - num);
			if (num >= minimumQuantity)
			{
				return true;
			}
		}
		foreach (BuildingRegistration playerBuildingRegistration in BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter))
		{
			foreach (ItemInstance value in playerBuildingRegistration.itemInstances.Values)
			{
				if (QuestRequirement.ItemMatches(value.itemName, itemNames, itemTags))
				{
					num++;
				}
				num += ItemQuantityInCargo(itemNames, itemTags, value.cargoInstances, minimumQuantity - num);
				if (num >= minimumQuantity)
				{
					return true;
				}
			}
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			num += ItemQuantityInCargo(itemNames, itemTags, vehicleInstance.cargoInstances, minimumQuantity - num);
			if (num >= minimumQuantity)
			{
				return true;
			}
		}
		return num >= minimumQuantity;
	}

	public string[] GetResolvedItemNames()
	{
		if (itemNames.Length != 0)
		{
			return itemNames;
		}
		if (itemTags.Length == 0)
		{
			return Array.Empty<string>();
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (ItemHasAnyTag(allItemController.Item, itemTags))
			{
				string producedItemName = allItemController.GetProducedItemName();
				hashSet.Add(producedItemName);
			}
		}
		string[] array = new string[hashSet.Count];
		hashSet.CopyTo(array);
		return array;
	}

	public bool MatchesItem(string itemName)
	{
		return QuestRequirement.ItemMatches(itemName, itemNames, itemTags);
	}

	private bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (buildingType == "ba:buildingtype_special")
		{
			return true;
		}
		return buildingRegistration.GetBuildingType() == buildingType;
	}

	private int ItemQuantityInCargo(string[] itemNamesToCheck, string[] itemTagsToCheck, IEnumerable<CargoInstance> items, int requiredQuantity)
	{
		int num = 0;
		foreach (CargoInstance item in items)
		{
			if (string.IsNullOrWhiteSpace(item.itemName) || !item.paid)
			{
				continue;
			}
			if (QuestRequirement.ItemMatches(item.itemName, itemNamesToCheck, itemTagsToCheck))
			{
				num += item.amount;
				if (num >= requiredQuantity)
				{
					return num;
				}
			}
			if (checkNestedCargoInstances && item.nestedCargoInstances.Count != 0)
			{
				num += ItemQuantityInNestedCargo(itemNamesToCheck, itemTagsToCheck, requiredQuantity - num, item);
				if (num >= requiredQuantity)
				{
					return num;
				}
			}
		}
		return num;
	}

	private static int ItemQuantityInNestedCargo(string[] itemNamesToCheck, string[] itemTagsToCheck, int requiredQuantity, CargoInstance item)
	{
		int num = 0;
		foreach (NestedCargoInstance nestedCargoInstance in item.nestedCargoInstances)
		{
			if (!string.IsNullOrWhiteSpace(nestedCargoInstance.itemName) && QuestRequirement.ItemMatches(nestedCargoInstance.itemName, itemNamesToCheck, itemTagsToCheck))
			{
				num += nestedCargoInstance.amount;
				if (num >= requiredQuantity)
				{
					return num;
				}
			}
		}
		return num;
	}

	private static bool ItemHasAnyTag(Item item, string[] tags)
	{
		foreach (string tag in tags)
		{
			if (item.HasTag(tag))
			{
				return true;
			}
		}
		return false;
	}
}
