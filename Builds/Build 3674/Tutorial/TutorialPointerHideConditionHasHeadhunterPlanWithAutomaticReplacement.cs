using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasHeadhunterPlanWithAutomaticReplacement")]
public class TutorialPointerHideConditionHasHeadhunterPlanWithAutomaticReplacement : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance == null || InstanceBehavior<UIs>.Instance.fullMenu == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business == null)
		{
			return false;
		}
		Address address = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
		foreach (HeadhunterPlan headhunterPlan in SaveGameManager.Current.headhunterPlans)
		{
			if (!(headhunterPlan.headquartersAddress != address) && (headhunterPlan.automaticallyReplaceOnResign || headhunterPlan.automaticallyReplaceOnRetire))
			{
				return true;
			}
		}
		return false;
	}
}
