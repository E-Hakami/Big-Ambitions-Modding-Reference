using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "DiscardCandidateMassAction", menuName = "BigAmbitions/Employee Mass Actions/Discard Candidate")]
public class DiscardCandidateMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = "myemployees_mass_action_discardcandidate"
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			foreach (EmployeeInstance massActionSelectedEmployee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
			{
				EmployeeHelper.DiscardCandidate(massActionSelectedEmployee);
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.candidateScrollerController.RemoveCandidate(massActionSelectedEmployee);
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: true);
		});
	}
}
