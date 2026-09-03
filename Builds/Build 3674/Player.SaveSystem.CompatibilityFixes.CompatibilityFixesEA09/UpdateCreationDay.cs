namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateCreationDay : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName == "ba:businesstype_empty" && buildingRegistration.creationDay == 0)
			{
				buildingRegistration.creationDay = -1;
			}
		}
	}
}
