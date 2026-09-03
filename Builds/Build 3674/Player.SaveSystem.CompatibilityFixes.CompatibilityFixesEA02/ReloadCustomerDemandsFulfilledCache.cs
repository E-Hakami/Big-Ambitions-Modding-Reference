using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class ReloadCustomerDemandsFulfilledCache : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && (bool)buildingRegistration.BuildingCached)
			{
				CustomerDemandHelper.ReloadCachedFulfilled(buildingRegistration);
			}
		}
	}
}
