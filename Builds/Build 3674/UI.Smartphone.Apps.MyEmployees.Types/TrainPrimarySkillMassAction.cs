using System.Collections.Generic;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees.Types;

[CreateAssetMenu(fileName = "TrainPrimarySkillMassAction", menuName = "BigAmbitions/Employee Mass Actions/Train Primary Skill")]
public class TrainPrimarySkillMassAction : EmployeeMassAction
{
	public override void Perform()
	{
		LanguageChangeEventDataHolder bodyData = "myemployees_mass_action_confirm".Localize(new
		{
			employeesAffected = MyEmployeesMassActionsUI.massActionSelectedEmployees.Count,
			action = "myemployees_mass_action_train_primary_skill_confirm"
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			foreach (EmployeeInstance massActionSelectedEmployee in MyEmployeesMassActionsUI.massActionSelectedEmployees)
			{
				if (massActionSelectedEmployee.CanTrainSkill(massActionSelectedEmployee.characterData.skills[0]))
				{
					string text = massActionSelectedEmployee.characterData.skills[0].name;
					int skillIncrease = Mathf.Min(Mathf.CeilToInt(100f - massActionSelectedEmployee.characterData.skills[0].value), 10);
					float trainingCost = EmployeeHelper.GetTrainingCost(massActionSelectedEmployee, text, skillIncrease);
					Dictionary<string, string> data = new Dictionary<string, string>
					{
						{
							"employee",
							massActionSelectedEmployee.characterData.name
						},
						{ "skillName", text }
					};
					TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_employeetraining", data);
					transactionInfo.SetTaxDeductibleName("ba:transaction_employeetraining_label");
					if (GameManager.ChangeMoneySafe(0f - trainingCost, transactionInfo))
					{
						EmployeeHelper.UnassignEmployeeFromAllWorkshifts(massActionSelectedEmployee);
						massActionSelectedEmployee.trainingSession = new EmployeeInstance.TrainingInstance
						{
							skill = text,
							startDay = SaveGameManager.Current.Day
						};
					}
					InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.employeeScrollerController.RefreshEmployeeModel(massActionSelectedEmployee);
				}
			}
			GameEvent.Invoke(string.Empty);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.OnMassActionPerformed(needsReloadingData: false, needsReorderingData: true);
		});
	}
}
