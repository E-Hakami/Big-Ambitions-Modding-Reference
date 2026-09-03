using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/IsWorkingAtStation")]
public class IsWorkingAtStation : QuestRequirement
{
	public bool onPlayerBusiness;

	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	public CustomBuildingTarget customBuildingTarget;

	public override bool CheckIfCompleted()
	{
		if (customBuildingTarget?.GetBuildingRegistration()?.businessTypeName == "ba:businesstype_gym")
		{
			return true;
		}
		if (!BuildingManager.IsInsideBuilding || !JobHelper.IsPlayerWorking())
		{
			return false;
		}
		bool num = businessTypeName == "ba:businesstype_empty" || InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == businessTypeName;
		bool flag = InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness == onPlayerBusiness;
		return num & flag;
	}
}
