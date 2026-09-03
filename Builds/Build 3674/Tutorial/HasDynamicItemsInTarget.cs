using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial;

public abstract class HasDynamicItemsInTarget : QuestRequirement
{
	[SerializeField]
	protected HasPurchasedDynamicItems hasPurchasedDynamicItemsRequirement;

	[SerializeField]
	protected CustomBuildingTarget customBuildingTarget;

	public override bool CheckIfCompleted()
	{
		TutorialDynamicItems remainingDynamicItems = GetRemainingDynamicItems();
		if (remainingDynamicItems != null && remainingDynamicItems.NoItemsRemaining())
		{
			return AreDynamicItemsToFulfillItemRequirementsInTarget();
		}
		return false;
	}

	public abstract TutorialDynamicItems GetRemainingDynamicItems();

	public abstract TutorialDynamicItems GetRemainingDynamicItemsForTutorialPointers();

	protected bool AreDynamicItemsToFulfillItemRequirementsInTarget()
	{
		TutorialDynamicItems dynamicItemsToFulfillItemRequirements = hasPurchasedDynamicItemsRequirement.GetDynamicItemsToFulfillItemRequirements();
		if (dynamicItemsToFulfillItemRequirements.NoItemsRemaining())
		{
			return true;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return false;
		}
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			dynamicItemsToFulfillItemRequirements.CheckItem(value.itemName);
		}
		return dynamicItemsToFulfillItemRequirements.NoItemsRemaining();
	}
}
