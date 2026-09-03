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
using UI.Smartphone.Apps.BizMan.PurchasingAgent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class PurchasingAgentsPlanList : MonoBehaviour
{
	public Transform entryTemplate;

	public PurchasingAgentPlanUI purchasingAgentPlanUISettings;

	[SerializeField]
	private GameObject noPartnershipsWarning;

	[SerializeField]
	private ReorderableList reorderableList;

	private Transform _selectedEntry;

	private List<EmployeeInstance> _purchasingAgents;

	private void Awake()
	{
		NoManagerAssignedPopUp noManagerAssignedPopUp = purchasingAgentPlanUISettings.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Combine(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshManagersList));
		purchasingAgentPlanUISettings.Initialize();
	}

	private void OnEnable()
	{
		reorderableList.OnItemReordered += OnPlanReordered;
		RefreshData();
	}

	private void OnDisable()
	{
		reorderableList.OnItemReordered -= OnPlanReordered;
	}

	private static void OnPlanReordered(int fromIndex, int toIndex)
	{
		List<ImportPartnership> assignedPlansForHeadquarters = PurchasingAgentHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		ImportPartnership item = assignedPlansForHeadquarters[fromIndex];
		ImportPartnership item2 = assignedPlansForHeadquarters[toIndex];
		List<ImportPartnership> importPartnerships = SaveGameManager.Current.importPartnerships;
		importPartnerships.Remove(item);
		int num = importPartnerships.IndexOf(item2);
		importPartnerships.Insert((fromIndex < toIndex) ? (num + 1) : num, item);
		SaveGameManager.MarkChange();
	}

	public void RefreshData()
	{
		purchasingAgentPlanUISettings.Hide();
		RefreshManagersList();
	}

	private void RefreshManagersList()
	{
		purchasingAgentPlanUISettings.Hide();
		_selectedEntry = null;
		entryTemplate.ResetTemplate();
		List<ImportPartnership> assignedPlansForHeadquarters = PurchasingAgentHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		foreach (ImportPartnership item in assignedPlansForHeadquarters)
		{
			SetUpPlanEntry(item);
		}
		noPartnershipsWarning.SetActive(assignedPlansForHeadquarters.Count == 0);
		reorderableList.Reinitialize();
	}

	private void SetUpPlanEntry(ImportPartnership plan)
	{
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(plan.employeeInstanceId);
		Transform entry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.parent);
		entry.name = plan.id;
		entry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.353f);
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectPlan(entry, plan);
		});
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(plan.importAddress);
		entry.GetLanguageChangeEventByName("ContractName").TextContainer.text = buildingRegistration.BusinessName;
		entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(employeeById == null);
		TextMeshProUGUI labelByName = entry.GetLabelByName("ManagerName");
		labelByName.margin = new Vector4((employeeById == null) ? 50 : 0, 0f, 0f, 0f);
		labelByName.text = ((employeeById != null) ? employeeById.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		UI.Elements.Dropdown managerDropdown = entry.GetDropDownByName("ManagerDropdown");
		managerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			OnChangedPurchasingAgent(plan, index - 1, entry, managerDropdown);
		});
		entry.gameObject.SetActive(value: true);
	}

	private void UpdatePurchasingAgentDropdownForPlan(ImportPartnership plan, Transform selectedEntry)
	{
		_purchasingAgents = SaveGameManager.Current.EmployeeInstances.FindAll((EmployeeInstance x) => x.assignedAddress == InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address && x.HasSkill("ba:skill_purchasingagent") && !SaveGameManager.Current.importPartnerships.Exists((ImportPartnership y) => y.id != plan.id && y.employeeInstanceId == x.id) && x.IsAssignedToAnyWorkShift());
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_purchasingAgents.Select((EmployeeInstance x) => x.GetEmployeeNameWithInfo()).ToList());
		int selectedOption = _purchasingAgents.FindIndex((EmployeeInstance x) => x.id == plan.employeeInstanceId) + 1;
		UI.Elements.Dropdown component = selectedEntry.Find("ManagerDropdown").GetComponent<UI.Elements.Dropdown>();
		component.SetOptions(list, localize: false, selectedOption);
		component.SetInteractable(DeliveryHelper.CanModifyContract(plan.nextDeliveryDay));
		purchasingAgentPlanUISettings.noManagerAssignedPopUp.SetUpEmployeeDropdown(_purchasingAgents);
		purchasingAgentPlanUISettings.noManagerAssignedPopUp.employeeDropdown.SetInteractable(DeliveryHelper.CanModifyContract(plan.nextDeliveryDay));
	}

	private void OnChangedPurchasingAgent(ImportPartnership plan, int purchasingAgentIndex, Transform entry, UI.Elements.Dropdown dropdown)
	{
		string text = ((purchasingAgentIndex == -1) ? null : _purchasingAgents[purchasingAgentIndex].id);
		if (text == plan.employeeInstanceId)
		{
			return;
		}
		if (text != null)
		{
			LanguageChangeEventDataHolder bodyData = "bizman_purchasingagent_change_confirm".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				ChangePurchasingAgent(plan, _purchasingAgents[purchasingAgentIndex], entry);
				entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
				entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
			}, delegate
			{
				dropdown.SelectOption(_purchasingAgents.FindIndex((EmployeeInstance x) => x.id == plan.employeeInstanceId) + 1);
			});
		}
		else
		{
			ChangePurchasingAgent(plan, (purchasingAgentIndex == -1) ? null : _purchasingAgents[purchasingAgentIndex], entry);
			entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: true);
			entry.GetLabelByName("ManagerName").margin = new Vector4(50f, 0f, 0f, 0f);
		}
	}

	private void ChangePurchasingAgent(ImportPartnership plan, EmployeeInstance newPurchasingAgent, Transform entry)
	{
		if (newPurchasingAgent == null)
		{
			plan.UnAssignEmployee();
		}
		else
		{
			plan.employeeInstanceId = newPurchasingAgent.id;
		}
		SaveGameManager.MarkChange();
		entry.GetLabelByName("ManagerName").text = ((newPurchasingAgent != null) ? newPurchasingAgent.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		if (!(_selectedEntry != entry))
		{
			purchasingAgentPlanUISettings.CancelOrder();
			SelectPlan(entry, plan);
		}
	}

	public void SelectPlan(Transform entry, ImportPartnership plan)
	{
		UnselectCurrentAgent();
		entry = (entry ? entry : entryTemplate.parent.Find(plan.id));
		if ((bool)entry)
		{
			UpdatePurchasingAgentDropdownForPlan(plan, entry);
			_selectedEntry = entry;
			SetEntryHighlight(_selectedEntry, selected: true);
			_selectedEntry.Find("ManagerName").gameObject.SetActive(value: false);
			_selectedEntry.Find("ManagerDropdown").gameObject.SetActive(value: true);
			_selectedEntry.GetLabelByName("ContractName").color = Colors.Midnight;
			purchasingAgentPlanUISettings.LoadPlan(plan);
			purchasingAgentPlanUISettings.noManagerAssignedPopUp.onChooseEmployee = delegate(int chosenEmployeeIndex)
			{
				OnChangedPurchasingAgent(plan, chosenEmployeeIndex, entry, purchasingAgentPlanUISettings.noManagerAssignedPopUp.employeeDropdown);
			};
		}
	}

	private void UnselectCurrentAgent()
	{
		if (!(_selectedEntry == null))
		{
			SetEntryHighlight(_selectedEntry, selected: false);
			_selectedEntry.Find("ManagerName").gameObject.SetActive(value: true);
			_selectedEntry.Find("ManagerDropdown").gameObject.SetActive(value: false);
			_selectedEntry.GetLabelByName("ContractName").color = Color.white;
			_selectedEntry = null;
		}
	}

	private static void SetEntryHighlight(Transform entry, bool selected)
	{
		Image component = entry.GetComponent<Image>();
		entry.GetComponent<Button>().targetGraphic = (selected ? null : component);
		component.color = new Color(1f, 1f, 1f, selected ? 1f : 0.353f);
	}
}
