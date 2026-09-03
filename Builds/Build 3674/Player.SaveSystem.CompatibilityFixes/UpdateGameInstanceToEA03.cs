using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA03 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[21]
	{
		(new FixOpenFastFoodQuestAppearingOnAdvancedSaveGames(), 1852),
		(new RemoveDeclinedHealthInsurancePlanOffers(), 1852),
		(new FixVehiclesFalling(), 1852),
		(new UpdateOfficesCachedProductsAndRetailPrices(), 1860),
		(new FixNoBuildingsForRentDuringSecondRetailQuest(), 1860),
		(new RemoveHealthInsuranceDemandFromHRManagers(), 1860),
		(new SellOldSofaAndDesktopWorkstationFurniture(), 1861),
		(new UpdatePricePerUnitOnPurchaseTimePrices(), 1861),
		(new EnsureAllFullTimeEmployeesHaveFullTimeDemand(), 1864),
		(new SetDeliveryContractsFee(), 1866),
		(new InitializePlayerSkills(), 1867),
		(new FixHeadquartersAreTemporarilyClosed(), 1869),
		(new FixAIBusinessesSharingOpeningHourSlots(), 1946),
		(new MoveSomeSpecialBuildingsToFourthAvenue(), 1983),
		(new InitializeInteriorInstallationFirmContracts(), 1984),
		(new ResetVehicleInstanceDeformations(), 1989),
		(new UpdateCandidatesToNewSystem(), 2002),
		(new EnablePartAndFullTimeToRecruitmentCampaigns(), 2003),
		(new InitializeHeadhunters(), 2009),
		(new FixPlayerBusinessSchedulesNotHavingOpeningHourSlots(), 2027),
		(new UpdateItemPrices(), 2033)
	};
}
