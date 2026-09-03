using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedHeadhunterToHeadhunterPlan")]
public class TutorialPointerHideConditionHasAssignedHeadhunterToHeadhunterPlan : TutorialPointerHideCondition
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
			if (headhunterPlan.headquartersAddress == address && !string.IsNullOrEmpty(headhunterPlan.assignedEmployeeId))
			{
				return true;
			}
		}
		return false;
	}
}
