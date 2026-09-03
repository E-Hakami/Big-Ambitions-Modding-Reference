using System.Linq;
using Blueprints;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixFactorySupplyDepotNotInitialized : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == "ba:street_fifthavenue" && x.StreetNumber == 57);
		if (buildingRegistration != null && BuildingHelper.SpecialServiceBuildings.TryGetValue(buildingRegistration.Address, out var value) && !(value.SpecialService == null) && !(buildingRegistration.businessTypeName == value.SpecialService.businessTypeName))
		{
			buildingRegistration.BusinessName = value.SpecialService.businessName;
			buildingRegistration.BusinessDescription = value.SpecialService.businessDescription;
			buildingRegistration.businessTypeName = value.SpecialService.businessTypeName;
			buildingRegistration.Layout = value.SpecialService.layout;
			buildingRegistration.scheduleDays = value.SpecialService.scheduleDays;
			buildingRegistration.signAppearanceSettings = value.SpecialService.signAppearanceSettings;
			buildingRegistration.logoSettings = value.SpecialService.logoSettings;
			buildingRegistration.customerCapacity = CompetitionHelper.GetAiBusinessCustomerCapacity(new BuildingSizeInfo(value), value.BuildingType);
			buildingRegistration.RentedByPlayer = false;
			buildingRegistration.AvailableForRent = false;
		}
	}
}
