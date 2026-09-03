using System;
using System.Collections.Generic;
using System.Text;
using BaTable;
using BigAmbitions.Characters.Skills;
using BigAmbitions.InputSystem;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI;
using UI.Elements;
using UI.Smartphone.Apps.MyEmployees;
using UnityEngine;
using UnityEngine.UI;

public sealed class CandidateCellView : BaTableCellView<CandidateModel>
{
	private static readonly Dictionary<string, (List<BuildingRegistration> Registrations, List<string> Options)> BusinessOptionsByEmployeeSetup = new Dictionary<string, (List<BuildingRegistration>, List<string>)>();

	public Toggle massActionToggle;

	public TextMeshProUGUI employeeName;

	public TextMeshProUGUI hourlyWage;

	public TextLocalizationComponent primarySkill;

	public TextMeshProUGUI schedule;

	public TextLocalizationComponent timeRemaining;

	public UI.Elements.Dropdown assignBusiness;

	private EmployeeInstance _employeeInstance;

	private List<BuildingRegistration> _buildingRegistrations;

	public override void SetData(CandidateModel data)
	{
		_employeeInstance = data.employeeInstance;
		massActionToggle.SetIsOnWithoutNotify(MyEmployeesMassActionsUI.massActionSelectedEmployees.Contains(_employeeInstance));
		massActionToggle.onValueChanged.RemoveAllListeners();
		massActionToggle.onValueChanged.AddListener(delegate(bool toggled)
		{
			if (PlayerAction.SelectMultipleElements.Pressing() && InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex != -1)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ToggleRangeOfEmployees(InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex, dataIndex, toggled);
			}
			else
			{
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.massActionsUI.ToggleSelectedEmployee(_employeeInstance, toggled);
			}
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.lastSelectedEmployeeIndex = dataIndex;
		});
		employeeName.text = data.employeeName;
		hourlyWage.text = data.hourlyWage.ToCurrencyFormat();
		primarySkill.Arguments = new
		{
			skillName = data.primarySkillName,
			percentage = data.primarySkillPercentage
		};
		schedule.text = data.schedule;
		if (data.hoursUntilExpiring < 24)
		{
			timeRemaining.SetData("common_hours".Localize(new
			{
				hours = data.hoursUntilExpiring
			}));
		}
		else
		{
			int value = Mathf.RoundToInt((float)data.hoursUntilExpiring / 24f);
			timeRemaining.SetData("common_days".Localize(new { value }));
		}
		assignBusiness.onOptionSelected.RemoveListener(AssignBusiness);
		assignBusiness.onOptionSelected.AddListener(AssignBusiness);
		List<string> newOptions;
		(_buildingRegistrations, newOptions) = GetBusinessOptions();
		if (_employeeInstance.IsAssignedToAnyBusiness())
		{
			assignBusiness.SetOptions(newOptions, localize: false, _buildingRegistrations.FindIndex((BuildingRegistration x) => x.Address == _employeeInstance.assignedAddress) + 1);
		}
		else
		{
			assignBusiness.SetOptions(newOptions, localize: false, 0);
		}
	}

	public static void ClearBusinessOptionsCache()
	{
		BusinessOptionsByEmployeeSetup.Clear();
	}

	private (List<BuildingRegistration> Registrations, List<string> Options) GetBusinessOptions()
	{
		string businessOptionsKey = GetBusinessOptionsKey();
		if (BusinessOptionsByEmployeeSetup.TryGetValue(businessOptionsKey, out (List<BuildingRegistration>, List<string>) value))
		{
			return value;
		}
		List<BuildingRegistration> validBuildingRegistrations = GetValidBuildingRegistrations();
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		foreach (BuildingRegistration item in validBuildingRegistrations)
		{
			list.Add(item.GetDisplayName());
		}
		value = (validBuildingRegistrations, list);
		BusinessOptionsByEmployeeSetup.Add(businessOptionsKey, value);
		return value;
	}

	private string GetBusinessOptionsKey()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_employeeInstance.assignedAddress);
		foreach (Skill skill in _employeeInstance.characterData.skills)
		{
			stringBuilder.Append('|');
			stringBuilder.Append(skill.name);
		}
		return stringBuilder.ToString();
	}

	private List<BuildingRegistration> GetValidBuildingRegistrations()
	{
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (IsValidBuildingRegistration(buildingRegistration))
			{
				list.Add(buildingRegistration);
			}
		}
		list.Sort((BuildingRegistration a, BuildingRegistration b) => string.Compare(a.GetDisplayName(), b.GetDisplayName(), StringComparison.Ordinal));
		return list;
	}

	private bool IsValidBuildingRegistration(BuildingRegistration buildingRegistration)
	{
		if (!buildingRegistration.RentedByPlayer)
		{
			return false;
		}
		string buildingType = buildingRegistration.GetBuildingType();
		if (buildingType == "ba:buildingtype_warehouse" && _employeeInstance.HasSkill("ba:skill_deliverydriver"))
		{
			return true;
		}
		if (buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		if (buildingRegistration.Address == _employeeInstance.assignedAddress)
		{
			return true;
		}
		if (BuildingTypeHelper.GetData(buildingType).NeedsCleaning && _employeeInstance.HasSkill("ba:skill_cleaning"))
		{
			return true;
		}
		BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
		if (!data)
		{
			return false;
		}
		if (data.HasTag(TagRef.Businesstag.allowtheft) && _employeeInstance.HasSkill("ba:skill_securityguard"))
		{
			return true;
		}
		string[] employeePrimarySkills = data.employeePrimarySkills;
		foreach (string skill in employeePrimarySkills)
		{
			if (_employeeInstance.HasSkill(skill))
			{
				return true;
			}
		}
		return false;
	}

	public override void RefreshCellView()
	{
		massActionToggle.SetIsOnWithoutNotify(MyEmployeesMassActionsUI.massActionSelectedEmployees.Contains(_employeeInstance));
		assignBusiness.SetVisualSelectedOption(_buildingRegistrations.FindIndex((BuildingRegistration x) => x.Address == _employeeInstance.assignedAddress) + 1);
	}

	private void AssignBusiness(int index)
	{
		Address address = ((index >= 1) ? _buildingRegistrations[index - 1].Address : null);
		if (!(_employeeInstance.assignedAddress == address))
		{
			_employeeInstance.assignedAddress = address;
			if (InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance == _employeeInstance)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.ShowEmployee(_employeeInstance);
			}
		}
	}

	public void Hire()
	{
		MyEmployeesMassActionsUI.massActionSelectedEmployees.Remove(_employeeInstance);
		EmployeeHelper.HireCandidate(_employeeInstance);
		if (InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance == _employeeInstance)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DeselectEmployee();
		}
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.candidateScrollerController.RemoveCandidate(_employeeInstance);
	}

	public void Discard()
	{
		MyEmployeesMassActionsUI.massActionSelectedEmployees.Remove(_employeeInstance);
		EmployeeHelper.DiscardCandidate(_employeeInstance);
		if (InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.SelectedEmployeeInstance == _employeeInstance)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DeselectEmployee();
		}
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.candidateScrollerController.RemoveCandidate(_employeeInstance);
	}

	public void Negotiate()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.NegotiateWithCandidate(_employeeInstance);
	}
}
