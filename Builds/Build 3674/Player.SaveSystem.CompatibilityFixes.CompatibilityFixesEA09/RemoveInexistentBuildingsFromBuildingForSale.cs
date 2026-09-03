namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveInexistentBuildingsFromBuildingForSale : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.buildingsForSale.RemoveAll((BuildingForSale x) => x.Building == null);
	}
}
