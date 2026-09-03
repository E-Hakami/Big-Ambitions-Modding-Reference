using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using Entities;
using Furniture.Requirements;
using Helpers;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/AnyPrimaryProduct")]
public class AnyPrimaryProduct : BusinessRequirement
{
	protected override bool MustInputLocalizeKey()
	{
		return false;
	}

	protected override bool MustInputTodoTaskItemName()
	{
		return false;
	}

	public override string GetLocalizeKey()
	{
		return "businessrequirement_atleastoneproduct";
	}

	public override string GetHelpLink(BuildingRegistration registration)
	{
		return "businesstypes-" + registration.businessTypeName.GetIdWithoutType().ToLower();
	}

	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.NoProducers;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		return BusinessHelper.IsThereAtLeastOnePrimaryProduct(registration);
	}

	public override string[] GetRequiredItemsForTutorialPointers(BuildingRegistration registration)
	{
		if (registration == null)
		{
			return null;
		}
		return GetItemsNeededToShowcaseTheFirstPrimaryProduct(registration);
	}

	private static string[] GetItemsNeededToShowcaseTheFirstPrimaryProduct(BuildingRegistration registration)
	{
		string value = BusinessTypeHelper.GetData(registration).GetPrimaryProducts().First();
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if (allItem.itemsThatCanShowcase.Contains(value))
			{
				string itemAttachedRequired = GetItemAttachedRequired(allItem.itemName);
				return string.IsNullOrEmpty(itemAttachedRequired) ? new string[1] { allItem.itemName } : new string[2] { allItem.itemName, itemAttachedRequired };
			}
		}
		return null;
	}

	private static string GetItemAttachedRequired(string itemName)
	{
		foreach (FurnitureRequirement furnitureRequirement in ItemsGetter.GetByName(itemName).furnitureRequirements)
		{
			if (furnitureRequirement is HasItemTypeAttached)
			{
				return "ba:itemname_counter1";
			}
		}
		return null;
	}

	public override string[] GetRequiredItemsForTutorial(BuildingRegistration registration)
	{
		if (registration == null)
		{
			return null;
		}
		HashSet<string> primaryProducts = BusinessTypeHelper.GetData(registration).GetPrimaryProducts();
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string item in primaryProducts)
		{
			foreach (Item allItem in ItemsGetter.AllItems)
			{
				if (allItem.itemsThatCanShowcase.Contains(item))
				{
					hashSet.Add(allItem.itemName);
				}
			}
		}
		return hashSet.ToArray();
	}
}
