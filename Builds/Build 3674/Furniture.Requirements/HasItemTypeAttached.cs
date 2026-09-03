using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Streets;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/HasItemTypeAttached")]
public class HasItemTypeAttached : FurnitureRequirement
{
	public ItemType itemType;

	[NonSerialized]
	private string[] _itemsFittingRequirementCached;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		BuildingRegistration buildingRegistration = itemInstance.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return true;
		}
		ItemInstance value = itemInstance;
		if (!string.IsNullOrEmpty(itemInstance.parentId))
		{
			if (buildingRegistration.itemInstances.TryGetValue(itemInstance.parentId, out value))
			{
				if ((value.ItemCached.type & itemType) != 0)
				{
					return true;
				}
			}
			else
			{
				Debug.LogError("Item " + itemInstance.itemName + " in " + buildingRegistration.Address.ToFormattedString() + " has non-existent parent with ID '" + itemInstance.parentId + "'");
				value = itemInstance;
			}
		}
		foreach (AttachableChild stackedItem in value.stackedItems)
		{
			if ((ItemsGetter.GetByName(stackedItem.childItemName).type & itemType) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public string[] GetAllItemsFittingRequirement()
	{
		if (_itemsFittingRequirementCached != null)
		{
			return _itemsFittingRequirementCached;
		}
		List<string> list = new List<string>();
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if ((allItem.type & itemType) != 0)
			{
				list.Add(allItem.itemName);
			}
		}
		_itemsFittingRequirementCached = list.ToArray();
		return _itemsFittingRequirementCached;
	}
}
