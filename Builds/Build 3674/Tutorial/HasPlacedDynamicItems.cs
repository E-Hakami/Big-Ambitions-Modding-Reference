using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPlacedDynamicItems")]
public class HasPlacedDynamicItems : HasDynamicItemsInTarget
{
	public override TutorialDynamicItems GetRemainingDynamicItems()
	{
		TutorialDynamicItems dynamicItems = hasPurchasedDynamicItemsRequirement.GetDynamicItems();
		if (dynamicItems.invalid)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			dynamicItems.CheckItem(value.itemName);
		}
		return dynamicItems;
	}

	public override TutorialDynamicItems GetRemainingDynamicItemsForTutorialPointers()
	{
		TutorialDynamicItems dynamicItemsForTutorialPointers = hasPurchasedDynamicItemsRequirement.GetDynamicItemsForTutorialPointers();
		if (dynamicItemsForTutorialPointers.invalid)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			dynamicItemsForTutorialPointers.CheckItem(value.itemName);
		}
		return dynamicItemsForTutorialPointers;
	}
}
