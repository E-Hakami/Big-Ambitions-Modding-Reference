using BigAmbitions.SaveSystem;
using UnityEngine;

namespace Tutorial;

public class HelpSlugOverriderSecondStoreBusinessType : HelpSlugOverrider
{
	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	public override string GetTargetHelpSlug()
	{
		BuildingRegistration buildingRegistration = playerStoreTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		return "businesstypes-" + buildingRegistration.businessTypeName.GetIdWithoutType().ToLower();
	}
}
