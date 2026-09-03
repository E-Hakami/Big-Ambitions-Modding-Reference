using BigAmbitions.Items;
using Entities;
using HGAttributes;
using UnityEngine;

namespace Buildings.BuildingTypes.Shared.BusinessRequirement;

[CreateAssetMenu(menuName = "BigAmbitions/BusinessRequirement/SpecificItemsInBuilding")]
public class SpecificItemsInBuilding : BusinessRequirement
{
	[AutocompleteDropdown("Items")]
	public string[] items;

	[AutocompleteDropdown("Items")]
	public string[] additionalItemsNeededForTutorial;

	public override TodoTaskType GetTodoTaskType()
	{
		return TodoTaskType.MissingRequiredItem;
	}

	public override string[] GetItems()
	{
		return items;
	}

	protected override bool MustInputLocalizeKey()
	{
		return false;
	}

	public override bool IsRequirementMet(BuildingRegistration registration)
	{
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			string[] array = items;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == value.itemName)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override string[] GetRequiredItemsForTutorialPointers(BuildingRegistration registration)
	{
		string[] requiredItemsForTutorialPointers = base.GetRequiredItemsForTutorialPointers(registration);
		if (additionalItemsNeededForTutorial == null || additionalItemsNeededForTutorial.Length == 0)
		{
			return requiredItemsForTutorialPointers;
		}
		string[] array = new string[requiredItemsForTutorialPointers.Length + additionalItemsNeededForTutorial.Length];
		requiredItemsForTutorialPointers.CopyTo(array, 0);
		additionalItemsNeededForTutorial.CopyTo(array, requiredItemsForTutorialPointers.Length);
		return array;
	}
}
