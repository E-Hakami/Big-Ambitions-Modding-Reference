using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using HGAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/SpecificItemInShelves")]
public class SpecificItemsInShelves : BusinessRequirement
{
	[AutocompleteDropdown("Items")]
	public string[] items;

	public override string GetLocalizeKey()
	{
		if (!MustInputLocalizeKey())
		{
			return items[0];
		}
		return localizeKey;
	}

	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.MissingRequiredItem;
	}

	protected override bool MustInputLocalizeKey()
	{
		string[] array = items;
		if (array == null)
		{
			return false;
		}
		return array.Length > 1;
	}

	protected override bool MustInputTodoTaskItemName()
	{
		string[] array = items;
		if (array == null)
		{
			return false;
		}
		return array.Length > 1;
	}

	public override string[] GetItems()
	{
		return items;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		string[] array = items;
		foreach (string item in array)
		{
			if (!registration.cachedAvailableProducts.Contains(item))
			{
				return false;
			}
		}
		return true;
	}

	public override string[] GetRequiredItemsForTutorial(BuildingRegistration registration)
	{
		HashSet<string> hashSet = new HashSet<string>();
		string[] array = items;
		foreach (string value in array)
		{
			foreach (Item allItem in ItemsGetter.AllItems)
			{
				if (allItem.itemsThatCanShowcase.Contains(value))
				{
					hashSet.Add(allItem.itemName);
				}
			}
		}
		return hashSet.ToArray();
	}

	public override string[] GetRequiredItemsForTutorialPointers(BuildingRegistration registration)
	{
		List<string> list = new List<string>();
		string[] array = items;
		foreach (string value in array)
		{
			foreach (Item allItem in ItemsGetter.AllItems)
			{
				if (allItem.itemsThatCanShowcase.Contains(value))
				{
					list.Add(allItem.itemName);
					break;
				}
			}
		}
		return list.ToArray();
	}
}
