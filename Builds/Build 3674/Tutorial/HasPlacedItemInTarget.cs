using BigAmbitions.Items;
using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPlacedItemInTarget")]
public class HasPlacedItemInTarget : QuestRequirement
{
	public CustomBuildingTarget customBuildingTarget;

	[AutocompleteDropdown("Items")]
	public string[] itemNames;

	public string[] itemTags;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return false;
		}
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (QuestRequirement.ItemMatches(value.itemName, itemNames, itemTags))
			{
				return true;
			}
		}
		return false;
	}
}
