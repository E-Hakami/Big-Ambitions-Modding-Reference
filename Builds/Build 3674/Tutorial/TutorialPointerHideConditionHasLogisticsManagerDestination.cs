using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasLogisticsManagerDestination")]
public class TutorialPointerHideConditionHasLogisticsManagerDestination : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		LogisticsManagerPlan firstLogisticsManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstLogisticsManagerPlan();
		if (firstLogisticsManagerPlan != null)
		{
			return firstLogisticsManagerPlan.destinations.Count > 0;
		}
		return false;
	}
}
