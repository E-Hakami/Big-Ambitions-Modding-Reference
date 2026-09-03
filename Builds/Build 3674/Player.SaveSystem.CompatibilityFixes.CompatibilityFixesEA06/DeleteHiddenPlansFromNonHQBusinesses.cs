using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class DeleteHiddenPlansFromNonHQBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && (buildingRegistration.RentedByPlayer || buildingRegistration.BuildingOwnedByPlayer) && buildingRegistration.businessTypeName != "ba:businesstype_headquarters")
			{
				BusinessHelper.DeleteHQPlans(buildingRegistration);
			}
		}
	}
}
