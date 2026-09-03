using System.Collections.Generic;
using System.Linq;
using System.Text;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Factories.Workstations;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using Helpers;
using Items.SpecialItems;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class MachineOverlay : IOverlay
{
	private static readonly StringBuilder IngredientsStringBuilder = new StringBuilder(256);

	[Header("Machine")]
	[SerializeField]
	private UI.Elements.Dropdown workstationDropdown;

	[SerializeField]
	private UI.Elements.Dropdown recipeDropdown;

	[SerializeField]
	private Transform productionMachineTemplate;

	[SerializeField]
	private GameObject productionMachineWarningIcon;

	[SerializeField]
	private TMP_Text recipeIngredientsLabel;

	private FactoryWorkstationInstance _workstationInstance;

	private GameObject _productionMachineMissingImage;

	private GameObject _productionMachineActiveImage;

	private FactoryAssemblyMachineController _assemblyMachineController;

	private void Awake()
	{
		workstationDropdown.onOptionSelected.AddListener(OnWorkstationOptionSelected);
		recipeDropdown.onOptionSelected.AddListener(OnRecipeOptionSelected);
	}

	public override bool IsValid(EntityController entityController)
	{
		return entityController is FactoryAssemblyMachineController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return entityController as FactoryAssemblyMachineController != null;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		_assemblyMachineController = entityController as FactoryAssemblyMachineController;
		if (!(_assemblyMachineController == null))
		{
			_workstationInstance = _assemblyMachineController.WorkstationInstance;
			UpdateWorkstationDropdown();
			UpdateProductionMachineRequirements();
			UpdateRecipeDropdown();
		}
	}

	private void UpdateWorkstationDropdown()
	{
		List<FactoryWorkstation> workstationsByAssemblyMachine = FactoryWorkstationHelper.GetWorkstationsByAssemblyMachine(_workstationInstance.itemName);
		List<string> newOptions = workstationsByAssemblyMachine.Select((FactoryWorkstation x) => x.localizationKey).ToList();
		FactoryWorkstation workstation = FactoryWorkstationHelper.GetWorkstation(_workstationInstance.workstationType);
		int selectedOption = workstationsByAssemblyMachine.IndexOf(workstation);
		workstationDropdown.SetOptions(newOptions, localize: true, selectedOption);
	}

	private void UpdateRecipeDropdown()
	{
		FactoryWorkstation workstation = _workstationInstance.Workstation;
		List<string> newOptions = workstation.supportedRecipes.Select((Recipe x) => x.output.item.GetLocalization()).ToList();
		int selectedOption = ((!string.IsNullOrEmpty(_workstationInstance.selectedRecipeId)) ? workstation.supportedRecipes.IndexOf(_workstationInstance.SelectedRecipe) : 0);
		recipeDropdown.SetOptions(newOptions, localize: false, selectedOption);
		UpdateIngredientsList();
	}

	private void UpdateProductionMachineRequirements()
	{
		productionMachineTemplate.ResetTemplate();
		bool active = false;
		foreach (string requiredProductionMachine in _workstationInstance.Workstation.requiredProductionMachines)
		{
			bool num = _workstationInstance.HasProductionMachine(requiredProductionMachine);
			Transform transform = productionMachineTemplate.CreateElement();
			transform.GetComponentInChildren<TextLocalizationComponent>().Key = requiredProductionMachine;
			if (num)
			{
				transform.Find("Missing").gameObject.SetActive(value: false);
				transform.Find("Active").gameObject.SetActive(value: true);
			}
			else
			{
				transform.Find("Missing").gameObject.SetActive(value: true);
				transform.Find("Active").gameObject.SetActive(value: false);
				active = true;
			}
		}
		productionMachineWarningIcon.SetActive(active);
		LayoutRebuilder.ForceRebuildLayoutImmediate(productionMachineTemplate.parent as RectTransform);
	}

	private void OnWorkstationOptionSelected(int index)
	{
		FactoryWorkstation factoryWorkstation = FactoryWorkstationHelper.GetWorkstationsByAssemblyMachine(_workstationInstance.itemName)[index];
		_workstationInstance.workstationType = factoryWorkstation.workstationType;
		_workstationInstance.selectedRecipeId = factoryWorkstation.supportedRecipes[0].id;
		UpdateRecipeDropdown();
		UpdateProductionMachineRequirements();
		GameEvent.Invoke("ba:gameevent_onfactorymachinerecipechanged");
	}

	private void OnRecipeOptionSelected(int index)
	{
		Recipe recipe = _workstationInstance.Workstation.supportedRecipes[index];
		_workstationInstance.selectedRecipeId = recipe.id;
		UpdateIngredientsList();
		GameEvent.Invoke("ba:gameevent_onfactorymachinerecipechanged");
	}

	private void UpdateIngredientsList()
	{
		List<RecipeItem> ingredients = _workstationInstance.SelectedRecipe.ingredients;
		int count = ingredients.Count;
		if (count == 0)
		{
			recipeIngredientsLabel.SetText(string.Empty);
			return;
		}
		Colors colors = InstanceBehavior<GlobalReferences>.Instance.colors;
		string text = colors.white.ToHex();
		string text2 = colors.red.ToHex();
		IngredientsStringBuilder.Clear();
		for (int i = 0; i < count; i++)
		{
			RecipeItem recipeItem = ingredients[i];
			string value = (((float)GetAmountOfItemsByName(recipeItem.item) >= (float)recipeItem.amount / (float)_workstationInstance.SelectedRecipe.output.amount) ? text : text2);
			if (i > 0)
			{
				IngredientsStringBuilder.Append(", ");
			}
			IngredientsStringBuilder.Append("<color=");
			IngredientsStringBuilder.Append(value);
			IngredientsStringBuilder.Append('>');
			IngredientsStringBuilder.Append(recipeItem.item.GetLocalization());
			IngredientsStringBuilder.Append("</color>");
		}
		recipeIngredientsLabel.SetText(IngredientsStringBuilder.ToString());
	}

	private static int GetAmountOfItemsByName(string itemName)
	{
		int num = 0;
		foreach (ItemInstance value in InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.Values)
		{
			if (!value.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage) || value.cargoInstances.Count <= 0)
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (cargoInstance.itemName == itemName)
				{
					num += cargoInstance.amount;
				}
			}
		}
		return num;
	}
}
