using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Components;
using UI.Elements;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagersPlanList : MonoBehaviour
{
	[SerializeField]
	private PricingManagerPlanUI pricingManagerPlanUI;

	[SerializeField]
	private PricingManagersPlanListEntry entryTemplate;

	[SerializeField]
	private Transform buttonEntry;

	[SerializeField]
	private TextLocalizationComponent ownedBusinessesLabel;

	[SerializeField]
	private TogglePanel filterPanel;

	[SerializeField]
	private ReorderableList reorderableList;

	private readonly List<PricingManagersPlanListEntry> _entries = new List<PricingManagersPlanListEntry>();

	private List<EmployeeInstance> _pricingManagers;

	private PricingManagersPlanListEntry _selectedEntry;

	private void OnEnable()
	{
		pricingManagerPlanUI.onNeighborhoodChanged += UpdateSelectedPlanNeighborhoodName;
		NoManagerAssignedPopUp noManagerAssignedPopUp = pricingManagerPlanUI.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Combine(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshPlansList));
		reorderableList.OnItemReordered += UpdatePlanOrder;
		RefreshPlansList();
	}

	private void OnDisable()
	{
		pricingManagerPlanUI.onNeighborhoodChanged -= UpdateSelectedPlanNeighborhoodName;
		NoManagerAssignedPopUp noManagerAssignedPopUp = pricingManagerPlanUI.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Remove(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshPlansList));
		reorderableList.OnItemReordered -= UpdatePlanOrder;
		filterPanel.Close();
		entryTemplate.transform.ResetTemplate();
		_entries.Clear();
		_selectedEntry = null;
	}

	public void AddPlan()
	{
		PricingManagerPlan pricingManagerPlan = new PricingManagerPlan
		{
			headquartersAddress = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address
		};
		PricingManagerHelper.AddPlan(pricingManagerPlan);
		SetUpPlanEntry(pricingManagerPlan);
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
		List<PricingManagersPlanListEntry> entries = _entries;
		SelectPlan(entries[entries.Count - 1]);
		SaveGameManager.MarkChange();
	}

	private static void UpdatePlanOrder(int fromIndex, int toIndex)
	{
		List<PricingManagerPlan> plansForHeadquarters = PricingManagerHelper.GetPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		PricingManagerPlan item = plansForHeadquarters[fromIndex];
		PricingManagerPlan item2 = plansForHeadquarters[toIndex];
		List<PricingManagerPlan> pricingManagerPlans = SaveGameManager.Current.pricingManagerPlans;
		pricingManagerPlans.Remove(item);
		int num = pricingManagerPlans.IndexOf(item2);
		pricingManagerPlans.Insert((fromIndex < toIndex) ? (num + 1) : num, item);
		SaveGameManager.MarkChange();
	}

	private void RefreshPlansList()
	{
		pricingManagerPlanUI.Hide();
		_selectedEntry = null;
		_entries.Clear();
		entryTemplate.transform.ResetTemplate();
		foreach (PricingManagerPlan plansForHeadquarter in PricingManagerHelper.GetPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address))
		{
			SetUpPlanEntry(plansForHeadquarter);
		}
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
		UpdateOwnedBusinessesLabel();
	}

	private void SetUpPlanEntry(PricingManagerPlan plan)
	{
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(plan.assignedEmployeeId);
		PricingManagersPlanListEntry pricingManagersPlanListEntry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
		pricingManagersPlanListEntry.Initialize(plan, SelectPlan);
		_entries.Add(pricingManagersPlanListEntry);
		string managerName = ((employeeById != null) ? employeeById.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		pricingManagersPlanListEntry.SetManager(managerName, employeeById != null);
		pricingManagersPlanListEntry.SetNeighborhoodName(GetNeighborhoodDisplayName(plan.supervisedNeighborhood));
		pricingManagersPlanListEntry.SetSelected(isSelected: false);
		pricingManagersPlanListEntry.gameObject.SetActive(value: true);
	}

	public void SelectPlan(PricingManagersPlanListEntry entry)
	{
		UnselectCurrentPlan();
		if (entry == null)
		{
			UpdateOwnedBusinessesLabel();
			return;
		}
		_selectedEntry = entry;
		_selectedEntry.SetSelected(isSelected: true);
		PricingManagerPlan plan = entry.Plan;
		UpdatePricingManagerDropdownForPlan(plan, entry);
		Dropdown managerDropdown = entry.ManagerDropdown;
		managerDropdown.onOptionSelected.RemoveAllListeners();
		managerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			OnChangedPricingManager(plan, index - 1, entry);
		});
		pricingManagerPlanUI.LoadPlan(plan);
		pricingManagerPlanUI.noManagerAssignedPopUp.onChooseEmployee = delegate(int chosenEmployeeIndex)
		{
			OnChangedPricingManager(plan, chosenEmployeeIndex, entry);
		};
		UpdateOwnedBusinessesLabel();
	}

	public void SelectPlanById(string planId)
	{
		foreach (PricingManagersPlanListEntry entry in _entries)
		{
			if (!(entry.Plan.id != planId))
			{
				SelectPlan(entry);
				break;
			}
		}
	}

	private void UnselectCurrentPlan()
	{
		if (!(_selectedEntry == null))
		{
			_selectedEntry.SetSelected(isSelected: false);
			_selectedEntry = null;
		}
	}

	private void UpdateSelectedPlanNeighborhoodName(string neighborhood)
	{
		if (!(_selectedEntry == null))
		{
			_selectedEntry.SetNeighborhoodName(GetNeighborhoodDisplayName(neighborhood));
			UpdateOwnedBusinessesLabel();
		}
	}

	private void UpdatePricingManagerDropdownForPlan(PricingManagerPlan plan, PricingManagersPlanListEntry selectedEntry)
	{
		_pricingManagers = new List<EmployeeInstance>();
		foreach (EmployeeInstance employeeInstance in SaveGameManager.Current.EmployeeInstances)
		{
			if (employeeInstance.assignedAddress == InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address && employeeInstance.HasSkill("ba:skill_pricingmanager") && !PricingManagerHelper.IsEmployeeAssignedToOtherPlan(employeeInstance.id, plan.id) && employeeInstance.IsAssignedToAnyWorkShift())
			{
				_pricingManagers.Add(employeeInstance);
			}
		}
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		foreach (EmployeeInstance pricingManager in _pricingManagers)
		{
			list.Add(pricingManager.GetEmployeeNameWithInfo());
		}
		selectedEntry.ManagerDropdown.SetOptions(list, localize: false, GetDropdownIndex(plan.assignedEmployeeId));
		pricingManagerPlanUI.noManagerAssignedPopUp.SetUpEmployeeDropdown(_pricingManagers);
	}

	private int GetDropdownIndex(string employeeId)
	{
		for (int i = 0; i < _pricingManagers.Count; i++)
		{
			if (_pricingManagers[i].id == employeeId)
			{
				return i + 1;
			}
		}
		return 0;
	}

	private void OnChangedPricingManager(PricingManagerPlan plan, int pricingManagerIndex, PricingManagersPlanListEntry entry)
	{
		EmployeeInstance newPricingManager = ((pricingManagerIndex == -1) ? null : _pricingManagers[pricingManagerIndex]);
		if (newPricingManager?.id == plan.assignedEmployeeId)
		{
			return;
		}
		string text = null;
		if (newPricingManager == null && plan.originalStorePrices.Count > 0)
		{
			text = "bizman_pricingmanagers_unassign_confirm";
		}
		else if (newPricingManager != null && newPricingManager.GetSkillValue("ba:skill_pricingmanager") < plan.PricingManagerSkillValue)
		{
			text = "bizman_pricingmanagers_lower_skill_confirm";
		}
		if (text != null)
		{
			HudConfirm.Show(null, text, delegate
			{
				ChangePricingManager(plan, newPricingManager, entry);
			}, delegate
			{
				entry.ManagerDropdown.SelectOption(GetDropdownIndex(plan.assignedEmployeeId));
			});
		}
		else
		{
			ChangePricingManager(plan, newPricingManager, entry);
		}
	}

	private void ChangePricingManager(PricingManagerPlan plan, EmployeeInstance newPricingManager, PricingManagersPlanListEntry entry)
	{
		if (newPricingManager == null)
		{
			plan.UnAssignEmployee();
		}
		else
		{
			plan.AssignEmployee(newPricingManager.id);
		}
		SaveGameManager.MarkChange();
		bool flag = newPricingManager != null;
		string managerName = (flag ? newPricingManager.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		entry.SetManager(managerName, flag);
		if (!(_selectedEntry != entry))
		{
			SelectPlan(entry);
		}
	}

	private void UpdateOwnedBusinessesLabel()
	{
		string text = ((_selectedEntry == null) ? null : _selectedEntry.Plan.supervisedNeighborhood);
		if (text.IsNullOrEmpty())
		{
			ownedBusinessesLabel.gameObject.SetActive(value: false);
			return;
		}
		ownedBusinessesLabel.gameObject.SetActive(value: true);
		ownedBusinessesLabel.SetData("bizman_pricingmanagers_owned_businesses".Localize(new
		{
			numberOfBusinesses = GetOwnedBusinessCount(text)
		}));
	}

	private static int GetOwnedBusinessCount(string neighborhood)
	{
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!(buildingRegistration.Neighborhood != neighborhood) && PricingManagerHelper.IsManageableBusiness(buildingRegistration))
			{
				num++;
			}
		}
		return num;
	}

	private static string GetNeighborhoodDisplayName(string neighborhood)
	{
		if (!neighborhood.IsNullOrEmpty())
		{
			return neighborhood.GetLocalization();
		}
		return "common_unassigned".GetLocalization();
	}
}
