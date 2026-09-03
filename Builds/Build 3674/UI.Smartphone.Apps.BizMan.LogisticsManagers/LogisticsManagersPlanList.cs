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

namespace UI.Smartphone.Apps.BizMan.LogisticsManagers;

public class LogisticsManagersPlanList : MonoBehaviour
{
	private const string WarehouseKey = "ba:businesstype_warehouse";

	private const string FactoryKey = "ba:businesstype_factory";

	public LogisticsManagerPlanUI logisticsManagerPlanUI;

	[SerializeField]
	private LogisticsManagersPlanListEntry entryTemplate;

	[SerializeField]
	private Transform buttonEntry;

	[SerializeField]
	private Button warehouseTabButton;

	[SerializeField]
	private Button factoryTabButton;

	[SerializeField]
	private TMP_Text tabHeaderLabel;

	[SerializeField]
	private ReorderableList reorderableList;

	[HideInInspector]
	public string currentTab;

	private readonly Dictionary<string, LogisticsManagersPlanListEntry> _entriesById = new Dictionary<string, LogisticsManagersPlanListEntry>();

	private List<EmployeeInstance> _logisticsManagers;

	private LogisticsManagersPlanListEntry _selectedEntry;

	private void Awake()
	{
		LogisticsManagerPlanUI obj = logisticsManagerPlanUI;
		obj.onWarehouseChanged = (Action<string>)Delegate.Combine(obj.onWarehouseChanged, new Action<string>(UpdateSelectedPlanWarehouseName));
		NoManagerAssignedPopUp noManagerAssignedPopUp = logisticsManagerPlanUI.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Combine(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshManagersList));
	}

	private void OnEnable()
	{
		reorderableList.OnItemReordered += OnPlanReordered;
		RefreshManagersList();
	}

	private void OnDisable()
	{
		reorderableList.OnItemReordered -= OnPlanReordered;
		entryTemplate.transform.ResetTemplate();
		_selectedEntry = null;
		_entriesById.Clear();
	}

	private void OnPlanReordered(int fromIndex, int toIndex)
	{
		List<LogisticsManagerPlan> filteredPlans = GetFilteredPlans();
		LogisticsManagerPlan item = filteredPlans[fromIndex];
		LogisticsManagerPlan item2 = filteredPlans[toIndex];
		List<LogisticsManagerPlan> logisticsManagerPlans = SaveGameManager.Current.logisticsManagerPlans;
		logisticsManagerPlans.Remove(item);
		int num = logisticsManagerPlans.IndexOf(item2);
		logisticsManagerPlans.Insert((fromIndex < toIndex) ? (num + 1) : num, item);
		SaveGameManager.MarkChange();
	}

	private void RefreshManagersList()
	{
		if (string.IsNullOrEmpty(currentTab))
		{
			ChangeTab("Warehouse");
			return;
		}
		logisticsManagerPlanUI.Hide();
		_selectedEntry = null;
		_entriesById.Clear();
		entryTemplate.transform.ResetTemplate();
		List<LogisticsManagerPlan> filteredPlans = GetFilteredPlans();
		for (int i = 0; i < filteredPlans.Count; i++)
		{
			SetUpPlanEntry(filteredPlans[i]);
		}
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
	}

	private List<LogisticsManagerPlan> GetFilteredPlans()
	{
		List<LogisticsManagerPlan> assignedPlansForHeadquarters = LogisticsManagerHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		bool flag = currentTab.Equals("factory", StringComparison.OrdinalIgnoreCase);
		List<LogisticsManagerPlan> list = new List<LogisticsManagerPlan>(assignedPlansForHeadquarters.Count);
		for (int i = 0; i < assignedPlansForHeadquarters.Count; i++)
		{
			LogisticsManagerPlan logisticsManagerPlan = assignedPlansForHeadquarters[i];
			if (logisticsManagerPlan.isFactory == flag)
			{
				list.Add(logisticsManagerPlan);
			}
		}
		return list;
	}

	private LogisticsManagersPlanListEntry SetUpPlanEntry(LogisticsManagerPlan plan)
	{
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(plan.assignedEmployeeId);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(plan.targetAddress);
		LogisticsManagersPlanListEntry entry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
		entry.Initialize(plan, delegate(LogisticsManagersPlanListEntry e)
		{
			SelectPlan(e.transform, e.Plan);
		});
		string managerName = ((employeeById != null) ? employeeById.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		string locationName = ((buildingRegistration != null) ? buildingRegistration.BusinessName : "common_unassigned".GetLocalization());
		entry.SetManager(managerName, employeeById != null);
		entry.SetLocationName(locationName);
		entry.SetSelected(isSelected: false);
		UI.Elements.Dropdown managerDropdown = entry.ManagerDropdown;
		managerDropdown.onOptionSelected.RemoveAllListeners();
		managerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			OnChangedLogisticsManager(plan, index - 1, entry, managerDropdown);
		});
		entry.gameObject.SetActive(value: true);
		_entriesById[plan.id] = entry;
		return entry;
	}

	private void UpdateSelectedPlanWarehouseName(string warehouseName)
	{
		if (!(_selectedEntry == null))
		{
			if (warehouseName == null)
			{
				warehouseName = "common_unassigned".GetLocalization();
			}
			_selectedEntry.SetLocationName(warehouseName);
		}
	}

	private void UpdateLogisticsManagerDropdownForPlan(LogisticsManagerPlan plan, LogisticsManagersPlanListEntry selectedEntry)
	{
		_logisticsManagers = SaveGameManager.Current.EmployeeInstances.FindAll((EmployeeInstance x) => x.assignedAddress == InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address && x.HasSkill("ba:skill_logisticsmanager") && !SaveGameManager.Current.logisticsManagerPlans.Exists((LogisticsManagerPlan y) => y.id != plan.id && y.assignedEmployeeId == x.id) && x.IsAssignedToAnyWorkShift());
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_logisticsManagers.Select((EmployeeInstance x) => x.GetEmployeeNameWithInfo()).ToList());
		int selectedOption = _logisticsManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1;
		selectedEntry.ManagerDropdown.SetOptions(list, localize: false, selectedOption);
		logisticsManagerPlanUI.noManagerAssignedPopUp.SetUpEmployeeDropdown(_logisticsManagers);
	}

	private void OnChangedLogisticsManager(LogisticsManagerPlan plan, int logisticsManagerIndex, LogisticsManagersPlanListEntry entry, UI.Elements.Dropdown dropdown)
	{
		string text = ((logisticsManagerIndex == -1) ? null : _logisticsManagers[logisticsManagerIndex].id);
		if (text == plan.assignedEmployeeId)
		{
			return;
		}
		if (text != null && LogisticsManagerPlan.CalculateMaxDestinations(plan.targetAddress, text) < plan.destinations.Count)
		{
			LanguageChangeEventDataHolder bodyData = ((plan.destinations.Count > LogisticsManagerPlan.GetMaxPossibleDestinations(plan.targetAddress)) ? "bizman_logisticsmanager_max_capacity_exceeded_confirm" : "bizman_logisticsmanager_lower_skill_confirm").Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				ChangeLogisticsManager(plan, _logisticsManagers[logisticsManagerIndex], entry);
			}, delegate
			{
				dropdown.SelectOption(_logisticsManagers.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
			});
		}
		else
		{
			ChangeLogisticsManager(plan, (logisticsManagerIndex == -1) ? null : _logisticsManagers[logisticsManagerIndex], entry);
		}
	}

	private void ChangeLogisticsManager(LogisticsManagerPlan plan, EmployeeInstance newLogisticsManager, LogisticsManagersPlanListEntry entry)
	{
		plan.assignedEmployeeId = newLogisticsManager?.id;
		SaveGameManager.MarkChange();
		bool flag = newLogisticsManager != null;
		string managerName = (flag ? newLogisticsManager.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		entry.SetManager(managerName, flag);
		if (!(_selectedEntry != entry))
		{
			SelectPlan(entry.transform, plan);
		}
	}

	public void SelectPlan(Transform entryTransform, LogisticsManagerPlan plan)
	{
		UnselectCurrentAgent();
		if (plan == null)
		{
			return;
		}
		LogisticsManagersPlanListEntry entry = null;
		if (entryTransform != null)
		{
			entry = entryTransform.GetComponent<LogisticsManagersPlanListEntry>();
		}
		if (entry == null)
		{
			_entriesById.TryGetValue(plan.id, out entry);
		}
		if (!(entry == null))
		{
			_selectedEntry = entry;
			_selectedEntry.SetSelected(isSelected: true);
			UpdateLogisticsManagerDropdownForPlan(plan, _selectedEntry);
			logisticsManagerPlanUI.LoadPlan(plan);
			logisticsManagerPlanUI.noManagerAssignedPopUp.onChooseEmployee = delegate(int chosenEmployeeIndex)
			{
				OnChangedLogisticsManager(plan, chosenEmployeeIndex, entry, logisticsManagerPlanUI.noManagerAssignedPopUp.employeeDropdown);
			};
		}
	}

	public void AddPlan()
	{
		LogisticsManagerPlan logisticsManagerPlan = new LogisticsManagerPlan
		{
			headquartersAddress = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address,
			isFactory = currentTab.Equals("factory", StringComparison.OrdinalIgnoreCase)
		};
		SaveGameManager.Current.logisticsManagerPlans.Add(logisticsManagerPlan);
		LogisticsManagersPlanListEntry logisticsManagersPlanListEntry = SetUpPlanEntry(logisticsManagerPlan);
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
		SelectPlan(logisticsManagersPlanListEntry.transform, logisticsManagerPlan);
		SaveGameManager.MarkChange();
	}

	private void UnselectCurrentAgent()
	{
		if (!(_selectedEntry == null))
		{
			_selectedEntry.SetSelected(isSelected: false);
			_selectedEntry = null;
		}
	}

	public void ChangeTab(string tab)
	{
		bool flag = tab.Equals("Warehouse", StringComparison.OrdinalIgnoreCase);
		bool flag2 = tab.Equals("Factory", StringComparison.OrdinalIgnoreCase);
		if (flag || flag2)
		{
			currentTab = tab;
			warehouseTabButton.transform.GetLabelByName("Label").color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
			factoryTabButton.transform.GetLabelByName("Label").color = (flag2 ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
			warehouseTabButton.interactable = !flag;
			factoryTabButton.interactable = !flag2;
			tabHeaderLabel.text = (flag ? "ba:businesstype_warehouse".GetLocalization() : "ba:businesstype_factory".GetLocalization());
			RefreshManagersList();
		}
	}
}
