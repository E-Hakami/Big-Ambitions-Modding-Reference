using Buildings.Office.Headquarters;
using Streets;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedWarehouse")]
public class TutorialPointerHideConditionHasAssignedWarehouse : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		LogisticsManagerPlan firstLogisticsManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstLogisticsManagerPlan();
		if (firstLogisticsManagerPlan != null)
		{
			return !firstLogisticsManagerPlan.targetAddress.IsUndefined();
		}
		return false;
	}
}
