using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using UI;
using UI.Elements;
using UnityEngine;

public class SetUpUniformsWindow : MonoBehaviour
{
	[SerializeField]
	private GameObject content;

	[SerializeField]
	private GameObject noUniformsWarning;

	[SerializeField]
	private Dropdown skillNameDropdown;

	[SerializeField]
	private Dropdown uniformDropdown;

	private List<string> _skillNames = new List<string>();

	private BuildingRegistration _registration;

	private readonly HashSet<string> _skillNameHashSet = new HashSet<string>();

	private static List<EmployeePreset> EmployeePresets => SaveGameManager.Current.employeePresets;

	public void Init(BuildingRegistration registration)
	{
		if (!ShouldShowWindow(registration))
		{
			content.SetActive(value: false);
			noUniformsWarning.SetActive(value: true);
			return;
		}
		content.SetActive(value: true);
		noUniformsWarning.SetActive(value: false);
		_registration = registration;
		InitSkillNameDropdown();
		InitUniformDropdown();
	}

	private void InitSkillNameDropdown()
	{
		SetSkillNameDropdownOptions();
		skillNameDropdown.onOptionSelected.RemoveAllListeners();
		skillNameDropdown.onOptionSelected.AddListener(OnSkillNameDropdownOptionSelected);
	}

	private void InitUniformDropdown()
	{
		SetUniformDropdownOptions(skillNameDropdown.SelectedOptionIndex);
		uniformDropdown.onOptionSelected.RemoveAllListeners();
		uniformDropdown.onOptionSelected.AddListener(OnUniformDropdownOptionSelected);
	}

	private void SetSkillNameDropdownOptions()
	{
		_skillNames = BusinessTypeHelper.GetData(_registration).employeePrimarySkills.ToList();
		_skillNameHashSet.Clear();
		foreach (string skillName in _skillNames)
		{
			_skillNameHashSet.Add(skillName);
		}
		string[] requiredBuildingSkills = BuildingTypeHelper.GetData(_registration).requiredBuildingSkills;
		foreach (string item in requiredBuildingSkills)
		{
			if (_skillNameHashSet.Add(item))
			{
				_skillNames.Add(item);
			}
		}
		skillNameDropdown.SetOptions(_skillNames, localize: true, 0);
	}

	private void SetUniformDropdownOptions(int skillNameIndex)
	{
		string key = _skillNames[skillNameIndex];
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(EmployeePresets.Select((EmployeePreset x) => x.name));
		int num = -1;
		if (_registration.uniformsBySkill.TryGetValue(key, out var selectedUniformId))
		{
			num = EmployeePresets.FindIndex((EmployeePreset x) => x.id == selectedUniformId);
		}
		uniformDropdown.SetOptions(list, localize: false, num + 1);
	}

	private void OnSkillNameDropdownOptionSelected(int optionIndex)
	{
		SetUniformDropdownOptions(optionIndex);
	}

	private void OnUniformDropdownOptionSelected(int optionIndex)
	{
		string text = _skillNames[skillNameDropdown.SelectedOptionIndex];
		string text2 = null;
		if (optionIndex <= 0)
		{
			_registration.uniformsBySkill.Remove(text);
		}
		else
		{
			text2 = EmployeePresets[optionIndex - 1].id;
			_registration.uniformsBySkill[text] = text2;
		}
		CustomerDemandHelper.ReloadCachedFulfilled(_registration.Address);
		InstanceBehavior<BuildingManager>.Instance.onUniformChanged.Invoke(text, text2);
		GameEvent.Invoke("ba:gameevent_employeeuniformassigned");
	}

	public void OnConfigureUniformPresetsButtonClick()
	{
		string initialPresetId = null;
		if (uniformDropdown.SelectedOptionIndex > 0)
		{
			initialPresetId = EmployeePresets[uniformDropdown.SelectedOptionIndex - 1].id;
		}
		InstanceBehavior<UIs>.Instance.employeePresetCustomizer.Show(initialPresetId, Gender.Male, delegate
		{
			SetUniformDropdownOptions(skillNameDropdown.SelectedOptionIndex);
		});
	}

	private static bool ShouldShowWindow(BuildingRegistration registration)
	{
		return registration.itemInstances.Values.Any((ItemInstance x) => ItemsGetter.GetByName(x.itemName).HasTag(TagRef.Itemtag.isuniformlocker));
	}
}
