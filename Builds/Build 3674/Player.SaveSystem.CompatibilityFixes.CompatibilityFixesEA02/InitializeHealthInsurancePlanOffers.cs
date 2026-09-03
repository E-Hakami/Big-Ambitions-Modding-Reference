using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class InitializeHealthInsurancePlanOffers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.healthInsurancePlanOffers = new List<HealthInsurancePlanOffer>();
	}
}
