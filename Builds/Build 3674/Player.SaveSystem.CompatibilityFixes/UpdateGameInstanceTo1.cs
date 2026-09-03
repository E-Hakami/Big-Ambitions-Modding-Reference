using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceTo1 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[15]
	{
		(new UpdateCachedAvailableProducts(), 3624),
		(new UpdateInvestmentFunds(), 3624),
		(new MigrateCurrentTaxPeriodDeductibleExpenses(), 3626),
		(new UpdateEvictedAddressOwnership(), 3632),
		(new AddFoodDeliveryContact(), 3633),
		(new UpdateSideQuestCompletions(), 3637),
		(new FixWrongAddresses(), 3637),
		(new FixEmployeeContactCategories(), 3652),
		(new MoveEmployeeMessagesToEmployeeContacts(), 3652),
		(new FixLegacyJobDemandMessageData(), 3652),
		(new MoveDeliveryReportsToLogisticsAlerts(), 3652),
		(new FixHamptonsHousesOnRent(), 3654),
		(new RemoveOrphanedPricingManagerPlans(), 3655),
		(new RemoveMissingItemParents(), 3655),
		(new CompleteResearchStoreObjectiveIfQuestAlreadyFinished(), 3672)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading { get; } = new(ICompatibilityFix, int)[3]
	{
		(new RecalculateLoanPayments(), 3624),
		(new SetUpTheHamptonsNeighborhood(), 3624),
		(new ForceUpdatePersonalGoals(), 3655)
	};
}
