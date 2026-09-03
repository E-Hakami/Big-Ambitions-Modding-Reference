using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateMarketDemands : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		ProductMarketHelper.UpdateMarketDemands(gameInstance);
	}
}
