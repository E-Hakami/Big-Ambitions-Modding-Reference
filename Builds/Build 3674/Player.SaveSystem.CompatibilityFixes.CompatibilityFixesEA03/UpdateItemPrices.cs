using System.Linq;
using AI.Citizens;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class UpdateItemPrices : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_midtown";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && x.Neighborhood == "ba:neighborhood_midtown"))
		{
			float num = CitizenHelper.MaxAcceptableRelativePrice(SocialClass.Upper, "ba:neighborhood_midtown");
			foreach (RetailPrice retailPrice in item.retailPrices)
			{
				float defaultMarketPrice = ItemHelper.GetDefaultMarketPrice(retailPrice.itemName);
				if (defaultMarketPrice != 0f && retailPrice.price / defaultMarketPrice > num)
				{
					retailPrice.price = Mathf.Floor(num * defaultMarketPrice);
				}
			}
		}
	}
}
