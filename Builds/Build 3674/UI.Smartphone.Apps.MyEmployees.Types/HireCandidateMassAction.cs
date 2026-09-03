using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "HireCandidateMassAction", menuName = "BigAmbitions/Employee Mass Actions/Hire Candidate")]
public class HireCandidateMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = "myemployees_mass_action_hirecandidate"
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			foreach (EmployeeInstance massActionSelectedEmployee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
			{
				EmployeeHelper.HireCandidate(massActionSelectedEmployee);
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.candidateScrollerController.RemoveCandidate(massActionSelectedEmployee);
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: true);
		});
	}
}
