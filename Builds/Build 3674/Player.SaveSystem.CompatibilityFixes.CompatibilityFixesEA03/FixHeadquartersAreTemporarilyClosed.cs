namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixHeadquartersAreTemporarilyClosed : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				buildingRegistration.temporarilyClosed = false;
			}
		}
	}
}
