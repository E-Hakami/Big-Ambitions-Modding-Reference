using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class FixCorruptedBuildings : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address("ba:street_eighthstreet", 12);
		Address address2 = new Address("ba:street_thirdavenue", 13);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (!(buildingRegistration is Warehouse))
		{
			Warehouse warehouse = new Warehouse
			{
				StreetName = buildingRegistration.StreetName,
				StreetNumber = buildingRegistration.StreetNumber,
				AvailableForRent = buildingRegistration.AvailableForRent,
				RentedByPlayer = buildingRegistration.RentedByPlayer,
				RentPerDay = buildingRegistration.RentPerDay,
				BusinessName = buildingRegistration.BusinessName,
				BusinessDescription = buildingRegistration.BusinessDescription,
				businessTypeName = buildingRegistration.businessTypeName,
				Layout = buildingRegistration.Layout,
				lastDeposit = buildingRegistration.lastDeposit,
				scheduleDays = buildingRegistration.scheduleDays,
				signAppearanceSettings = buildingRegistration.signAppearanceSettings,
				interiorDesigns = buildingRegistration.interiorDesigns,
				logoSettings = buildingRegistration.logoSettings,
				takeoverOfferAcceptRate = buildingRegistration.takeoverOfferAcceptRate,
				cachedAvailableProducts = buildingRegistration.cachedAvailableProducts,
				creationDay = buildingRegistration.creationDay,
				lastDayOnSale = buildingRegistration.lastDayOnSale
			};
			SaveGameManager.Current.BuildingRegistrations.Remove(buildingRegistration);
			SaveGameManager.Current.BuildingRegistrations.Add(warehouse);
			if (warehouse.RentedByPlayer)
			{
				BuildingHelper.SellBuilding(address, $"{address} was sold (caused by compatibility support)");
			}
		}
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(address2);
		if (buildingRegistration2.RentedByPlayer && (buildingRegistration2.scheduleDays == null || buildingRegistration2.scheduleDays.Count == 0))
		{
			BuildingRegistration buildingRegistration3 = new BuildingRegistration
			{
				StreetName = buildingRegistration2.StreetName,
				StreetNumber = buildingRegistration2.StreetNumber,
				AvailableForRent = buildingRegistration2.AvailableForRent,
				RentedByPlayer = buildingRegistration2.RentedByPlayer,
				RentPerDay = buildingRegistration2.RentPerDay,
				BusinessName = buildingRegistration2.BusinessName,
				BusinessDescription = buildingRegistration2.BusinessDescription,
				businessTypeName = buildingRegistration2.businessTypeName,
				Layout = buildingRegistration2.Layout,
				lastDeposit = buildingRegistration2.lastDeposit,
				scheduleDays = buildingRegistration2.scheduleDays,
				signAppearanceSettings = buildingRegistration2.signAppearanceSettings,
				interiorDesigns = buildingRegistration2.interiorDesigns,
				logoSettings = buildingRegistration2.logoSettings,
				takeoverOfferAcceptRate = buildingRegistration2.takeoverOfferAcceptRate,
				cachedAvailableProducts = buildingRegistration2.cachedAvailableProducts,
				creationDay = buildingRegistration2.creationDay,
				lastDayOnSale = buildingRegistration2.lastDayOnSale
			};
			SaveGameManager.Current.BuildingRegistrations.Remove(buildingRegistration2);
			SaveGameManager.Current.BuildingRegistrations.Add(buildingRegistration3);
			if (buildingRegistration3.RentedByPlayer)
			{
				BuildingHelper.SellBuilding(address2, $"{address2} was sold (caused by compatibility support)");
			}
		}
		Address address3 = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
		if (address == address3 || address2 == address3)
		{
			SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
		}
	}
}
