using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA010 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[24]
	{
		(new InitializeSpecialBusinesses(), 3346),
		(new DefaultAiEmployeeWeeklyHoursDemand(), 3346),
		(new InitVehicleDeliveryContracts(), 3422),
		(new AddExportMultiplierGameVariable(), 3422),
		(new UpdateCachedAvailableProductsForAiCoffeeShopsAndUpdateMarketDemand(), 3422),
		(new ReplaceOldFactoryMachines(), 3425),
		(new UpdateExistingFactoryMachines(), 3422),
		(new AddMissingNeighborhoodStats(), 3422),
		(new UpdateEmployeeItemsAndHours(), 3430),
		(new RemovePositionZeroItemsInLayouts(), 3430),
		(new RemoveNonExistingDeliveryDrivers(), 3431),
		(new InitLastCategoryName(), 3432),
		(new FixNullBusinessNames(), 3432),
		(new RescueVehiclesUnderStockCo(), 3435),
		(new ChangeBusinessNameOfLegacyBusinesses(), 3435),
		(new FixNullLogoShapes(), 3447),
		(new RemoveLegacyFactoryItemsFromLogisticPlans(), 3447),
		(new FixDuplicatedVehicleSlots(), 3447),
		(new UpdatePlayerBusinessCustomerCapacities(), 3451),
		(new UpdatePlayerBusinessPromotion(), 3451),
		(new UpdateMarketDemands(), 3451),
		(new RemoveInvalidPlans(), 3452),
		(new RemoveContactsWithNullId(), 3462),
		(new RemoveFurnitureContractsFromOldSupplyFactoryDepot(), 3462)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[6]
	{
		(new AddFactoryExportsList(), 3424),
		(new UpdateLogisticsManagerPlans(), 3422),
		(new RunMovingServiceAndAutoScheduleInFactories(), 3422),
		(new UpdateCachedAvailableProducts(), 3430),
		(new UpdateCachedAvailableProductsForAiBusinessesAndUpdateMarketDemand(), 3431),
		(new FixRoofItemPositions(), 3458)
	};
}
