using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasHeadhunterPlan")]
public class TutorialPointerHideConditionHasHeadhunterPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance?.fullMenu?.bizMan?.business == null)
		{
			return false;
		}
		Address address = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
		foreach (HeadhunterPlan headhunterPlan in SaveGameManager.Current.headhunterPlans)
		{
			if (headhunterPlan.headquartersAddress == address)
			{
				return true;
			}
		}
		return false;
	}
}
