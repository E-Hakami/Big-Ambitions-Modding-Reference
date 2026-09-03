using Buildings;
using HGAttributes;
using NaughtyAttributes;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/SelectedBizManBuildingHasNoCharacteristics")]
public class TutorialPointerHideConditionSelectedBizManBuildingHasNoCharacteristics : TutorialPointerHideCondition
{
	[SerializeField]
	private bool specificType;

	[SerializeField]
	[ShowIf("specificType")]
	[AutocompleteDropdown("BuildingTypes")]
	private string buildingType;

	[SerializeField]
	private bool specificSize;

	[SerializeField]
	[ShowIf("specificSize")]
	private string buildingSize;

	[SerializeField]
	private bool specificVersion;

	[SerializeField]
	[ShowIf("specificVersion")]
	private int buildingVersion;

	protected override bool ConditionMetInternal()
	{
		Building building = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.building;
		if (building == null)
		{
			return true;
		}
		if (specificType && building.BuildingType != buildingType)
		{
			return true;
		}
		if (specificSize && building.BuildingSize != buildingSize)
		{
			return true;
		}
		if (specificVersion && building.BuildingVersion != buildingVersion)
		{
			return true;
		}
		return false;
	}
}
