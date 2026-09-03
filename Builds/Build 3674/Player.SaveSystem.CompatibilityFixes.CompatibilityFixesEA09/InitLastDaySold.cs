using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class InitLastDaySold : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (ProductMarketEntry productMarketEntry in gameInstance.productMarketEntries)
		{
			foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
			{
				demandValue.lastDaySold = gameInstance.Day;
			}
		}
	}
}
