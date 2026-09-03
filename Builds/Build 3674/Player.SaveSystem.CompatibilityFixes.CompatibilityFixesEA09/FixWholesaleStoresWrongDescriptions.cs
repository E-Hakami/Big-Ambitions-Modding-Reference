using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class FixWholesaleStoresWrongDescriptions : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_twentyfifthstreet" && x.StreetNumber == 4);
		if (buildingRegistration != null && BuildingHelper.SpecialServiceBuildings.TryGetValue(buildingRegistration.Address, out var value) && value.SpecialService != null)
		{
			buildingRegistration.BusinessDescription = value.SpecialService.businessDescription;
		}
		BuildingRegistration buildingRegistration2 = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_sixthstreet" && x.StreetNumber == 1);
		if (buildingRegistration2 != null && BuildingHelper.SpecialServiceBuildings.TryGetValue(buildingRegistration2.Address, out var value2) && value2.SpecialService != null)
		{
			buildingRegistration2.BusinessDescription = value2.SpecialService.businessDescription;
		}
	}
}
