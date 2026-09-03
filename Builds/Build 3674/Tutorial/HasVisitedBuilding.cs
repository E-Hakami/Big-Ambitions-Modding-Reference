using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/HasVisitedBuilding")]
public class HasVisitedBuilding : QuestRequirement
{
	public QuestEntryTarget target;

	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	public override bool CheckIfCompleted()
	{
		if (InstanceBehavior<BuildingManager>.Instance.building == null)
		{
			return false;
		}
		if (target == null)
		{
			return InstanceBehavior<BuildingManager>.Instance.building.BuildingType == buildingType;
		}
		return InstanceBehavior<BuildingManager>.Instance.building.Address == target.GetAddress();
	}
}
