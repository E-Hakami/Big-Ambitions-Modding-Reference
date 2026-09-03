using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA08 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[18]
	{
		(new UpdateItemInstancesToNewSystem(), 2915),
		(new ResetScheduleOnBusinessesBrokenByMovingService(), 2915),
		(new UpdateEmployeeCachedInfo(), 2916),
		(new UpdateBathroomTiles(), 2916),
		(new UpdateDeliveryDriversHoursAndDays(), 2918),
		(new InitializeAccessoriesUI(), 2926),
		(new UpdateSomeItemsAttachmentsToNewAttachments(), 2932),
		(new UpdateApronsAndBulletproofVestsWithNewIds(), 2935),
		(new TransferUniformsToNewSystem(), 2936),
		(new UpdateCharactersSkinColors(), 2939),
		(new RestoreElGatoFoodMarket(), 2952),
		(new AddBeardsToMalePlayers(), 2966),
		(new ShutdownBusinessesFromRivalsAlreadyDefeated(), 2972),
		(new FixPricePerUnitNan(), 2986),
		(new UpdateEmployeeAttachedItems(), 2987),
		(new FixAttachedItemsBrokenByMovingService(), 2987),
		(new FixBrokenSchedule(), 2994),
		(new FixWorkShiftsWithSoldItems(), 3009)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading => new(ICompatibilityFix, int)[7]
	{
		(new FixWallCornerIds(), 2934),
		(new FixHandTruckDeliverySpotPositionOnL1(), 3009),
		(new PreselectPlayerOwnedMapFilter(), 2910),
		(new UpdateCachedAvailableProductsForAiBusinesses(), 2915),
		(new RemoveCorruptedWorkShiftsAfterAnInstallationContract(), 2959),
		(new RegenerateBusinessLogoForWarehouses(), 2961),
		(new FixPriceReductionDefenseNotAffectingSomeBusinessesItems(), 2972)
	};
}
