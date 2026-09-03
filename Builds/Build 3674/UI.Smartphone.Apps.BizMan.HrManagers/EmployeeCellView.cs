using BaTable;
using Entities;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.HrManagers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public sealed class EmployeeCellView : BaTableCellView<HrManagerEmployeeModel>
{
	[SerializeField]
	private TMP_Text employeeName;

	[SerializeField]
	private TextLocalizationComponent primarySkill;

	[SerializeField]
	private TMP_Text businessName;

	[SerializeField]
	private TextLocalizationComponent salary;

	[SerializeField]
	private TMP_Text insuranceDemand;

	[SerializeField]
	private ProgressBar satisfaction;

	[SerializeField]
	private Toggle assignToggle;

	public override void SetData(HrManagerEmployeeModel data)
	{
		employeeName.text = data.employeeName;
		if ((bool)businessName)
		{
			businessName.text = data.businessName;
		}
		if ((bool)primarySkill)
		{
			primarySkill.Arguments = new
			{
				skill = data.primarySkillName,
				value = Mathf.RoundToInt(data.primarySkillPercentage)
			};
		}
		if ((bool)salary)
		{
			salary.Arguments = new
			{
				wage = data.hourlyWage.ToCurrencyFormat()
			};
		}
		if ((bool)satisfaction)
		{
			satisfaction.SetValue(data.satisfaction);
		}
		if ((bool)insuranceDemand)
		{
			insuranceDemand.text = data.insuranceDemandText;
		}
		if ((bool)assignToggle)
		{
			assignToggle.onValueChanged.RemoveAllListeners();
			assignToggle.onValueChanged.AddListener(delegate(bool assigned)
			{
				if (assigned && !InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.hrHrManagerPlanUI.CanAssignEmployee())
				{
					Notifications.Show(NotificationType.Error, "bizman_hrmanager_max_employees_reached", null, 4f, "bizman_hrmanager_max_employees_reached");
					assignToggle.SetIsOnWithoutNotify(value: false);
				}
				else
				{
					InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.hrHrManagerPlanUI.SetEmployeeAssigned(data.employeeId, assigned);
					data.assigned = assigned;
				}
			});
			assignToggle.SetIsOnWithoutNotify(data.assigned);
		}
		Button component = base.transform.GetComponent<Button>();
		if (!component)
		{
			return;
		}
		component.onClick.RemoveAllListeners();
		component.onClick.AddListener(delegate
		{
			EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(data.employeeId);
			if (!employeeById.isBeingReplaced)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(employeeById);
			}
		});
	}
}
