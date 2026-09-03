using AI.Citizens;
using BigAmbitions.InteriorDesigner.InteriorElements;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdatePlayerBusinessPromotion : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		InteriorElementsHelper.Init();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				BusinessHelper.UpdatePromotion(buildingRegistration);
			}
		}
	}
}
