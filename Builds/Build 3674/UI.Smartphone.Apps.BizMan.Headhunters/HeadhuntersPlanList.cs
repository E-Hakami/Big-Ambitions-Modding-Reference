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

namespace UI.Smartphone.Apps.BizMan.Headhunters;

public class HeadhuntersPlanList : MonoBehaviour
{
	[SerializeField]
	private HeadhunterPlanUI planUI;

	[SerializeField]
	private Transform entryTemplate;

	[SerializeField]
	private Transform buttonEntry;

	[SerializeField]
	private ReorderableList reorderableList;

	private Transform _selectedEntry;

	private List<EmployeeInstance> _headhunters;

	private void Awake()
	{
		HeadhunterPlanUI headhunterPlanUI = planUI;
		headhunterPlanUI.onHrManagerPlansChanged = (Action<int, int>)Delegate.Combine(headhunterPlanUI.onHrManagerPlansChanged, new Action<int, int>(UpdateSelectedPlanHrManagersInfo));
		NoManagerAssignedPopUp noManagerAssignedPopUp = planUI.noManagerAssignedPopUp;
		noManagerAssignedPopUp.onDeletePlan = (Action)Delegate.Combine(noManagerAssignedPopUp.onDeletePlan, new Action(RefreshManagersList));
	}

	private void OnEnable()
	{
		RefreshData();
		reorderableList.OnItemReordered += UpdatePlanOrder;
	}

	private void OnDisable()
	{
		entryTemplate.ResetTemplate();
		reorderableList.OnItemReordered -= UpdatePlanOrder;
	}

	private static void UpdatePlanOrder(int fromIndex, int toIndex)
	{
		List<HeadhunterPlan> assignedPlansForHeadquarters = HeadhunterHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address);
		HeadhunterPlan item = assignedPlansForHeadquarters[fromIndex];
		HeadhunterPlan item2 = assignedPlansForHeadquarters[toIndex];
		List<HeadhunterPlan> headhunterPlans = SaveGameManager.Current.headhunterPlans;
		headhunterPlans.Remove(item);
		int num = headhunterPlans.IndexOf(item2);
		headhunterPlans.Insert((fromIndex < toIndex) ? (num + 1) : num, item);
		SaveGameManager.MarkChange();
	}

	private void RefreshData()
	{
		RefreshManagersList();
	}

	private void RefreshManagersList()
	{
		planUI.Hide();
		_selectedEntry = null;
		entryTemplate.ResetTemplate();
		foreach (HeadhunterPlan assignedPlansForHeadquarter in HeadhunterHelper.GetAssignedPlansForHeadquarters(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address))
		{
			SetUpPlanEntry(assignedPlansForHeadquarter);
		}
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
	}

	private Transform SetUpPlanEntry(HeadhunterPlan plan)
	{
		EmployeeInstance headhunterInstance = plan.HeadhunterInstance;
		Transform entry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.parent);
		entry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.353f);
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectPlan(entry, plan);
		});
		entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(headhunterInstance == null);
		TextMeshProUGUI labelByName = entry.GetLabelByName("ManagerName");
		labelByName.margin = new Vector4((headhunterInstance == null) ? 50 : 0, 0f, 0f, 0f);
		labelByName.text = ((headhunterInstance != null) ? headhunterInstance.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		UI.Elements.Dropdown managerDropdown = entry.GetDropDownByName("ManagerDropdown");
		managerDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			OnChangedHeadhunter(plan, index - 1, entry, managerDropdown);
		});
		entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedHrPlans}/{plan.MaxHrPlansThatCanBeAssigned}";
		entry.gameObject.SetActive(value: true);
		return entry;
	}

	private void UpdateSelectedPlanHrManagersInfo(int currentAmountOfHrPlans, int maxHrPlans)
	{
		_selectedEntry.GetLabelByName("Info").text = $"{currentAmountOfHrPlans}/{maxHrPlans}";
	}

	private void UpdateHeadhunterDropdownForPlan(HeadhunterPlan plan, Transform selectedEntry)
	{
		_headhunters = SaveGameManager.Current.EmployeeInstances.FindAll((EmployeeInstance x) => x.assignedAddress == InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address && x.HasSkill("ba:skill_headhunter") && !SaveGameManager.Current.headhunterPlans.Exists((HeadhunterPlan y) => y.id != plan.id && y.assignedEmployeeId == x.id) && x.IsAssignedToAnyWorkShift());
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_headhunters.Select((EmployeeInstance x) => x.GetEmployeeNameWithInfo()).ToList());
		int selectedOption = _headhunters.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1;
		selectedEntry.Find("ManagerDropdown").GetComponent<UI.Elements.Dropdown>().SetOptions(list, localize: false, selectedOption);
		planUI.noManagerAssignedPopUp.SetUpEmployeeDropdown(_headhunters);
	}

	private void OnChangedHeadhunter(HeadhunterPlan plan, int headhunterIndex, Transform entry, UI.Elements.Dropdown dropdown)
	{
		string text = ((headhunterIndex == -1) ? null : _headhunters[headhunterIndex].id);
		if (text == plan.assignedEmployeeId)
		{
			return;
		}
		if (text != null)
		{
			EmployeeInstance newHeadhunter = EmployeeHelper.GetEmployeeById(text);
			float skillValue = newHeadhunter.GetSkillValue("ba:skill_headhunter");
			int newHeadhunterMaxHrManagers = skillValue.CalculateMaxHrPlans();
			bool flag = newHeadhunterMaxHrManagers >= plan.assignedHrPlans.Count((string x) => !string.IsNullOrEmpty(x));
			int num = skillValue.CalculateMaxDealBreakersPoints();
			int num2 = plan.dealBreakerTypes.Sum((string x) => HeadhunterHelper.GetData(x).recruitmentPointCost);
			bool flag2 = num >= num2;
			if (!flag && !flag2)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_headhunter_fewer_points_and_cant_handle_hrmanagers_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					for (int i = newHeadhunterMaxHrManagers; i < plan.assignedHrPlans.Length; i++)
					{
						plan.assignedHrPlans[i] = null;
					}
					plan.dealBreakerTypes.Clear();
					ChangeHeadhunter(plan, _headhunters[headhunterIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
					entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedHrPlans}/{plan.MaxHrPlansThatCanBeAssigned}";
				}, delegate
				{
					dropdown.SelectOption(_headhunters.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
			if (!flag2)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_headhunter_fewer_points_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					plan.dealBreakerTypes.Clear();
					ChangeHeadhunter(plan, _headhunters[headhunterIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
					entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedHrPlans}/{plan.MaxHrPlansThatCanBeAssigned}";
				}, delegate
				{
					dropdown.SelectOption(_headhunters.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
			if (!flag)
			{
				LanguageChangeEventDataHolder bodyData = "bizman_headhunter_cant_handle_hrmanagers_confirm".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					plan.assignedEmployeeId = newHeadhunter.id;
					for (int i = plan.MaxHrPlansThatCanBeAssigned; i < plan.assignedHrPlans.Length; i++)
					{
						plan.assignedHrPlans[i] = null;
					}
					ChangeHeadhunter(plan, _headhunters[headhunterIndex], entry);
					entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: false);
					entry.GetLabelByName("ManagerName").margin = new Vector4(0f, 0f, 0f, 0f);
					entry.GetLabelByName("Info").text = $"{plan.NumberOfAssignedHrPlans}/{plan.MaxHrPlansThatCanBeAssigned}";
				}, delegate
				{
					dropdown.SelectOption(_headhunters.FindIndex((EmployeeInstance x) => x.id == plan.assignedEmployeeId) + 1);
				});
				return;
			}
		}
		ChangeHeadhunter(plan, (headhunterIndex == -1) ? null : _headhunters[headhunterIndex], entry);
		entry.Find("ManagerName/UnassignedManagerIcon").gameObject.SetActive(value: true);
		entry.GetLabelByName("ManagerName").margin = new Vector4(50f, 0f, 0f, 0f);
	}

	private void ChangeHeadhunter(HeadhunterPlan plan, EmployeeInstance newHeadhunter, Transform entry)
	{
		plan.assignedEmployeeId = newHeadhunter?.id;
		entry.GetLabelByName("ManagerName").text = ((newHeadhunter != null) ? newHeadhunter.GetEmployeeNameWithInfo() : "common_unassigned".GetLocalization());
		UpdateSelectedPlanHrManagersInfo(plan.NumberOfAssignedHrPlans, plan.MaxHrPlansThatCanBeAssigned);
		if (!(_selectedEntry != entry))
		{
			SelectPlan(entry, plan);
		}
	}

	private void SelectPlan(Transform entry, HeadhunterPlan plan)
	{
		UnselectCurrentAgent();
		UpdateHeadhunterDropdownForPlan(plan, entry);
		_selectedEntry = entry;
		_selectedEntry.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
		_selectedEntry.Find("ManagerName").gameObject.SetActive(value: false);
		_selectedEntry.Find("ManagerDropdown").gameObject.SetActive(value: true);
		_selectedEntry.GetLabelByName("Info").color = Colors.Midnight;
		planUI.Hide();
		planUI.LoadPlan(plan);
		planUI.noManagerAssignedPopUp.onChooseEmployee = delegate(int chosenEmployeeIndex)
		{
			OnChangedHeadhunter(plan, chosenEmployeeIndex, entry, planUI.noManagerAssignedPopUp.employeeDropdown);
		};
	}

	public void AddPlan()
	{
		HeadhunterPlan headhunterPlan = new HeadhunterPlan
		{
			headquartersAddress = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address
		};
		SaveGameManager.Current.headhunterPlans.Add(headhunterPlan);
		Transform entry = SetUpPlanEntry(headhunterPlan);
		buttonEntry.SetAsLastSibling();
		reorderableList.Reinitialize();
		SelectPlan(entry, headhunterPlan);
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
