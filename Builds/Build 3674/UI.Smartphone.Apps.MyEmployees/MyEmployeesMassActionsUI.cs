using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MyEmployees;

public class MyEmployeesMassActionsUI : MonoBehaviour
{
	[SerializeField]
	private MyEmployees myEmployees;

	public Toggle massActionAllToggle;

	[SerializeField]
	private UI.Elements.Dropdown massActionDropdown;

	[SerializeField]
	private Transform massActionOptionsPanel;

	[SerializeField]
	private TextLocalizationComponent massActionPanelTitle;

	[SerializeField]
	private UI.Elements.Dropdown massActionOptionsDropdown;

	private string[] _massActionsTypes;

	private int _currentMassActionOption;

	private UnityAction<int> _currentMassAction;

	public static List<EmployeeInstance> massActionSelectedEmployees;

	public void Initialize()
	{
		massActionAllToggle.onValueChanged.AddListener(MassActionToggleAll);
		massActionDropdown.SetPlaceholder("myemployees_mass_action_dropdown_placeholder");
		massActionDropdown.onOptionSelected.RemoveAllListeners();
		massActionDropdown.onOptionSelected.AddListener(DoMassAction);
		massActionOptionsDropdown.onOptionSelected.RemoveAllListeners();
		massActionOptionsDropdown.onOptionSelected.AddListener(SelectMassActionOption);
		massActionOptionsDropdown.SetPlaceholder("common_select_option");
	}

	public void OnMassActionPerformed(bool needsReloadingData = false, bool needsReorderingData = false)
	{
		myEmployees.UpdateCurrentEmployeeBox();
		ClosePanel();
		if (needsReorderingData)
		{
			myEmployees.GetCurrentScroller().RefreshActiveCellViews();
			myEmployees.ReorderCurrentScroller();
		}
		else if (needsReloadingData)
		{
			myEmployees.GetCurrentScroller().ReloadData();
		}
		MassActionToggleAll(toggled: false);
		massActionAllToggle.SetIsOnWithoutNotify(value: false);
	}

	private void MassActionToggleAll(bool toggled)
	{
		massActionSelectedEmployees = ((!toggled) ? new List<EmployeeInstance>() : ((myEmployees.CurrentTab == "Candidates") ? SaveGameManager.Current.CandidateEmployeeInstances.ToList() : EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		})));
		massActionSelectedEmployees.RemoveAll((EmployeeInstance x) => (!(myEmployees.CurrentTab == "Candidates")) ? myEmployees.employeeScrollerController.data.All((EmployeeModel cell) => cell.employeeInstance != x) : myEmployees.candidateScrollerController.data.All((CandidateModel cell) => cell.employeeInstance != x));
		myEmployees.GetCurrentScroller().RefreshActiveCellViews();
	}

	public void ToggleSelectedEmployee(EmployeeInstance employeeInstance, bool toggled)
	{
		if (toggled && !massActionSelectedEmployees.Contains(employeeInstance))
		{
			massActionSelectedEmployees.Add(employeeInstance);
		}
		else if (!toggled && massActionSelectedEmployees.Contains(employeeInstance))
		{
			massActionSelectedEmployees.Remove(employeeInstance);
		}
		bool isOnWithoutNotify = massActionSelectedEmployees.Count == ((myEmployees.CurrentTab == "Candidates") ? SaveGameManager.Current.CandidateEmployeeInstances.Count : EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}).Count);
		massActionAllToggle.SetIsOnWithoutNotify(isOnWithoutNotify);
	}

	public void ToggleRangeOfEmployees(int firstSelectedIndex, int lastSelectedIndex, bool newState)
	{
		int num = ((firstSelectedIndex <= lastSelectedIndex) ? firstSelectedIndex : lastSelectedIndex);
		int num2 = ((firstSelectedIndex > lastSelectedIndex) ? firstSelectedIndex : lastSelectedIndex);
		for (int i = num; i <= num2; i++)
		{
			EmployeeInstance employeeInstance = ((myEmployees.CurrentTab == "Candidates") ? myEmployees.candidateScrollerController.data[i].employeeInstance : myEmployees.employeeScrollerController.data[i].employeeInstance);
			ToggleSelectedEmployee(employeeInstance, newState);
		}
		myEmployees.GetCurrentScroller().RefreshActiveCellViews();
	}

	public void UpdateMassActionDropdown(string tab)
	{
		_massActionsTypes = EmployeeMassActionHelper.GetMassActionsByTab(tab);
		massActionDropdown.SetOptions(_massActionsTypes.ToList());
	}

	public void ShowMassActionOptionsPanel(string titleKey, List<string> options, UnityAction<int> onConfirm)
	{
		_currentMassActionOption = -1;
		massActionPanelTitle.Key = titleKey;
		massActionOptionsDropdown.SetOptions(options, localize: false);
		_currentMassAction = onConfirm;
		massActionOptionsPanel.gameObject.SetActive(value: true);
	}

	private void SelectMassActionOption(int index)
	{
		_currentMassActionOption = index;
	}

	public void ConfirmMassActionOption()
	{
		_currentMassAction(_currentMassActionOption);
	}

	public void ClosePanel()
	{
		massActionOptionsPanel.gameObject.SetActive(value: false);
	}

	private void DoMassAction(int index)
	{
		if (massActionSelectedEmployees.Count == 0)
		{
			Notifications.ShowError("myemployees_mass_action_no_employees_selected");
		}
		else
		{
			EmployeeMassActionHelper.GetMassAction(_massActionsTypes[index]).Perform();
		}
		massActionDropdown.ResetSelectedOption();
	}

	private void OnDisable()
	{
		ClosePanel();
	}
}
