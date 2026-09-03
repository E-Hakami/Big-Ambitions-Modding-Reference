using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class RemoveOrphanedPricingManagerPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		List<PricingManagerPlan> pricingManagerPlans = gameInstance.pricingManagerPlans;
		for (int num = pricingManagerPlans.Count - 1; num >= 0; num--)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(pricingManagerPlans[num].headquartersAddress);
			if (buildingRegistration != null && !buildingRegistration.RentedByPlayer)
			{
				pricingManagerPlans.RemoveAt(num);
			}
		}
	}
}
