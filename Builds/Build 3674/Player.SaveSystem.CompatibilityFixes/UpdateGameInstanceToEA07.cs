using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA07 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[19]
	{
		(new SetPlayerMonopolyInDemands(), 2804),
		(new UpdateMarketDemands(), 2811),
		(new UpdateLegacyLayoutsNames(), 2820),
		(new ResetScheduleDaysOnAddressesWhereMovingServiceFailed(), 2821),
		(new UpdateTodoTasks(), 2822),
		(new SetUpGameInstanceSeed(), 2823),
		(new InitializeNextRainStartTime(), 2825),
		(new FixDrivingRemovedForklift(), 2831),
		(new DeleteCustomerEntriesWithNullTimeStamp(), 2841),
		(new InitializeOldGymAsARegularCompetitor(), 2842),
		(new FixNextRecruitDayFromHeadhunterPlans(), 2842),
		(new FixFactorySupplyDepotNotInitialized(), 2846),
		(new RescueVehiclesInOldIkaParking(), 2846),
		(new FixNullEmployeeInWorkShifts(), 2848),
		(new FixInteriorInstallationFirmsFromEA03NotInitializedCorrectly(), 2850),
		(new FixIrsNotCorrectlyInitialized(), 2850),
		(new RemoveUnneededPaperBags(), 2850),
		(new FixGasStationsAreCasinos(), 2850),
		(new FixManhattanMoversNotInitialized(), 2850)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[1] { (new AddBuildingOwnerIdToBuildingsWhereItsMissing(), 2815) };
}
