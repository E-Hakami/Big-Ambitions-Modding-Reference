using Buildings.Office.Headquarters;
using Streets;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedLogisticsManagerDestination")]
public class TutorialPointerHideConditionHasAssignedLogisticsManagerDestination : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		LogisticsManagerPlan firstLogisticsManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstLogisticsManagerPlan();
		if (firstLogisticsManagerPlan != null && firstLogisticsManagerPlan.destinations.Count > 0)
		{
			return !firstLogisticsManagerPlan.destinations[0].deliveryTargetAddress.IsUndefined();
		}
		return false;
	}
}
