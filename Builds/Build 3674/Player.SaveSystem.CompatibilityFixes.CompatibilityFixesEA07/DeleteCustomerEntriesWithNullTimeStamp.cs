namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class DeleteCustomerEntriesWithNullTimeStamp : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				buildingRegistration.unprocessedCompletedOrders.RemoveAll((Order x) => x.timestamp == null);
			}
		}
	}
}
