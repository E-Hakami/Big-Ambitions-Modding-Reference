namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemovePaperBagsFromPlayerBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				buildingRegistration.cachedAvailableProducts.RemoveAll((string x) => x == "ba:itemname_paperbag");
			}
		}
	}
}
