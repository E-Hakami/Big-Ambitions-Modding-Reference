using Extensions;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class SetUpGameInstanceSeed : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.seed = RngHelper.GenerateGameSeed();
	}
}
