using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class SetPlayerMonopolyInDemands : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (ProductMarketEntry productMarketEntry in gameInstance.productMarketEntries)
		{
			foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
			{
				demandValue.RecalculateIfPlayerHasMonopoly(productMarketEntry.itemName);
			}
		}
	}
}
