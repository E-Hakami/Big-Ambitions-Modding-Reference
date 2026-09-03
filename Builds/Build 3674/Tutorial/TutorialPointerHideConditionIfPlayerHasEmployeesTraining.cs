using System.Linq;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IfPlayerHasEmployeesTraining")]
public class TutorialPointerHideConditionIfPlayerHasEmployeesTraining : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return SaveGameManager.Current.EmployeeInstances.Any((EmployeeInstance x) => x.IsTraining);
	}
}
