using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using HGAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/ItemsOfSpecificTypeInBuilding")]
public class ItemsOfTypeInBuilding : BusinessRequirement
{
	public ItemType itemType;

	[AutocompleteDropdown("Items")]
	public string[] additionalItemsNeededForTutorial;

	private string[] _cachedItems;

	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.MissingRequiredItem;
	}

	public override ItemType GetItemType()
	{
		return itemType;
	}

	protected override bool MustInputLocalizeKey()
	{
		return false;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if ((value.ItemCached.type & itemType) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public override string[] GetRequiredItemsForTutorialPointers(BuildingRegistration registration)
	{
		string text = null;
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if (allItem.type.HasFlag(itemType))
			{
				text = allItem.itemName;
				break;
			}
		}
		if (additionalItemsNeededForTutorial == null || additionalItemsNeededForTutorial.Length == 0)
		{
			return new string[1] { text };
		}
		string[] array = new string[1 + additionalItemsNeededForTutorial.Length];
		array[0] = text;
		additionalItemsNeededForTutorial.CopyTo(array, 1);
		return array;
	}

	public override string[] GetItems()
	{
		if (_cachedItems != null)
		{
			return _cachedItems;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if (allItem.type.HasFlag(itemType))
			{
				hashSet.Add(allItem.itemName);
			}
		}
		_cachedItems = hashSet.ToArray();
		return _cachedItems;
	}
}
