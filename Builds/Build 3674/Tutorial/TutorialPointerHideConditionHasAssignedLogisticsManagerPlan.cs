using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedLogisticsManagerPlan")]
public class TutorialPointerHideConditionHasAssignedLogisticsManagerPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		LogisticsManagerPlan firstLogisticsManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstLogisticsManagerPlan();
		if (firstLogisticsManagerPlan != null)
		{
			return !string.IsNullOrEmpty(firstLogisticsManagerPlan.assignedEmployeeId);
		}
		return false;
	}
}
