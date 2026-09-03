using Streets;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IfSelectedEmployeeHasBusinessAssigned")]
public class TutorialPointerHideConditionIfSelectedEmployeeHasBusinessAssignedStatus : TutorialPointerHideCondition
{
	[SerializeField]
	private bool businessAssigned;

	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance != null)
		{
			return !InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance.assignedAddress.IsUndefined() == businessAssigned;
		}
		return false;
	}
}
