using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UI.Smartphone.Apps.BizMan.HrManagers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public class HrManagerPlanUI : MonoBehaviour
{
	public NoManagerAssignedPopUp noManagerAssignedPopUp;

	public Action<int, int> onAssignedEmployeesChanged;

	public Action<HrManagerPlan> onHealthInsuranceChanged;

	[SerializeField]
	private TextLocalizationComponent employeesAssignedLabel;

	[SerializeField]
	private Toggle replaceAbsentEmployeesToggle;

	[SerializeField]
	private Slider trainingTargetSlider;

	[SerializeField]
	private TMP_Text trainingTargetValue;

	[SerializeField]
	private TextLocalizationComponent averageSalaryLabel;

	[SerializeField]
	private ProgressBar averageSatisfactionProgressBar;

	[SerializeField]
	private ProgressBar averagePrimarySkillProgressBar;

	[SerializeField]
	private EmployeesScrollerController employeesScrollerController;

	[SerializeField]
	private Transform assignEmployeesList;

	[SerializeField]
	private EmployeesScrollerController assignableEmployeesScrollerController;

	[SerializeField]
	private TextLocalizationComponent assignableEmployeesTitle;

	[SerializeField]
	private TextLocalizationComponent healthInsuranceInfoLabel;

	[SerializeField]
	private GameObject cancelHealthInsuranceButton;

	[SerializeField]
	private GameObject upgradeHealthInsuranceButton;

	[SerializeField]
	private TextLocalizationComponent noHealthInsuranceInfoLabel;

	private HrManagerPlan _currentPlan;

	public bool IsAssignEmployeesListOpen => assignEmployeesList.gameObject.activeInHierarchy;

	private void Awake()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Combine(obj.deletePlan, new Action(DeletePlan));
	}

	public void LoadPlan(HrManagerPlan plan)
	{
		_currentPlan = plan;
		replaceAbsentEmployeesToggle.onValueChanged.RemoveAllListeners();
		replaceAbsentEmployeesToggle.onValueChanged.AddListener(delegate(bool value)
		{
			_currentPlan.replaceAbsentEmployees = value;
		});
		replaceAbsentEmployeesToggle.SetIsOnWithoutNotify(_currentPlan.replaceAbsentEmployees);
		trainingTargetSlider.onValueChanged.RemoveAllListeners();
		trainingTargetSlider.onValueChanged.AddListener(delegate(float newValue)
		{
			int num = Mathf.RoundToInt(newValue);
			_currentPlan.trainingTarget = num;
			trainingTargetValue.text = $"{num}%";
		});
		trainingTargetSlider.value = _currentPlan.trainingTarget;
		CloseAssignEmployeesList();
		SetUpBasicData(_currentPlan.EmployeeInstances);
		SetUpHealthInsuranceInfo();
		if (plan.assignedEmployeeId == null)
		{
			noManagerAssignedPopUp.Show();
		}
		else
		{
			noManagerAssignedPopUp.Hide();
		}
		base.gameObject.SetActive(value: true);
	}

	private void SetUpBasicData(List<EmployeeInstance> employees = null)
	{
		employeesAssignedLabel.Arguments = new
		{
			amount = _currentPlan.NumberOfAssignedEmployees,
			max = _currentPlan.MaxEmployees
		};
		if (employees == null)
		{
			employees = _currentPlan.EmployeeInstances;
		}
		if (employees.Count == 0)
		{
			averageSalaryLabel.Arguments = new
			{
				wage = "-"
			};
			averageSatisfactionProgressBar.SetValue(0f);
			averagePrimarySkillProgressBar.SetValue(0f);
		}
		else
		{
			averageSalaryLabel.Arguments = new
			{
				wage = employees.Average((EmployeeInstance x) => x.hourlyWage).ToCurrencyFormat()
			};
			averageSatisfactionProgressBar.SetValue(employees.Average((EmployeeInstance x) => x.satisfaction));
			averagePrimarySkillProgressBar.SetValue(employees.Average((EmployeeInstance x) => x.characterData.skills[0].value));
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			employeesScrollerController.Load(employees, includeUnassignedEmployees: false, _currentPlan.assignedEmployeeId);
		});
		assignableEmployeesTitle.Arguments = new
		{
			manager = _currentPlan.HrManagerInstance?.characterData.name,
			amount = _currentPlan.NumberOfAssignedEmployees,
			max = _currentPlan.MaxEmployees
		};
	}

	private void SetUpHealthInsuranceInfo()
	{
		if (!_currentPlan.HasActiveHealthInsurance())
		{
			healthInsuranceInfoLabel.gameObject.SetActive(value: false);
			cancelHealthInsuranceButton.SetActive(value: false);
			upgradeHealthInsuranceButton.SetActive(value: false);
			noHealthInsuranceInfoLabel.gameObject.SetActive(value: true);
			noHealthInsuranceInfoLabel.Key = (SaveGameManager.Current.healthInsurancePlanOffers.Exists((HealthInsurancePlanOffer x) => x.hrManagerPlanId == _currentPlan.id && !x.negotiationFinished) ? "bizman_hrmanager_health_insurance_wait_for_hospital" : "bizman_hrmanager_no_health_insurance_info");
		}
		else
		{
			healthInsuranceInfoLabel.gameObject.SetActive(value: true);
			cancelHealthInsuranceButton.SetActive(value: true);
			upgradeHealthInsuranceButton.SetActive(_currentPlan.CanUpgradeHealthInsurancePlan());
			noHealthInsuranceInfoLabel.gameObject.SetActive(value: false);
			healthInsuranceInfoLabel.Arguments = new
			{
				planType = _currentPlan.healthInsurancePlan.planType.GetLocalizeKey(),
				pricePerDayAndEmployee = _currentPlan.healthInsurancePlan.pricePerDayAndEmployee.ToCurrencyFormat(),
				minimumEmployees = HealthInsuranceHelper.MinimumEmployeesInCharge
			};
		}
	}

	public void CancelInsurance()
	{
		LanguageChangeEventDataHolder bodyData = "bizman_hrmanager_cancel_insurance_confirm".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			_currentPlan.CancelHealthInsurancePlan();
			SetUpHealthInsuranceInfo();
			onHealthInsuranceChanged?.Invoke(_currentPlan);
		});
	}

	public void UpgradeInsurance()
	{
		LanguageChangeEventDataHolder bodyData = "bizman_hrmanager_upgrade_insurance_confirm".Localize(new
		{
			currentInsurance = _currentPlan.healthInsurancePlan.planType.GetLocalizeKey().Localize(),
			newInsurance = _currentPlan.healthInsurancePlan.planType.Next().GetLocalizeKey().Localize(),
			price = _currentPlan.GetUpgradeHealthInsurancePlanPrice().ToCurrencyFormat()
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			_currentPlan.UpgradeHealthInsurancePlan();
			SetUpHealthInsuranceInfo();
			onHealthInsuranceChanged?.Invoke(_currentPlan);
		});
	}

	public void OpenAssignEmployeesList()
	{
		assignEmployeesList.gameObject.SetActive(value: true);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			assignableEmployeesScrollerController.Load(_currentPlan.EmployeeInstances, includeUnassignedEmployees: true, _currentPlan.assignedEmployeeId);
		});
	}

	public bool CanAssignEmployee()
	{
		if (_currentPlan != null)
		{
			return _currentPlan.NumberOfAssignedEmployees < _currentPlan.MaxEmployees;
		}
		return false;
	}

	public void Fill()
	{
		int num = _currentPlan.MaxEmployees - _currentPlan.NumberOfAssignedEmployees;
		if (num <= 0)
		{
			return;
		}
		foreach (EmployeeInstance item in (from x in assignableEmployeesScrollerController.data
			select EmployeeHelper.GetEmployeeById(x.employeeId) into x
			where string.IsNullOrEmpty(x.assignedHrManagerPlanId) && x.id != _currentPlan.assignedEmployeeId
			select x).Take(num))
		{
			SetEmployeeAssigned(item.id, assigned: true, refreshData: false);
		}
		assignableEmployeesScrollerController.Load(_currentPlan.EmployeeInstances, includeUnassignedEmployees: true, _currentPlan.assignedEmployeeId);
		SetUpBasicData();
	}

	public void ClearAssignedEmployees()
	{
		if (_currentPlan.NumberOfAssignedEmployees == 0)
		{
			return;
		}
		foreach (string item in _currentPlan.assignedEmployees.ToList())
		{
			SetEmployeeAssigned(item, assigned: false, refreshData: false);
		}
		assignableEmployeesScrollerController.Load(_currentPlan.EmployeeInstances, includeUnassignedEmployees: true, _currentPlan.assignedEmployeeId);
		SetUpBasicData();
	}

	public void SetEmployeeAssigned(string employeeId, bool assigned, bool refreshData = true)
	{
		if (assigned && !_currentPlan.assignedEmployees.Contains(employeeId))
		{
			_currentPlan.assignedEmployees.Add(employeeId);
			EmployeeHelper.GetEmployeeById(employeeId).assignedHrManagerPlanId = _currentPlan.id;
		}
		else if (!assigned && _currentPlan.assignedEmployees.Contains(employeeId))
		{
			_currentPlan.assignedEmployees.Remove(employeeId);
			EmployeeHelper.GetEmployeeById(employeeId).assignedHrManagerPlanId = null;
		}
		if (refreshData)
		{
			SetUpBasicData();
		}
		onAssignedEmployeesChanged(_currentPlan.NumberOfAssignedEmployees, _currentPlan.MaxEmployees);
	}

	public void CloseAssignEmployeesList()
	{
		assignEmployeesList.gameObject.SetActive(value: false);
	}

	private void DeletePlan()
	{
		if (_currentPlan == null)
		{
			Debug.LogError("No plan selected");
		}
		else
		{
			HrManagerHelper.DeletePlan(_currentPlan.id);
		}
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		noManagerAssignedPopUp.Hide();
	}

	private void OnDisable()
	{
		CloseAssignEmployeesList();
		_currentPlan = null;
	}
}
