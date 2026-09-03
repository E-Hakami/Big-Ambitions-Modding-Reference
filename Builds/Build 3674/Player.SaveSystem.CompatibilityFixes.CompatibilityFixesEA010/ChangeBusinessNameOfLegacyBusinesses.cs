using BigAmbitions.Rivals;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class ChangeBusinessNameOfLegacyBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		AiBusinessDefault[] allBusinessDefaults = CompetitionHelper.GetAllBusinessDefaults();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer || (bool)CompetitionHelper.GetBusinessDefault(buildingRegistration.BusinessName))
			{
				continue;
			}
			string businessOwnerRivalId = buildingRegistration.businessOwnerRivalId;
			bool flag = businessOwnerRivalId.IsSpecialRival();
			AiBusinessDefault[] array = allBusinessDefaults;
			foreach (AiBusinessDefault aiBusinessDefault in array)
			{
				if (aiBusinessDefault.businessTypeName != buildingRegistration.businessTypeName)
				{
					continue;
				}
				if (flag)
				{
					if (!(aiBusinessDefault.corporationRivalId != businessOwnerRivalId))
					{
						buildingRegistration.BusinessName = aiBusinessDefault.businessName;
						break;
					}
				}
				else if (!(buildingRegistration.Layout != aiBusinessDefault.buildingLayout))
				{
					buildingRegistration.BusinessName = aiBusinessDefault.businessName;
					break;
				}
			}
		}
	}
}
