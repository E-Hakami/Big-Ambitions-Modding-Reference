using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateRealStateRentValues : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (RealEstate item in gameInstance.realEstate)
		{
			float buildingDailyMarketRentPerSqm = item.Building.GetBuildingDailyMarketRentPerSqm();
			float num = (float)Mathf.RoundToInt(buildingDailyMarketRentPerSqm * 0.8f * 100f) / 100f;
			if (item.pendingPricePerSqm > num)
			{
				item.pricePerSqm = item.pendingPricePerSqm;
			}
			else if (item.pricePerSqm < num)
			{
				item.pricePerSqm = buildingDailyMarketRentPerSqm;
				item.pendingPricePerSqm = buildingDailyMarketRentPerSqm;
			}
			item.daysUntilUpdatingPricePerSqm = 0;
		}
	}
}
