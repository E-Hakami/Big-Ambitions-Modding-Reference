using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public class HrManagersPlanList : MonoBehaviour
{
	[SerializeField]
	private HrManagerPlanUI hrManagerPlanUI;

	[SerializeField]
	private Transform entryTemplate;

	[SerializeField]
	private Transform buttonEntry;

	[SerializeField]
	private Sprite bronzeHealthInsuranceIcon;

	[SerializeField]
	private Sprite silverHealthInsuranceIcon;

	[SerializeField]
	private Sprite goldHealthInsuranceIcon;

	[SerializeField]
	private ReorderableList reorderableList;

	private Transform _selectedEntry;

	private List<EmployeeInstance> _hrManagers;

	private readonly Dictionary<string, Transform> _entriesByPlanIds = new Dictionary<string, Transform>();

	private void Awake()
	{
		HrManagerPlanUI obj = hrManagerPlanUI;
		obj.onAssignedEmployeesChanged = (Action<int, int>)Delegate.Combine(obj.onAssignedEmployeesChanged, new Action<int, int>(UpdateSelectedPlanAssignedEmployees));
		HrManagerPlanUI obj2 = hrManagerPlanUI;
		obj2.onHealthInsuranceChanged = (Action<HrManagerPlan>)Delegate.Combine(obj2.onHealthInsuranceChanged, new Action<HrManagerPlan>(UpdateSelectedPlanHealthInsurance));
		NoManagerAssignedPopUp noManagerAssignedPopUp = hrManagerPlanUI.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Combine(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshManagersList));
	}

	private void OnEnable()
	{
		RefreshManagersList();
		reorderableList.OnItemReordered += UpdatePlanOrder;
	}

	private void OnDisable()
	{
		entryTemplate.ResetTemplate();
		_entriesByPlanIds.Clear();
		reorderableList.OnItemReordered -= UpdatePlanOrder;
	}

	private static void UpdatePlanOrder(int fromIndex, int toIndex)
	{
		List<HrManagerPlan> assignedPlansForHeadquarters = HrManagerHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		HrManagerPlan item = assignedPlansForHeadquarters[fromIndex];
		HrManagerPlan item2 = assignedPlansForHeadquarters[toIndex];
		List<HrManagerPlan> hrManagerPlans = SaveGameManager.Current.hrManagerPlans;
		hrManagerPlans.Remove(item);
		int num = hrManagerPlans.IndexOf(item2);
		hrManagerPlans.Insert((fromIndex < toIndex) ? (num + 1) : num, item);
		SaveGameManager.MarkChange();
	}

	private void RefreshManagersList()
	{
		hrManagerPlanUI.Hide();
		_selectedEntry = null;
		entryTemplate.ResetTemplate();
		_entriesByPlanIds.Clear();
		foreach (HrManagerPlan assignedPlansForHeadquarter in HrManagerHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address))
		{
			SetUpPlanEntry(assignedPlansForHeadquarter);
		}
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
	}

	private void SetUpPlanEntry(HrManagerPlan plan)
	{
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(plan.assignedEmployeeId);
		Transform entry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.parent);
		entry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.353f);
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectPlan(plan);
		});
		_entriesByPlanIds.Add(plan.id, entry);
		entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(employeeById == null);
		TextMeshProUGUI labelByName = entry.GetLabelByName("ManagerName");
		labelByName.margin = new Vector4((employeeById == null) ? 50 : 0, 0f, 0f, 0f);
		labelByName.text = ((employeeById != null) ? employeeById.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedEmployees}/{plan.MaxEmployees}";
		UI.Elements.Dropdown managerDropdown = entry.GetDropDownByName("ManagerDropdown");
		managerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			OnChangedHrManager(plan, index - 1, entry, managerDropdown);
		});
		Image component = entry.Find("HealthInsuranceIcon").GetComponent<Image>();
		if (plan.healthInsurancePlan != null)
		{
			switch (plan.healthInsurancePlan.planType)
			{
			case HealthInsurancePlanType.Bronze:
				component.sprite = bronzeHealthInsuranceIcon;
				break;
			case HealthInsurancePlanType.Silver:
				component.sprite = silverHealthInsuranceIcon;
				break;
			case HealthInsurancePlanType.Gold:
				component.sprite = goldHealthInsuranceIcon;
				break;
			}
			component.enabled = true;
		}
		else
		{
			component.enabled = false;
		}
		entry.gameObject.SetActive(value: true);
	}

	private void UpdateSelectedPlanAssignedEmployees(int assignedEmployees, int maxEmployees)
	{
		_selectedEntry.GetLabelByName("Info").text = $"{assignedEmployees}/{maxEmployees}";
	}

	private void UpdateSelectedPlanHealthInsurance(HrManagerPlan plan)
	{
		Image component = _selectedEntry.Find("HealthInsuranceIcon").GetComponent<Image>();
		if (plan.healthInsurancePlan == null)
		{
			component.enabled = false;
			return;
		}
		switch (plan.healthInsurancePlan.planType)
		{
		case HealthInsurancePlanType.Bronze:
			component.sprite = bronzeHealthInsuranceIcon;
			break;
		case HealthInsurancePlanType.Silver:
			component.sprite = silverHealthInsuranceIcon;
			break;
		case HealthInsurancePlanType.Gold:
			component.sprite = goldHealthInsuranceIcon;
			break;
		}
		component.enabled = true;
	}

	private void UpdateHrManagerDropdownForPlan(HrManagerPlan plan, Transform selectedEntry)
	{
		_hrManagers = SaveGameManager.Current.EmployeeInstances.FindAll((EmployeeInstance x) => x.assignedAddress == InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address && x.HasSkill("ba:skill_hrmanager") && !SaveGameManager.Current.hrManagerPlans.Exists((HrManagerPlan y) => y.id != plan.id && y.assignedEmployeeId == x.id) && x.IsAssignedToAnyWorkShift());
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_hrManagers.Select((EmployeeInstance x) => x.GetEmployeeNameWithInfo()).ToList());
		int selectedOption = _hrManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1;
		selectedEntry.Find("ManagerDropdown").GetComponent<UI.Elements.Dropdown>().SetOptions(list, localize: false, selectedOption);
		hrManagerPlanUI.noManagerAssignedPopUp.SetUpEmployeeDropdown(_hrManagers);
	}

	private void OnChangedHrManager(HrManagerPlan plan, int hrManagerIndex, Transform entry, UI.Elements.Dropdown dropdown)
	{
		string text = ((hrManagerIndex == -1) ? null : _hrManagers[hrManagerIndex].id);
		if (text == plan.assignedEmployeeId)
		{
			return;
		}
		if (text != null)
		{
			float skillValue = EmployeeHelper.GetEmployeeById(text).GetSkillValue("ba:skill_hrmanager");
			int newHrManagerMaxEmployees = skillValue.CalculateMaxAssignableEmployees();
			bool flag = newHrManagerMaxEmployees >= plan.assignedEmployees.Count;
			bool flag2 = true;
			if (plan.healthInsurancePlan != null)
			{
				flag2 = skillValue >= plan.healthInsurancePlan.planType.GetMinSkillForPlan();
			}
			if (!flag && !flag2)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_hrmanagers_cant_handle_all_employees_and_health_insurance_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					while (plan.assignedEmployees.Count > newHrManagerMaxEmployees)
					{
						List<string> assignedEmployees = plan.assignedEmployees;
						EmployeeHelper.GetEmployeeById(assignedEmployees[assignedEmployees.Count - 1]).assignedHrManagerPlanId = null;
						plan.assignedEmployees.RemoveAt(plan.assignedEmployees.Count - 1);
					}
					plan.CancelHealthInsurancePlan();
					ChangeHrManager(plan, _hrManagers[hrManagerIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
					entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedEmployees}/{plan.MaxEmployees}";
				}, delegate
				{
					dropdown.SelectOption(_hrManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
			if (!flag2)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_hrmanagers_cant_handle_health_insurance_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					plan.CancelHealthInsurancePlan();
					ChangeHrManager(plan, _hrManagers[hrManagerIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
				}, delegate
				{
					dropdown.SelectOption(_hrManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
			if (!flag)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_hrmanagers_cant_handle_all_employees_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					while (plan.assignedEmployees.Count > newHrManagerMaxEmployees)
					{
						List<string> assignedEmployees = plan.assignedEmployees;
						EmployeeHelper.GetEmployeeById(assignedEmployees[assignedEmployees.Count - 1]).assignedHrManagerPlanId = null;
						plan.assignedEmployees.RemoveAt(plan.assignedEmployees.Count - 1);
					}
					ChangeHrManager(plan, _hrManagers[hrManagerIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
					entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedEmployees}/{plan.MaxEmployees}";
				}, delegate
				{
					dropdown.SelectOption(_hrManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
		}
		ChangeHrManager(plan, (hrManagerIndex == -1) ? null : _hrManagers[hrManagerIndex], entry);
		entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: true);
		entry.GetLabelByName("ManagerName").margin = new Vector4(50f, 0f, 0f, 0f);
	}

	private void ChangeHrManager(HrManagerPlan plan, EmployeeInstance newHrManager, Transform entry)
	{
		plan.AssignEmployee(newHrManager?.id);
		if (newHrManager != null && newHrManager.assignedHrManagerPlanId == plan.id)
		{
			plan.assignedEmployees.Remove(newHrManager.id);
			newHrManager.assignedHrManagerPlanId = null;
		}
		entry.GetLabelByName("ManagerName").text = ((newHrManager != null) ? newHrManager.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		UpdateSelectedPlanAssignedEmployees(plan.NumberOfAssignedEmployees, plan.MaxEmployees);
		if (!(_selectedEntry != entry))
		{
			SelectPlan(plan);
		}
	}

	public void SelectPlan(HrManagerPlan plan)
	{
		Transform entry = _entriesByPlanIds[plan.id];
		UnselectCurrentAgent();
		UpdateHrManagerDropdownForPlan(plan, entry);
		_selectedEntry = entry;
		_selectedEntry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
		_selectedEntry.Find("ManagerName").gameObject.SetActive(value: false);
		_selectedEntry.Find("ManagerDropdown").gameObject.SetActive(value: true);
		_selectedEntry.GetLabelByName("Info").color = Colors.Midnight;
		hrManagerPlanUI.LoadPlan(plan);
		hrManagerPlanUI.noManagerAssignedPopUp.onChooseEmployee = delegate(int chosenEmployeeIndex)
		{
			OnChangedHrManager(plan, chosenEmployeeIndex, entry, hrManagerPlanUI.noManagerAssignedPopUp.employeeDropdown);
		};
	}

	public void AddPlan()
	{
		HrManagerPlan hrManagerPlan = new HrManagerPlan
		{
			headquartersAddress = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address
		};
		SaveGameManager.Current.hrManagerPlans.Add(hrManagerPlan);
		SetUpPlanEntry(hrManagerPlan);
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
		SelectPlan(hrManagerPlan);
		SaveGameManager.MarkChange();
	}

	private void UnselectCurrentAgent()
	{
		if (!(_selectedEntry == null))
		{
			_selectedEntry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.353f);
			_selectedEntry.Find("ManagerName").gameObject.SetActive(value: true);
			_selectedEntry.Find("ManagerDropdown").gameObject.SetActive(value: false);
			_selectedEntry.GetLabelByName("Info").color = Color.white;
			_selectedEntry = null;
		}
	}
}
