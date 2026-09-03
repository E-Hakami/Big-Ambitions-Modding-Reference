using UI.Topbar.Accessories;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class InitializeAccessoriesUI : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.accessoriesData == null)
		{
			gameInstance.accessoriesData = new AccessoriesData();
		}
	}
}
