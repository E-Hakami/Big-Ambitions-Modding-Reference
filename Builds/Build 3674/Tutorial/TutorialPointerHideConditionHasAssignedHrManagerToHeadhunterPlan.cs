using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedHrManagerToHeadhunterPlan")]
public class TutorialPointerHideConditionHasAssignedHrManagerToHeadhunterPlan : TutorialPointerHideCondition
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
			if (headhunterPlan.headquartersAddress != address || headhunterPlan.assignedHrPlans == null)
			{
				continue;
			}
			for (int i = 0; i < headhunterPlan.assignedHrPlans.Length; i++)
			{
				if (!string.IsNullOrEmpty(headhunterPlan.assignedHrPlans[i]))
				{
					return true;
				}
			}
		}
		return false;
	}
}
