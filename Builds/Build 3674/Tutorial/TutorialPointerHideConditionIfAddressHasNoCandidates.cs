using System.Linq;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/AddressHasNoCandidates")]
public class TutorialPointerHideConditionIfAddressHasNoCandidates : TutorialPointerHideCondition
{
	[SerializeField]
	private CustomBuildingTarget addressTarget;

	protected override bool ConditionMetInternal()
	{
		return SaveGameManager.Current.CandidateEmployeeInstances.All((EmployeeInstance x) => x.assignedAddress != addressTarget.GetAddress());
	}
}
