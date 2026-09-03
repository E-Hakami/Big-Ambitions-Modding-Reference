using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA09 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[44]
	{
		(new RemoveInexistentBuildings(), 3219),
		(new UpdateBizManDeliveries(), 3102),
		(new UpdateImportContracts(), 3102),
		(new UpdateDiplomas(), 3102),
		(new UpdateMarketEvents(), 3102),
		(new InitLastDaySold(), 3102),
		(new AddStorageRoomAndBathroomToD2(), 3102),
		(new AddStorageRoomAndBathroomToJ1(), 3102),
		(new UpdateWholesaleStores(), 3124),
		(new MoveMetroWholesaleToHellsKitchen(), 3124),
		(new MoveGeneralUsTrucksToIndustryCity(), 3124),
		(new UpdateSixthAndSeventhStreetAddresses(), 3124),
		(new FixHealthInsuranceAddress(), 3102),
		(new UpdateCompletedQuestForNewTutorial(), 3100),
		(new AngledWoodenDisplayStandItemsPositioning(), 3118),
		(new UpdatePlayerSettings(), 3118),
		(new AddEyebrowsToPlayers(), 3131),
		(new AddRequiredBathroomItemsAndChangingRoomsToDeliverySpots(), 3131),
		(new DeleteHiddenPlansFromNonHQBusinesses(), 3136),
		(new UpdateSideQuestCompletions(), 3140),
		(new UpdateCreationDay(), 3151),
		(new FixWholesaleStoresWrongDescriptions(), 3164),
		(new RemovePaperBagsFromPlayerBusinesses(), 3164),
		(new UpdateWholesalersContactAddress(), 3164),
		(new SetSwimmingDefaultTime(), 3164),
		(new RemoveAFreshStartHappinessModifier(), 3179),
		(new UpdateRealStateRentValues(), 3181),
		(new CompleteSomeTutorialObjectivesIfNeeded(), 3189),
		(new SetUpBaseCustomerPromotionMultiplier(), 3199),
		(new Change48ThAvenueToI3(), 3199),
		(new UpdateRivalsToNewTiers(), 3206),
		(new FixIndustryCityHasCorruptedBuildings(), 3210),
		(new FixIndustryCityHasUnbalancedAiBusinesses(), 3211),
		(new FixDeliverySpotAt000(), 3216),
		(new FixWrongMidtownHospitalLayout(), 3216),
		(new RemoveCorruptedWholesaleContracts(), 3216),
		(new SetUpUrgentFeesMultiplier(), 3219),
		(new FixPricePerUnitNan(), 3219),
		(new SetNextForceShutdownInNeighborhoods(), 3219),
		(new UpdateEarlyTriggeredSideQuests(), 3219),
		(new RemoveInexistentBuildingsFromBuildingForSale(), 3221),
		(new RemoveCorruptedWorkShiftsAfterAnInstallationContract(), 3230),
		(new RescueVehiclesOnRoofTopsAndPark(), 3233),
		(new RemoveContactsWithNullId(), 3234)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[7]
	{
		(new RecalculateRetailPrices(), 3006),
		(new SetUpIndustryCityNeighborhood(), 3102),
		(new ReloadCustomerDemandsFulfilledCache(), 3119),
		(new FixHighAiRetailPrices(), 3206),
		(new UpdateMarketDemands(), 3210),
		(new RestoreDeliverySpotInPAndQWarehouses(), 3230),
		(new RemoveFlatbedSpawnersFromPlayerLayouts(), 3231)
	};
}
