using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixIrsNotCorrectlyInitialized : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_fifthavenue" && x.StreetNumber == 70);
		if (buildingRegistration != null)
		{
			buildingRegistration.businessTypeName = "ba:businesstype_irs";
		}
	}
}
