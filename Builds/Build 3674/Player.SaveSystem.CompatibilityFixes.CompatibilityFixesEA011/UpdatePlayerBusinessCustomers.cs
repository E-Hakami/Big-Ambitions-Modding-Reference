using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class UpdatePlayerBusinessCustomers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
				buildingRegistration.UpdateCachedAvailableProducts();
				CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, dayOfWeek);
			}
		}
	}
}
