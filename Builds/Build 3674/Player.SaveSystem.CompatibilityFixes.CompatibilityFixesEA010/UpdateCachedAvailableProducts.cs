using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateCachedAvailableProducts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			buildingRegistration.UpdateCachedAvailableProducts();
		}
	}
}
