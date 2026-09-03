using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasAssignedHrManagerToHrManagerPlan")]
public class TutorialPointerHideConditionHasAssignedHrManagerToHrManagerPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance?.fullMenu?.bizMan?.business == null)
		{
			return false;
		}
		Address address = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
		foreach (HrManagerPlan hrManagerPlan in SaveGameManager.Current.hrManagerPlans)
		{
			if (hrManagerPlan.headquartersAddress == address && !string.IsNullOrEmpty(hrManagerPlan.assignedEmployeeId))
			{
				return true;
			}
		}
		return false;
	}
}
