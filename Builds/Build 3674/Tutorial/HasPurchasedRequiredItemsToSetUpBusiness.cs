using System.Collections.Generic;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPurchasedRequiredItemsToSetUpBusiness")]
public class HasPurchasedRequiredItemsToSetUpBusiness : HasPurchasedDynamicItems
{
	protected override void SetDynamicItems()
	{
		dynamicItems.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			dynamicItems.invalid = true;
			return;
		}
		List<BusinessRequirement> businessRequirements = BusinessTypeHelper.GetData(buildingRegistration).businessRequirements;
		if (businessRequirements == null || businessRequirements.Count == 0)
		{
			dynamicItems.invalid = true;
			return;
		}
		foreach (BusinessRequirement item in businessRequirements)
		{
			string[] requiredItemsForTutorial = item.GetRequiredItemsForTutorial(buildingRegistration);
			if (requiredItemsForTutorial != null && requiredItemsForTutorial.Length != 0)
			{
				dynamicItems.AddCollection(requiredItemsForTutorial);
			}
		}
	}

	protected override void SetDynamicItemsForTutorialPointers()
	{
		dynamicItemsForTutorialPointers.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			dynamicItemsForTutorialPointers.invalid = true;
			return;
		}
		List<BusinessRequirement> businessRequirements = BusinessTypeHelper.GetData(buildingRegistration).businessRequirements;
		if (businessRequirements == null || businessRequirements.Count == 0)
		{
			dynamicItemsForTutorialPointers.invalid = true;
			return;
		}
		foreach (BusinessRequirement item in businessRequirements)
		{
			string[] requiredItemsForTutorialPointers = item.GetRequiredItemsForTutorialPointers(buildingRegistration);
			if (requiredItemsForTutorialPointers != null && requiredItemsForTutorialPointers.Length != 0)
			{
				string[] array = requiredItemsForTutorialPointers;
				foreach (string text in array)
				{
					dynamicItemsForTutorialPointers.AddCollection(new string[1] { text });
				}
			}
		}
	}
}
