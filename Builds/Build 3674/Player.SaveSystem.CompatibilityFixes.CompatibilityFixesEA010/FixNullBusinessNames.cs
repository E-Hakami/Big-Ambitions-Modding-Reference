using Streets;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class FixNullBusinessNames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !(buildingRegistration.businessTypeName == "ba:businesstype_empty") && string.IsNullOrEmpty(buildingRegistration.BusinessName))
			{
				buildingRegistration.BusinessName = buildingRegistration.Address.ToFormattedString();
			}
		}
	}
}
