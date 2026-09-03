using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA04 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[17]
	{
		(new FixNoBuildingsForRentDuringFirstRetailQuest(), 2042),
		(new FixKristianBahoodWrongContact(), 2042),
		(new FixEmployeeUniformPresetsHaveHeadsAndHairs(), 2042),
		(new FixWrongInstallationFirms(), 2043),
		(new FixBusinessesTakenOverSharingSameSchedule(), 2050),
		(new UpdateBusinessTypeNames(), 2050),
		(new UpdateSecurityLevels(), 2053),
		(new UpdateMarketDemands(), 2183),
		(new UpdateKabobsLayout(), 2191),
		(new FixDoubleEmployeesSaved(), 2213),
		(new FixHRManagerNotHavingCorrectClass(), 2221),
		(new RemoveOldMops(), 2221),
		(new UpdateMapFilterNames(), 2225),
		(new FixMaterialIDNotFound(), 2225),
		(new RemoveCoatCheckDemand(), 2233),
		(new UpdateHappinessModifiers(), 2238),
		(new RescueMissingVehiclesWithWrongAddress(), 2240)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[2]
	{
		(new UpdateCachedAvailableProductsForNightclubs(), 2044),
		(new UpdateDirtSpotsToNewSystem(), 2190)
	};
}
