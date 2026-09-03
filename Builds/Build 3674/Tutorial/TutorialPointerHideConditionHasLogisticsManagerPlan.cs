using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasLogisticsManagerPlan")]
public class TutorialPointerHideConditionHasLogisticsManagerPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return SaveGameManager.Current.logisticsManagerPlans.Count > 0;
	}
}
