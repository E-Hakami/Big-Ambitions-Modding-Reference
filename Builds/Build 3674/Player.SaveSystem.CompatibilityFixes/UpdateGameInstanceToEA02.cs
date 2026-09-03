using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA02 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[13]
	{
		(new ConvertDonutsIndustrialFryersToBakeryShowcases(), 1805),
		(new Change28FourthAvenueBuildingSettings(), 1806),
		(new UpdateItemInstanceYRotation(), 1807),
		(new Change6SixthAvenueBuildingSettings(), 1808),
		(new UpdateQuests1213And17(), 1808),
		(new InitializeHealthInsurancePlanOffers(), 1813),
		(new UpdateCharacterDataToNewSystem(), 1824),
		(new ReloadCustomerDemandsFulfilledCache(), 1833),
		(new CompleteQuestImproveCleaningAnd13IfNeeded(), 1840),
		(new SetUpVehicleDeformationRandomness(), 1843),
		(new CompleteDeliveryContractQuestIfNeeded(), 1844),
		(new CleanBrokenTasksFromOldBusinesses(), 1844),
		(new FixCorruptedBuildings(), 1844)
	};
}
