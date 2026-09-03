using System;
using System.Collections.Generic;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Factories.Workstations;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Factory;

public class BizManFactoryValidWorkstationPanel : BizManFactoryWorkstationPanel
{
	private const string NoEmployeeAssignedKey = "bizman_factory_no_employee_assigned";

	public Action<bool, int> onProduceUpToChanged;

	[SerializeField]
	private Toggle produceUpToToggle;

	[SerializeField]
	private Toggle produceContinuouslyToggle;

	[SerializeField]
	private AmountSelector upToAmountSelector;

	[SerializeField]
	private TMP_Text employeeNameText;

	[SerializeField]
	private Button manageEmployeeButton;

	[SerializeField]
	private UI.Elements.Dropdown recipeDropdown;

	[SerializeField]
	private Transform stockAndIngredientTemplate;

	[SerializeField]
	private Image workstationImage;

	[SerializeField]
	private TMP_Text outputText;

	private FactoryWorkstation _workstationData;

	private EmployeeInstance _employee;

	protected override void Awake()
	{
		produceUpToToggle.onValueChanged.AddListener(OnToggleProduceUpTo);
		produceContinuouslyToggle.onValueChanged.AddListener(OnToggleProduceContinuously);
		recipeDropdown.onOptionSelected.AddListener(OnRecipeDropdownOptionChanged);
		upToAmountSelector.onAmountUpdate.AddListener(OnUpToAmountChanged);
		base.Awake();
	}

	protected override void OnDestroy()
	{
		produceUpToToggle.onValueChanged.RemoveListener(OnToggleProduceUpTo);
		produceContinuouslyToggle.onValueChanged.RemoveListener(OnToggleProduceContinuously);
		recipeDropdown.onOptionSelected.RemoveListener(OnRecipeDropdownOptionChanged);
		upToAmountSelector.onAmountUpdate.RemoveListener(OnUpToAmountChanged);
	}

	public void OnManageEmployeeClick()
	{
		InstanceBehavior<UIs>.Instance.smartphoneUI.OpenApp(AppName.MyEmployees);
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(EmployeeHelper.GetEmployeeById(_employee.id));
	}

	public override void SetUp(FactoryWorkstationInstance instance, string alias, BuildingRegistration reg)
	{
		base.SetUp(instance, alias, reg);
		_employee = EmployeeHelper.GetEmployeeAtStationAndHour(registration, workstationInstance.id);
		SetUpProductionSettings();
		SetUpEmployee();
		SetUpRecipeDropdown();
		UpdateRecipeTable();
	}

	private void SetUpProductionSettings()
	{
		produceUpToToggle.isOn = workstationInstance.produceUpTo;
		produceContinuouslyToggle.isOn = !workstationInstance.produceUpTo;
		upToAmountSelector.Interactable = workstationInstance.produceUpTo;
		upToAmountSelector.SetAmount(workstationInstance.produceUpToValue);
	}

	private void SetUpEmployee()
	{
		if (_employee != null)
		{
			employeeNameText.SetText(string.Format("{0} ({1}%)", _employee.characterData.name, Mathf.FloorToInt(_employee.GetSkillValue("ba:skill_factoryworker"))));
			manageEmployeeButton.gameObject.SetActive(value: true);
		}
		else
		{
			employeeNameText.SetText("bizman_factory_no_employee_assigned".GetLocalization());
			manageEmployeeButton.gameObject.SetActive(value: false);
		}
	}

	private void SetUpRecipeDropdown()
	{
		List<string> list = new List<string>();
		_workstationData = workstationInstance.Workstation;
		foreach (Recipe supportedRecipe in _workstationData.supportedRecipes)
		{
			list.Add(supportedRecipe.output.item);
		}
		int b = _workstationData.supportedRecipes.FindIndex((Recipe x) => x.id == workstationInstance.selectedRecipeId);
		b = Mathf.Max(0, b);
		recipeDropdown.SetOptions(list, localize: true, b);
	}

	private void UpdateRecipeTable()
	{
		workstationImage.sprite = _workstationData.icon256;
		Recipe selectedRecipe = workstationInstance.SelectedRecipe;
		stockAndIngredientTemplate.ResetTemplate();
		foreach (RecipeItem ingredient in selectedRecipe.ingredients)
		{
			Transform obj = stockAndIngredientTemplate.CreateElement();
			TMP_Text component = obj.Find("StockValue").GetComponent<TMP_Text>();
			int num = BuildingHelper.CountResourcesInPallets(registration.Address, ingredient.item);
			component.SetText(num.ToFormattedNumber());
			Color32 color = ((num == 0) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.black);
			component.color = color;
			obj.Find("IngredientValue").GetComponent<TMP_Text>().SetText($"{ingredient.amount}x {ingredient.item.GetLocalization()}");
		}
		int num2 = ((_employee != null) ? selectedRecipe.GetScaledOutputAmount(_employee.GetSkillValue("ba:skill_factoryworker")) : 0);
		outputText.SetText("common_per_hour".Localize(new
		{
			textValue = $"{num2}x {selectedRecipe.output.item.GetLocalization()}"
		}).ToString());
	}

	private void OnToggleProduceContinuously(bool isOn)
	{
		workstationInstance.produceUpTo = !isOn;
		upToAmountSelector.Interactable = !isOn;
		produceUpToToggle.SetIsOnWithoutNotify(!isOn);
		onProduceUpToChanged?.Invoke(workstationInstance.produceUpTo, workstationInstance.produceUpToValue);
	}

	private void OnToggleProduceUpTo(bool isOn)
	{
		workstationInstance.produceUpTo = isOn;
		upToAmountSelector.Interactable = isOn;
		produceContinuouslyToggle.SetIsOnWithoutNotify(!isOn);
		onProduceUpToChanged?.Invoke(workstationInstance.produceUpTo, workstationInstance.produceUpToValue);
	}

	private void OnUpToAmountChanged(int amount)
	{
		workstationInstance.produceUpToValue = amount;
		onProduceUpToChanged?.Invoke(workstationInstance.produceUpTo, amount);
	}

	private void OnRecipeDropdownOptionChanged(int index)
	{
		workstationInstance.selectedRecipeId = _workstationData.supportedRecipes[index].id;
		isActiveSwapper.IsOn = workstationInstance.IsWorkstationActive(registration);
		UpdateRecipeTable();
		machineList.activeWorkstationTemplate.UpdateRecipe();
	}
}
