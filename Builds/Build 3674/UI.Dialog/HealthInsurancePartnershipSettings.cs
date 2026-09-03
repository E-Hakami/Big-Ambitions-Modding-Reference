using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace UI.Dialog;

public class HealthInsurancePartnershipSettings : MonoBehaviour
{
	[SerializeField]
	public Dropdown hrManagerDropdown;

	[SerializeField]
	public TMP_Text employeeSkillValue;

	[SerializeField]
	public Dropdown planTypeDropdown;

	[HideInInspector]
	public HrManagerPlan selectedHrManagerPlan;

	[HideInInspector]
	public bool hasSelectedPlan;

	[HideInInspector]
	public HealthInsurancePlanType selectedPlanType;

	private float _selectedManagerSkill;

	private void Start()
	{
		selectedHrManagerPlan = null;
		hrManagerDropdown.SetPlaceholder("dialog_health_insurance_manager_select_hr_manager");
		planTypeDropdown.SetPlaceholder("dialog_health_insurance_manager_select_plan_type");
		List<HrManagerPlan> availablePlans = SaveGameManager.Current.hrManagerPlans.Where((HrManagerPlan x) => x.CanHaveHealthInsurancePlan).ToList();
		hrManagerDropdown.SetOptions(availablePlans.Select((HrManagerPlan x) => x.HrManagerInstance.characterData.name).ToList(), localize: false);
		IEnumerable<HealthInsurancePlanType> source = Enum.GetValues(typeof(HealthInsurancePlanType)).Cast<HealthInsurancePlanType>();
		planTypeDropdown.SetOptions(source.Select((HealthInsurancePlanType x) => x.GetLocalizeKey()).ToList());
		hrManagerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			SelectHrManagerPlan(availablePlans[index]);
		});
		planTypeDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			SelectPlan((HealthInsurancePlanType)index);
		});
	}

	private void SelectHrManagerPlan(HrManagerPlan hrManagerPlan)
	{
		selectedHrManagerPlan = hrManagerPlan;
		_selectedManagerSkill = hrManagerPlan.HrManagerSkillValue;
		employeeSkillValue.text = $"{Mathf.FloorToInt(_selectedManagerSkill)}%";
		if (_selectedManagerSkill < selectedPlanType.GetMinSkillForPlan())
		{
			planTypeDropdown.ResetSelectedOption();
			hasSelectedPlan = false;
		}
		planTypeDropdown.SetInteractable(interactable: true);
	}

	private void SelectPlan(HealthInsurancePlanType planType)
	{
		if (_selectedManagerSkill < planType.GetMinSkillForPlan())
		{
			planTypeDropdown.ResetSelectedOption();
			hasSelectedPlan = false;
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"healthPlanType",
				planType.GetLocalizeKey()
			} };
			Notifications.Show(NotificationType.Error, "dialog_health_insurance_manager_hr_manager_skill_too_low", notificationData, 4f, "hrManagerSkillTooLow");
		}
		else
		{
			hasSelectedPlan = true;
			selectedPlanType = planType;
		}
	}
}
