using System.Linq;
using Blueprints;
using Buildings;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixInteriorInstallationFirmsFromEA03NotInitializedCorrectly : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address kristianBahoodAddress = new Address("ba:street_firstavenue", 2);
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => (bool)x.BuildingCached && x.Address == kristianBahoodAddress);
		if (buildingRegistration != null && buildingRegistration.businessTypeName != "ba:businesstype_interiorinstallationfirm")
		{
			buildingRegistration.Reset();
			Building building = BuildingHelper.GetBuilding(kristianBahoodAddress);
			buildingRegistration.StreetName = kristianBahoodAddress.streetName;
			buildingRegistration.StreetNumber = kristianBahoodAddress.streetNumber;
			if (BuildingHelper.SpecialServiceBuildings.ContainsKey(kristianBahoodAddress))
			{
				buildingRegistration.BusinessName = building.SpecialService.businessName;
				buildingRegistration.BusinessDescription = building.SpecialService.businessDescription;
				buildingRegistration.businessTypeName = building.SpecialService.businessTypeName;
				buildingRegistration.Layout = building.SpecialService.layout;
				buildingRegistration.scheduleDays = building.SpecialService.scheduleDays;
				buildingRegistration.signAppearanceSettings = building.SpecialService.signAppearanceSettings;
				buildingRegistration.logoSettings = building.SpecialService.logoSettings;
				buildingRegistration.customerCapacity = CompetitionHelper.GetAiBusinessCustomerCapacity(new BuildingSizeInfo(building), building.BuildingType);
				buildingRegistration.RentedByPlayer = false;
				buildingRegistration.AvailableForRent = false;
			}
		}
		Address nugemInteriorsAddress = new Address("ba:street_firstavenue", 11);
		BuildingRegistration buildingRegistration2 = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => (bool)x.BuildingCached && x.Address == nugemInteriorsAddress);
		if (buildingRegistration2 != null && buildingRegistration2.businessTypeName != "ba:businesstype_interiorinstallationfirm")
		{
			buildingRegistration2.Reset();
			Building building2 = BuildingHelper.GetBuilding(nugemInteriorsAddress);
			buildingRegistration2.StreetName = nugemInteriorsAddress.streetName;
			buildingRegistration2.StreetNumber = nugemInteriorsAddress.streetNumber;
			if (BuildingHelper.SpecialServiceBuildings.ContainsKey(nugemInteriorsAddress))
			{
				buildingRegistration2.BusinessName = building2.SpecialService.businessName;
				buildingRegistration2.BusinessDescription = building2.SpecialService.businessDescription;
				buildingRegistration2.businessTypeName = building2.SpecialService.businessTypeName;
				buildingRegistration2.Layout = building2.SpecialService.layout;
				buildingRegistration2.scheduleDays = building2.SpecialService.scheduleDays;
				buildingRegistration2.signAppearanceSettings = building2.SpecialService.signAppearanceSettings;
				buildingRegistration2.logoSettings = building2.SpecialService.logoSettings;
				buildingRegistration2.customerCapacity = CompetitionHelper.GetAiBusinessCustomerCapacity(new BuildingSizeInfo(building2), building2.BuildingType);
				buildingRegistration2.RentedByPlayer = false;
				buildingRegistration2.AvailableForRent = false;
			}
		}
	}
}
