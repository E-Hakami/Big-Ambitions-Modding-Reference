namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class FixHamptonsHousesOnRent : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.BuildingCached.IsHamptonsHouse())
			{
				buildingRegistration.AvailableForRent = false;
				if (buildingRegistration.BuildingCached.IsHamptonsAIVilla())
				{
					buildingRegistration.RentedByPlayer = false;
				}
			}
		}
	}
}
