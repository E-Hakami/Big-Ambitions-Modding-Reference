using System.Collections.Generic;
using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasEmployeeAssignedToHrManagerPlan")]
public class TutorialPointerHideConditionHasEmployeeAssignedToHrManagerPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance == null || InstanceBehavior<UIs>.Instance.fullMenu == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business == null)
		{
			return false;
		}
		Address address = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
		List<HrManagerPlan> hrManagerPlans = SaveGameManager.Current.hrManagerPlans;
		for (int i = 0; i < hrManagerPlans.Count; i++)
		{
			HrManagerPlan hrManagerPlan = hrManagerPlans[i];
			if (hrManagerPlan.headquartersAddress == address && hrManagerPlan.assignedEmployees.Count > 0)
			{
				return true;
			}
		}
		return false;
	}
}
