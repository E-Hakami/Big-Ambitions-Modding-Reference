using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA06 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[13]
	{
		(new UpdateHeadquartersPlansToNewSystem(), 2566),
		(new FixWarehousesHavingWrongBusinessTypeName(), 2566),
		(new MovingServiceBuilding(), 2567),
		(new InitializeMovingServiceContracts(), 2567),
		(new InitializeRivalFactories(), 2567),
		(new FixApartmentsHallwaysIds(), 2580),
		(new AddStoredRetailPrices(), 2598),
		(new RemoveNonQuantityItemsFromLogisticsManagerPlans(), 2598),
		(new FixMissingVehicleSlotsInWarehouses(), 2616),
		(new FixNumberOfVehicleSlotsInWarehouses(), 2674),
		(new DeleteHiddenPlansFromNonHQBusinesses(), 2679),
		(new RemoveForkliftsFromSavedVehicles(), 2680),
		(new UnassignEmptyWarehousesFromLogisticPlans(), 2699)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[3]
	{
		(new RegenerateBusinessLogoForWarehouses(), 2578),
		(new FixNightclubFees(), 2642),
		(new FixNoDeliverySpotInFactories(), 2678)
	};
}
