using System;
using System.Linq;
using Entities;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "PayBonusesMassAction", menuName = "BigAmbitions/Employee Mass Actions/Pay Bonuses")]
public class PayBonusesMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		float val = MyEmployeesMassActionsUI.massActionSelectedEmployees.Sum((EmployeeInstance employee) => employee.GetBonusAmount());
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = "myemployees_mass_action_pay_bonuses_confirm".Localize(new
			{
				bonusesSum = val.ToShortCurrencyFormat()
			})
		});
		Action onConfirmAction = GiveBonuses;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	private void GiveBonuses()
	{
		foreach (EmployeeInstance employee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
		{
			if (employee.CanGiveBonus())
			{
				employee.GiveBonus();
				EmployeeModel employeeModel = InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.employeeScrollerController.data.FirstOrDefault((EmployeeModel x) => x.employeeInstance == employee);
				if (employeeModel != null)
				{
					employeeModel.satisfaction = employee.satisfaction;
				}
			}
		}
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: false, needsReorderingData: true);
	}
}
