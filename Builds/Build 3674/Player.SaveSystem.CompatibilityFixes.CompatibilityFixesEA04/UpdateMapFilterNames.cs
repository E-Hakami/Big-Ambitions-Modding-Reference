namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateMapFilterNames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		for (int i = 0; i < gameInstance.SelectedCitymapFilters.Count; i++)
		{
			if (gameInstance.SelectedCitymapFilters[i] == "specialbuilding_appliancestore")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_appliancestore";
			}
			else if (gameInstance.SelectedCitymapFilters[i] == "specialbuilding_bank")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_bank";
			}
			else if (gameInstance.SelectedCitymapFilters[i] == "specialbuilding_officesupplystore")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_officesupplystore";
			}
			else if (gameInstance.SelectedCitymapFilters[i] == "specialbuilding_wholesalestore")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_wholesalestore";
			}
			else if (gameInstance.SelectedCitymapFilters[i] == "common_gym")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_gyn";
			}
			else if (gameInstance.SelectedCitymapFilters[i] == "common_gasstation")
			{
				gameInstance.SelectedCitymapFilters[i] = "businesstype_gasstation";
			}
		}
	}
}
