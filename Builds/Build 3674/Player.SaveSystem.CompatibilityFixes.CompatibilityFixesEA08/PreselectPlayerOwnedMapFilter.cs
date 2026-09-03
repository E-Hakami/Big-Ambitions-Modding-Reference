namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class PreselectPlayerOwnedMapFilter : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.SelectedCitymapFilters != null && !gameInstance.SelectedCitymapFilters.Contains("buildingresume_rented_by_you"))
		{
			gameInstance.SelectedCitymapFilters.Add("buildingresume_rented_by_you");
		}
	}
}
