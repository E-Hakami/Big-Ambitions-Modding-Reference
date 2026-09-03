using System.Collections.Generic;
using BigAmbitions.Tags;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixRetailPrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && !buildingRegistration.AvailableForRent && buildingRegistration.businessTypeName != "ba:businesstype_empty" && !BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowplayercreation))
			{
				List<RetailPrice> retailPrices = buildingRegistration.retailPrices;
				if (retailPrices != null && retailPrices.Count > 0)
				{
					foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
					{
						retailPrice.price = ItemHelper.GetDefaultMarketPrice(retailPrice.itemName);
					}
					continue;
				}
			}
			if (buildingRegistration.RentedByPlayer && buildingRegistration.businessTypeName == "ba:businesstype_empty")
			{
				buildingRegistration.retailPrices.Clear();
			}
		}
	}
}
