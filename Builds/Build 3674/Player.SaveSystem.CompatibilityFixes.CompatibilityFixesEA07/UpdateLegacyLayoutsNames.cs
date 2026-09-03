namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class UpdateLegacyLayoutsNames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached)
			{
				string text = buildingRegistration.BuildingCached.SpecialService?.layout;
				if (!string.IsNullOrEmpty(text))
				{
					buildingRegistration.Layout = text;
				}
			}
		}
	}
}
