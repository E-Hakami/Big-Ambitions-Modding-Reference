using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA05 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[20]
	{
		(new UpdateContactsCategories(), 2358),
		(new SetEmployeeInitialSkillAmount(), 2376),
		(new RenameAIBusiness(), 2376),
		(new K1ElevatorMaterial(), 2387),
		(new SetEmployeeComplaints(), 2396),
		(new GenerateRivals(), 2433),
		(new UpdateMarketDemands(), 2406),
		(new RotateCashRegisters(), 2412),
		(new FixDeliveryContractQuestEntriesNotCompleted(), 2417),
		(new KickOutBuildingCompatibility(), 2419),
		(new FixNullEmployeeInWorkShifts(), 2421),
		(new RemoveActiveVehicleIfHasDifferentAddress(), 2426),
		(new UpdateCachedAvailableProductsForAiOffices(), 2430),
		(new FixFurnitureShopTutorialRolledBack(), 2448),
		(new FixTooLowSalariesInOldSaveGames(), 2452),
		(new FixDuplicatedRetailPrices(), 2454),
		(new RemovePoachedEmployeesFromAiEmployeesList(), 2454),
		(new UpdateMarketDemands(), 2461),
		(new FixRetailPrices(), 2461),
		(new InitializeSalaryNegotiations(), 2461)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[11]
	{
		(new SetUpLowerManhattanNeighborhood(), 2376),
		(new ConvertHandTruckSpawnersToItemInstances(), 2387),
		(new PopulateRivals(), 2433),
		(new AddEmployeesToAiBusinesses(), 2440),
		(new SetRivalProgressCompatibility(), 2443),
		(new AddDailyIncomesToAiBusinesses(), 2448),
		(new FixRivalsNotInitiated(), 2450),
		(new UpdateCachedAvailableProductsForBrokenAIBusinesses(), 2452),
		(new FixReappearingDefeatedRivals(), 2452),
		(new RegenerateWronglyGeneratedAIBusinessEmployees(), 2452),
		(new RecalculateRetailPrices(), 2455)
	};
}
