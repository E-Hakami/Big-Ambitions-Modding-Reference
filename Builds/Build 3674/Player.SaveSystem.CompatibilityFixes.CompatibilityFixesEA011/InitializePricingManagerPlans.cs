using System.Collections.Generic;
using Buildings.Office.Headquarters;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class InitializePricingManagerPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.pricingManagerPlans == null)
		{
			gameInstance.pricingManagerPlans = new List<PricingManagerPlan>();
		}
	}
}
