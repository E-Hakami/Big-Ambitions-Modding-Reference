using System.Collections.Generic;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateGameInstanceToEA011 : ICompatibilityVersion
{
	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesBeforeLoading => new(ICompatibilityFix, int)[11]
	{
		(new UpdateRenamedBusinessNames(), 3506),
		(new MigrateSignWorldSpaceTextToLinkedItemName(), 3515),
		(new RescueMissingVehiclesWithWrongAddress(), 3518),
		(new FixMissingDeliverySpot(), 3524),
		(new FixContactsDescriptionAndMergeDuplicates(), 3525),
		(new RemoveDuplicateEmployees(), 3532),
		(new FixHappinessModifierFirstJob(), 3536),
		(new FixNullBusinessNames(), 3536),
		(new InitializePricingManagerPlans(), 3628),
		(new InitializeFoodDeliveryContracts(), 3630),
		(new NormalizeHeadhunterPlans(), 3630)
	};

	public IEnumerable<(ICompatibilityFix fix, int buildNumber)> CompatibilityFixesAfterLoading { get; } = new(ICompatibilityFix, int)[3]
	{
		(new UpdatePlayerBusinessCustomers(), 3515),
		(new FixWarehouseSlotMismatch(), 3524),
		(new AssignParkedVehiclesToWarehouseSlots(), 3640)
	};
}
