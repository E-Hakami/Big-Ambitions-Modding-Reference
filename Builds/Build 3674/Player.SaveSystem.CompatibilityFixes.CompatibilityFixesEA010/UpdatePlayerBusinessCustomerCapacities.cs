using AI.Citizens;
using BigAmbitions.InteriorDesigner.InteriorElements;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdatePlayerBusinessCustomerCapacities : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		InteriorElementsHelper.Init();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && (!(buildingRegistration.businessTypeName != "ba:businesstype_cinema") || !(buildingRegistration.businessTypeName != "ba:businesstype_theater")))
			{
				BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
			}
		}
	}
}
