using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class InitializeFoodDeliveryContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.FoodDeliveryContracts == null)
		{
			gameInstance.FoodDeliveryContracts = new List<FoodDeliveryContract>();
		}
	}
}
