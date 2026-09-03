using System.Collections.Generic;
using BigAmbitions.Tags;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPurchasedRequiredInventoryToSetUpBusiness")]
public class HasPurchasedRequiredInventoryToSetUpBusiness : HasPurchasedDynamicItems
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
		List<string> primaryRetailProducts = BusinessTypeHelper.GetPrimaryRetailProducts(buildingRegistration.businessTypeName);
		if (primaryRetailProducts == null || primaryRetailProducts.Count == 0)
		{
			dynamicItems.invalid = true;
			return;
		}
		if (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			dynamicItems.AddCollection(new string[1] { "ba:itemname_paperbag" });
		}
		dynamicItems.AddCollection(primaryRetailProducts.ToArray());
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
		List<string> primaryRetailProducts = BusinessTypeHelper.GetPrimaryRetailProducts(buildingRegistration.businessTypeName);
		if (primaryRetailProducts == null || primaryRetailProducts.Count == 0)
		{
			dynamicItemsForTutorialPointers.invalid = true;
			return;
		}
		if (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			dynamicItemsForTutorialPointers.AddCollection(new string[1] { "ba:itemname_paperbag" });
		}
		dynamicItemsForTutorialPointers.AddCollection(new string[1] { primaryRetailProducts[0] });
	}
}
