using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.HRManagers;
using UnityEngine;
using UnityEngine.UI;

public class HeadhuntersAutomaticReplacementTab : MonoBehaviour
{
	[SerializeField]
	private HeadhunterPlanUI planUI;

	[SerializeField]
	private GameObject notEnoughSkillWarning;

	[SerializeField]
	private GameObject automaticReplacementPanel;

	[SerializeField]
	private Transform hrManagerSlotTemplate;

	[SerializeField]
	private Toggle automaticallyReplaceOnResignToggle;

	[SerializeField]
	private Toggle automaticallyReplaceOnRetireToggle;

	[SerializeField]
	private HeadhunterEmployeesScrollerController employeeScrollerController;

	[SerializeField]
	private TextLocalizationComponent resignReplacementFee;

	[SerializeField]
	private TextLocalizationComponent retireReplacementFee;

	private List<string> _hrManagerPlansIds;

	private void Awake()
	{
		automaticallyReplaceOnResignToggle.onValueChanged.AddListener(ToggleAutomaticReplacementOnResign);
		automaticallyReplaceOnRetireToggle.onValueChanged.AddListener(ToggleAutomaticReplacementOnRetire);
		resignReplacementFee.Arguments = new
		{
			replacementFee = 2500f.ToShortCurrencyFormat()
		};
		retireReplacementFee.Arguments = new
		{
			replacementFee = 2500f.ToShortCurrencyFormat()
		};
	}

	private void OnEnable()
	{
		SetUpAutomaticReplacementInfo();
	}

	private void SetUpAutomaticReplacementInfo()
	{
		if (planUI.currentPlan.MaxHrPlansThatCanBeAssigned == 0)
		{
			ShowNotEnoughSkillWarning();
		}
		else
		{
			ShowAutomaticReplacementInfo();
		}
	}

	private void ShowAutomaticReplacementInfo()
	{
		notEnoughSkillWarning.SetActive(value: false);
		SetUpHrManagerPlansList();
		SetUpToggles();
		employeeScrollerController.Load(planUI.currentPlan);
		automaticReplacementPanel.SetActive(value: true);
	}

	private void ShowNotEnoughSkillWarning()
	{
		automaticReplacementPanel.SetActive(value: false);
		notEnoughSkillWarning.SetActive(value: true);
	}

	private void SetUpHrManagerPlansList()
	{
		_hrManagerPlansIds = new List<string> { null };
		_hrManagerPlansIds.AddRange(from x in SaveGameManager.Current.hrManagerPlans
			where !SaveGameManager.Current.headhunterPlans.Exists((HeadhunterPlan plan) => plan != planUI.currentPlan && plan.assignedHrPlans.Contains(x.id))
			select x.id);
		List<string> newOptions = _hrManagerPlansIds.Select(GetDropdownPlanName).ToList();
		hrManagerSlotTemplate.ResetTemplate();
		for (int num = 0; num < planUI.currentPlan.MaxHrPlansThatCanBeAssigned; num++)
		{
			int slotNumber = num;
			Transform transform = hrManagerSlotTemplate.CreateElement();
			transform.GetLanguageChangeEventByName("HRManagerSlotLabel").Suffix = $" {slotNumber + 1}";
			UI.Elements.Dropdown hrManagerPlanDropdown = transform.Find("HRManagerDropdown").GetComponent<UI.Elements.Dropdown>();
			hrManagerPlanDropdown.SetOptions(newOptions, localize: false, _hrManagerPlansIds.IndexOf(planUI.currentPlan.assignedHrPlans[slotNumber]));
			hrManagerPlanDropdown.onOptionSelected.AddListener(delegate(int index)
			{
				SelectHrManagerPlan(slotNumber, index, hrManagerPlanDropdown);
			});
		}
	}

	private string GetDropdownPlanName(string hrManagerPlanId)
	{
		if (string.IsNullOrEmpty(hrManagerPlanId))
		{
			return "headhunter_select_employee".GetLocalization();
		}
		HrManagerPlan plan = SaveGameManager.Current.hrManagerPlans.First((HrManagerPlan hrManagerPlan) => hrManagerPlan.id == hrManagerPlanId);
		if (string.IsNullOrEmpty(plan.assignedEmployeeId))
		{
			int number = SaveGameManager.Current.hrManagerPlans.Where((HrManagerPlan y) => y.headquartersAddress == plan.headquartersAddress).ToList().IndexOf(plan) + 1;
			string businessName = BuildingHelper.GetBuildingRegistration(plan.headquartersAddress).BusinessName;
			return LocalizorManager.GetLocalization("bizman_headhunter_unassigned_hr_plan_name", new
			{
				number = number,
				headquartersName = businessName
			});
		}
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(plan.assignedEmployeeId);
		if (!employeeById.isBeingReplaced)
		{
			return employeeById.characterData.name;
		}
		return EmployeeHelper.GetAwaitingReplacementText();
	}

	private void SelectHrManagerPlan(int slot, int planIndex, UI.Elements.Dropdown dropdown)
	{
		if (_hrManagerPlansIds[planIndex] != null && planUI.currentPlan.assignedHrPlans.Contains(_hrManagerPlansIds[planIndex]))
		{
			planUI.currentPlan.assignedHrPlans[slot] = null;
			Notifications.ShowError("headhunter_selected_employee_already_selected");
			dropdown.SelectOption(0);
		}
		else
		{
			planUI.currentPlan.assignedHrPlans[slot] = _hrManagerPlansIds[planIndex];
		}
		int numberOfAssignedHrPlans = planUI.currentPlan.NumberOfAssignedHrPlans;
		int maxHrPlansThatCanBeAssigned = planUI.currentPlan.MaxHrPlansThatCanBeAssigned;
		planUI.onHrManagerPlansChanged(numberOfAssignedHrPlans, maxHrPlansThatCanBeAssigned);
		employeeScrollerController.Load(planUI.currentPlan);
	}

	private void SetUpToggles()
	{
		automaticallyReplaceOnResignToggle.SetIsOnWithoutNotify(planUI.currentPlan.automaticallyReplaceOnResign);
		automaticallyReplaceOnRetireToggle.SetIsOnWithoutNotify(planUI.currentPlan.automaticallyReplaceOnRetire);
	}

	private void ToggleAutomaticReplacementOnResign(bool toggled)
	{
		planUI.currentPlan.automaticallyReplaceOnResign = toggled;
	}

	private void ToggleAutomaticReplacementOnRetire(bool toggled)
	{
		planUI.currentPlan.automaticallyReplaceOnRetire = toggled;
	}
}
