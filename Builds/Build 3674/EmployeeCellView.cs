using BaTable;
using BigAmbitions.InputSystem;
using Buildings.Office.Headquarters;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI;
using UI.Smartphone.Apps.MyEmployees;
using UnityEngine;
using UnityEngine.UI;

public sealed class EmployeeCellView : BaTableCellView<EmployeeModel>
{
	public Toggle massActionToggle;

	public TextMeshProUGUI employeeName;

	public TextMeshProUGUI age;

	public TextMeshProUGUI hourlyWage;

	public TextLocalizationComponent primarySkill;

	public TextLocalizationComponent task;

	public TextMeshProUGUI currentBusinessName;

	public TextMeshProUGUI hoursPerWeek;

	public TextMeshProUGUI satisfaction;

	public GameObject warningIcon;

	public Button hrManagerButton;

	public BasicTooltip hrManagerTooltip;

	public override void SetData(EmployeeModel data)
	{
		massActionToggle.SetIsOnWithoutNotify(MyEmployeesMassActionsUI.massActionSelectedEmployees.Contains(data.employeeInstance));
		massActionToggle.onValueChanged.RemoveAllListeners();
		massActionToggle.onValueChanged.AddListener(delegate(bool toggled)
		{
			if (PlayerAction.SelectMultipleElements.Pressing() && InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex != -1)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ToggleRangeOfEmployees(InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex, dataIndex, toggled);
			}
			else
			{
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ToggleSelectedEmployee(data.employeeInstance, toggled);
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex = dataIndex;
		});
		employeeName.text = data.employeeName;
		age.text = TimeHelper.GetYearsByDays(data.ageInDays).ToString();
		hourlyWage.text = data.hourlyWage.ToCurrencyFormat();
		primarySkill.Arguments = new
		{
			skillName = data.primarySkillName,
			percentage = data.primarySkillPercentage
		};
		task.Key = data.task ?? "myemployees_no_task";
		task.TextContainer.color = ((data.task == null) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : (data.isAbsent ? InstanceBehavior<GlobalReferences>.Instance.colors.yellow : InstanceBehavior<GlobalReferences>.Instance.colors.white));
		warningIcon.SetActive(data.isAbsent);
		currentBusinessName.text = (string.IsNullOrEmpty(data.currentBusinessName) ? "common_unassigned".GetLocalization() : data.currentBusinessName);
		currentBusinessName.color = (string.IsNullOrWhiteSpace(data.currentBusinessName) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		hoursPerWeek.text = data.hoursPerWeek.ToString();
		hrManagerButton.onClick.RemoveAllListeners();
		HrManagerPlan hrManagerPlan = HrManagerHelper.GetPlanFromId(data.employeeInstance.assignedHrManagerPlanId);
		if (hrManagerPlan?.HrManagerInstance == null)
		{
			hrManagerButton.interactable = false;
			hrManagerTooltip.enabled = false;
		}
		else
		{
			hrManagerButton.onClick.AddListener(delegate
			{
				ShowHrManagerPlan(hrManagerPlan);
			});
			if (hrManagerPlan.healthInsurancePlan == null)
			{
				hrManagerTooltip.titleKey = "myemployees_hrmanager_info";
				hrManagerTooltip.localizationArguments = new
				{
					hrManagerName = hrManagerPlan.HrManagerInstance.characterData.name
				};
			}
			else
			{
				hrManagerTooltip.titleKey = "myemployees_hrmanager_info_with_health_insurance";
				hrManagerTooltip.localizationArguments = new
				{
					hrManagerName = hrManagerPlan.HrManagerInstance.characterData.name,
					healthInsurance = hrManagerPlan.healthInsurancePlan.planType.GetLocalizeKey().GetLocalization()
				};
			}
			hrManagerButton.interactable = true;
			hrManagerTooltip.enabled = true;
		}
		satisfaction.text = Mathf.RoundToInt(data.satisfaction) + "%";
		TextMeshProUGUI textMeshProUGUI = satisfaction;
		float num = data.satisfaction;
		Color32 color = ((num <= 30f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((!(num <= 60f)) ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.yellow));
		textMeshProUGUI.color = color;
	}

	public override void RefreshCellView()
	{
		EmployeeModel employeeModel = InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.employeeScrollerController.data[dataIndex];
		SetData(employeeModel);
		if (InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance == employeeModel.employeeInstance)
		{
			VisualizeSelected(selected: true);
		}
	}

	private void ShowHrManagerPlan(HrManagerPlan hrManagerPlan)
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(hrManagerPlan.headquartersAddress, "HRManagers");
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.hrManagersPlanList.SelectPlan(hrManagerPlan);
	}
}
