using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.SaveSystem;
using Buildings;
using Helpers;
using UI.Elements;
using UnityEngine;

public class UniformEditorTool : MonoBehaviour
{
	[SerializeField]
	private Dropdown businessTypeDropdown;

	[SerializeField]
	private GameObject specificBusinessPanel;

	[SerializeField]
	private Dropdown specificBusinessDropdown;

	[SerializeField]
	private GameObject specificSkillPanel;

	[SerializeField]
	private Dropdown specificSkillDropdown;

	[SerializeField]
	private GameObject removeButton;

	[SerializeField]
	private GameObject createButton;

	[SerializeField]
	private UniformCustomizer uniformCustomizer;

	private BusinessType _selectedBusinessType;

	private List<SpecialService> _selectedTypeSpecificBusinesses;

	private SpecialService _specificBusiness;

	private bool _specificSkillToggled;

	private string _specificSkill;

	private void Start()
	{
		uniformCustomizer.appearanceSetter.gameObject.SetActive(value: false);
		uniformCustomizer.Hide();
		specificBusinessPanel.SetActive(value: false);
		removeButton.SetActive(value: false);
		createButton.SetActive(value: false);
		specificSkillPanel.SetActive(value: false);
		List<string> list = new List<string> { "None" };
		list.AddRange(SkillHelper.AllSkillNames);
		specificSkillDropdown.SetOptions(list, localize: false, 0);
		AddressableLoader.Register<BusinessType>("BusinessTypes", BusinessTypeHelper.OnBusinessTypesLoaded);
		AddressableLoader.ForceLoad();
		businessTypeDropdown.SetPlaceholder("Select business type", localize: false);
		businessTypeDropdown.SetOptions(BusinessTypeHelper.BusinessTypeNames.ToList(), localize: false);
		businessTypeDropdown.onOptionSelected.AddListener(SelectBusinessType);
		specificBusinessDropdown.onOptionSelected.AddListener(SelectSpecificBusiness);
		specificSkillDropdown.onOptionSelected.AddListener(SelectSpecificSkill);
		uniformCustomizer.onUniformSave.AddListener(SetBusinessSODirty);
	}

	private void SelectBusinessType(int index)
	{
		_selectedBusinessType = BusinessTypeHelper.GetData(BusinessTypeHelper.BusinessTypeNames.ElementAt(index));
		_specificSkillToggled = false;
		specificSkillDropdown.ResetSelectedOption(0);
		_specificBusiness = null;
		_selectedTypeSpecificBusinesses = (from x in BuildingHelper.SpecialServiceBuildings
			select x.Value.SpecialService into x
			where x.businessTypeName == _selectedBusinessType.businessTypeName
			select x).ToList();
		List<string> list = new List<string> { "All businesses" };
		list.AddRange(_selectedTypeSpecificBusinesses.Select((SpecialService x) => x.businessName));
		specificBusinessDropdown.SetOptions(list, localize: false, 0);
		specificBusinessPanel.SetActive(value: true);
		specificSkillPanel.SetActive(value: true);
		EmployeePreset employeePreset = _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent);
		if (employeePreset == null)
		{
			createButton.SetActive(value: true);
			removeButton.SetActive(value: false);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: false);
			uniformCustomizer.Hide();
		}
		else
		{
			createButton.SetActive(value: false);
			removeButton.SetActive(value: true);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: true);
			uniformCustomizer.Show(employeePreset);
		}
	}

	private void SelectSpecificBusiness(int index)
	{
		_specificSkillToggled = false;
		specificSkillDropdown.ResetSelectedOption(0);
		EmployeePreset employeePreset;
		if (index == 0)
		{
			_specificBusiness = null;
			employeePreset = _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent);
		}
		else
		{
			_specificBusiness = _selectedTypeSpecificBusinesses[index - 1];
			employeePreset = _specificBusiness.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent);
		}
		if (employeePreset == null)
		{
			createButton.SetActive(value: true);
			removeButton.SetActive(value: false);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: false);
			uniformCustomizer.Hide();
		}
		else
		{
			createButton.SetActive(value: false);
			removeButton.SetActive(value: true);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: true);
			uniformCustomizer.Show(employeePreset);
		}
	}

	private void SelectSpecificSkill(int index)
	{
		EmployeePreset employeePreset;
		if (index == 0)
		{
			_specificSkillToggled = false;
			employeePreset = ((!(_specificBusiness == null)) ? _specificBusiness.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent) : _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent));
		}
		else
		{
			_specificSkillToggled = true;
			_specificSkill = SkillHelper.AllSkillNames.ElementAt(index - 1);
			employeePreset = ((!(_specificBusiness == null)) ? _specificBusiness.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && x.skill == _specificSkill) : _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && x.skill == _specificSkill));
		}
		if (employeePreset == null)
		{
			createButton.SetActive(value: true);
			removeButton.SetActive(value: false);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: false);
			uniformCustomizer.Hide();
		}
		else
		{
			createButton.SetActive(value: false);
			removeButton.SetActive(value: true);
			uniformCustomizer.appearanceSetter.gameObject.SetActive(value: true);
			uniformCustomizer.Show(employeePreset);
		}
	}

	private void SetBusinessSODirty()
	{
	}

	public void Create()
	{
		EmployeePreset employeePreset = BusinessTypeHelper.GetData("ba:businesstype_giftshop").uniforms[0].Copy();
		if (_specificSkillToggled)
		{
			employeePreset.skillDependent = true;
			employeePreset.skill = _specificSkill;
		}
		if (_specificBusiness == null)
		{
			_selectedBusinessType.uniforms.Add(employeePreset);
		}
		else
		{
			_specificBusiness.uniforms.Add(employeePreset);
		}
		uniformCustomizer.appearanceSetter.gameObject.SetActive(value: true);
		uniformCustomizer.Show(employeePreset);
		createButton.SetActive(value: false);
		removeButton.SetActive(value: true);
	}

	public void Delete()
	{
		if (_specificBusiness == null)
		{
			EmployeePreset item = ((!_specificSkillToggled) ? _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent) : _selectedBusinessType.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && x.skill == _specificSkill));
			_selectedBusinessType.uniforms.Remove(item);
		}
		else
		{
			EmployeePreset item = ((!_specificSkillToggled) ? _specificBusiness.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent) : _specificBusiness.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && x.skill == _specificSkill));
			_specificBusiness.uniforms.Remove(item);
		}
		uniformCustomizer.appearanceSetter.gameObject.SetActive(value: false);
		uniformCustomizer.Hide();
		createButton.SetActive(value: true);
		removeButton.SetActive(value: false);
	}
}
