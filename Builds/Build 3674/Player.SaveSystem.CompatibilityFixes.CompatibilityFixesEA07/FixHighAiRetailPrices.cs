using AI.Citizens;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixHighAiRetailPrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				buildingRegistration.retailPrices.Clear();
				continue;
			}
			foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
			{
				float num = ItemHelper.CalculateOptimalPriceByNeighborhood(retailPrice.itemName, buildingRegistration.Neighborhood);
				if (retailPrice.price > num)
				{
					retailPrice.price = num;
				}
			}
		}
		CompetitionHelper.RecalculateRetailPricesForAll();
	}
}
