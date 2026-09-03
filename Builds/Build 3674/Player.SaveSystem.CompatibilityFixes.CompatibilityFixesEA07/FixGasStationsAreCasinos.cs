using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixGasStationsAreCasinos : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_firstavenue" && x.StreetNumber == 7);
		if (buildingRegistration != null)
		{
			buildingRegistration.businessTypeName = "ba:businesstype_gasstation";
		}
		BuildingRegistration buildingRegistration2 = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_fifthavenue" && x.StreetNumber == 47);
		if (buildingRegistration2 != null)
		{
			buildingRegistration2.businessTypeName = "ba:businesstype_gasstation";
		}
	}
}
