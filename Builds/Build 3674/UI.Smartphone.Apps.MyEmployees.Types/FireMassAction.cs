using System.Linq;
using Entities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "FireMassAction", menuName = "BigAmbitions/Employee Mass Actions/Fire")]
public class FireMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = "myemployees_mass_action_fire_confirm"
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			foreach (EmployeeInstance item in MyEmployeesMassActionsUI.massActionSelectedEmployees.ToList())
			{
				item.RemoveEmployee();
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.employeeScrollerController.RemoveEmployee(item);
			}
			GameEvent.Invoke(string.Empty);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: true);
		});
	}
}
