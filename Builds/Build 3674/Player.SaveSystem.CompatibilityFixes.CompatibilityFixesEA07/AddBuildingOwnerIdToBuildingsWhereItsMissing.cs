using BigAmbitions.Rivals;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class AddBuildingOwnerIdToBuildingsWhereItsMissing : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (string.IsNullOrEmpty(buildingRegistration.buildingOwnerRivalId) && (bool)buildingRegistration.BuildingCached && !buildingRegistration.IsOnSale() && buildingRegistration.BuildingCached.SpecialService == null)
			{
				buildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding(buildingRegistration.Neighborhood);
			}
		}
	}
}
