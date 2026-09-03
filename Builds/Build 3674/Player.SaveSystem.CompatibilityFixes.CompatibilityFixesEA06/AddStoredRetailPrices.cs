using System.Collections.Generic;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class AddStoredRetailPrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			buildingRegistration.storedRetailPrices = new List<RetailPrice>();
			foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
			{
				buildingRegistration.storedRetailPrices.Add(new RetailPrice
				{
					itemName = retailPrice.itemName,
					price = retailPrice.price
				});
			}
		}
	}
}
