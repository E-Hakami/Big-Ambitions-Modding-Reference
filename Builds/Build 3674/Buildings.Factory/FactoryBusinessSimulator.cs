using System.Collections.Generic;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using Helpers.BusinessSimulation;
using UnityEngine;

namespace Buildings.Factory;

[CreateAssetMenu(menuName = "BusinessSimulator/Factory")]
public class FactoryBusinessSimulator : BusinessSimulator
{
	private readonly Dictionary<string, CargoInstance> _ingredientsGrabbed = new Dictionary<string, CargoInstance>();

	private readonly List<ItemInstance> _palletShelves = new List<ItemInstance>();

	private readonly Dictionary<string, int> _plannedIngredients = new Dictionary<string, int>();

	private readonly Dictionary<string, int> _shelfCargoDict = new Dictionary<string, int>();

	private readonly List<FactoryWorkstationInstance> _workstations = new List<FactoryWorkstationInstance>();

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		_palletShelves.Clear();
		_workstations.Clear();
		_shelfCargoDict.Clear();
		_plannedIngredients.Clear();
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (value.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				_palletShelves.Add(value);
				if (value.cargoInstances.Count <= 0)
				{
					continue;
				}
				foreach (CargoInstance cargoInstance in value.cargoInstances)
				{
					_shelfCargoDict.SumOrAdd(cargoInstance.itemName, cargoInstance.amount);
				}
			}
			else if (value is FactoryWorkstationInstance factoryWorkstationInstance && !string.IsNullOrEmpty(factoryWorkstationInstance.workstationType) && factoryWorkstationInstance.IsWorkstationValid())
			{
				_workstations.Add(factoryWorkstationInstance);
			}
		}
		_workstations.Sort((FactoryWorkstationInstance a, FactoryWorkstationInstance b) => a.priority.CompareTo(b.priority));
	}

	public override void SimulateCurrentHour()
	{
		foreach (FactoryWorkstationInstance workstation in _workstations)
		{
			if (workstation.SelectedRecipe == null)
			{
				continue;
			}
			EmployeeInstance employeeAtStationAndHour = EmployeeHelper.GetEmployeeAtStationAndHour(buildingRegistration, workstation.id, currentHour);
			if (employeeAtStationAndHour == null || workstation.HasReachedProductionLimit(buildingRegistration, _palletShelves))
			{
				continue;
			}
			int outputAmountFromRecipe = GetOutputAmountFromRecipe(workstation, employeeAtStationAndHour.GetSkillValue("ba:skill_factoryworker"));
			if (outputAmountFromRecipe <= 0)
			{
				continue;
			}
			_ingredientsGrabbed.Clear();
			bool flag = true;
			foreach (KeyValuePair<string, int> plannedIngredient in _plannedIngredients)
			{
				if (!GetIngredientFromShelves(_ingredientsGrabbed, plannedIngredient.Key, plannedIngredient.Value))
				{
					flag = false;
					break;
				}
			}
			if (!flag)
			{
				ReturnIngredientsToShelves(_ingredientsGrabbed);
				RevertPlannedIngredientsFromPlan();
				continue;
			}
			CargoInstance cargoInstance = new CargoInstance(workstation.SelectedRecipe.output.item, outputAmountFromRecipe, 0f);
			int i = 0;
			for (int count = _palletShelves.Count; i < count && !_palletShelves[i].TryToAddToCargo(cargoInstance); i++)
			{
			}
			UpdateFactoryExportsList();
		}
	}

	private void RevertPlannedIngredientsFromPlan()
	{
		foreach (var (key, amount) in _plannedIngredients)
		{
			_shelfCargoDict.SumOrAdd(key, amount);
		}
		_plannedIngredients.Clear();
	}

	private bool GetIngredientFromShelves(Dictionary<string, CargoInstance> ingredientsGrabbed, string ingredientName, int ingredientAmount)
	{
		int num = ingredientAmount;
		int i = 0;
		for (int count = _palletShelves.Count; i < count; i++)
		{
			ItemInstance itemInstance = _palletShelves[i];
			for (int num2 = itemInstance.cargoInstances.Count - 1; num2 >= 0; num2--)
			{
				CargoInstance cargoInstance = itemInstance.cargoInstances[num2];
				if (!(cargoInstance.itemName != ingredientName))
				{
					int num3 = ((num < cargoInstance.amount) ? num : cargoInstance.amount);
					if (num3 > 0)
					{
						itemInstance.ReduceFromCargo(cargoInstance, num3);
						if (ingredientsGrabbed.TryGetValue(ingredientName, out var value))
						{
							value.amount += num3;
						}
						else
						{
							value = new CargoInstance(ingredientName, num3, cargoInstance.pricePerUnit);
							ingredientsGrabbed.Add(ingredientName, value);
						}
						num -= num3;
						if (num <= 0)
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private void ReturnIngredientsToShelves(Dictionary<string, CargoInstance> ingredientsGrabbed)
	{
		foreach (CargoInstance value in ingredientsGrabbed.Values)
		{
			ItemHelper.DeliverCargoToBuilding(value, buildingRegistration, (ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage));
		}
	}

	public override void OnTimeMachineEnd(BuildingRegistration registration)
	{
	}

	private void UpdateFactoryExportsList()
	{
		List<FactoryExport> factoryExports = buildingRegistration.factoryExports;
		foreach (CargoInstance value in _ingredientsGrabbed.Values)
		{
			float num = (float)value.amount * value.pricePerUnit;
			if (num <= 0f)
			{
				continue;
			}
			FactoryExport factoryExport = null;
			int i = 0;
			for (int count = factoryExports.Count; i < count; i++)
			{
				if (!(factoryExports[i].itemName != value.itemName))
				{
					factoryExport = factoryExports[i];
					break;
				}
			}
			if (factoryExport != null)
			{
				factoryExport.totalIngredientsCost += num;
				continue;
			}
			factoryExports.Add(new FactoryExport
			{
				itemName = value.itemName,
				amount = 0,
				totalIngredientsCost = num,
				totalPrice = 0f
			});
		}
	}

	private int GetOutputAmountFromRecipe(FactoryWorkstationInstance workstation, float employeeSkillValue)
	{
		if (_shelfCargoDict.Count == 0)
		{
			return 0;
		}
		if (!TryComputeIngredientRatio(workstation, out var ratio))
		{
			return 0;
		}
		Recipe selectedRecipe = workstation.SelectedRecipe;
		int scaledOutputAmount = selectedRecipe.GetScaledOutputAmount(employeeSkillValue);
		int producedItems = Mathf.RoundToInt((float)scaledOutputAmount * Mathf.Min(1f, ratio));
		if (producedItems < 1)
		{
			return 0;
		}
		if (workstation.produceUpTo && !TryApplyProduceUpToCap(workstation, scaledOutputAmount, ref ratio, ref producedItems))
		{
			return 0;
		}
		string item = selectedRecipe.output.item;
		if (!TryApplyOutputCapacityCap(workstation, scaledOutputAmount, item, ref ratio, ref producedItems))
		{
			return 0;
		}
		PlanIngredientsForRatio(workstation, ratio);
		_shelfCargoDict.SumOrAdd(item, producedItems);
		SaveGameManager.Current.achievementsData.goodsProducedInFactories += producedItems;
		return producedItems;
	}

	private bool TryComputeIngredientRatio(FactoryWorkstationInstance workstation, out float ratio)
	{
		List<RecipeItem> ingredients = workstation.SelectedRecipe.ingredients;
		ratio = 1f;
		int i = 0;
		for (int count = ingredients.Count; i < count; i++)
		{
			RecipeItem recipeItem = ingredients[i];
			if (!_shelfCargoDict.TryGetValue(recipeItem.item, out var value))
			{
				return false;
			}
			float num = (float)value / (float)recipeItem.amount;
			if (num <= 0f)
			{
				return false;
			}
			float num2 = ((num > 1f) ? 1f : num);
			if (!(num2 >= ratio))
			{
				ratio = num2;
				if (ratio <= 0f)
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool TryApplyProduceUpToCap(FactoryWorkstationInstance workstation, float scaledOutputAmount, ref float ratio, ref int producedItems)
	{
		string item = workstation.SelectedRecipe.output.item;
		_shelfCargoDict.TryGetValue(item, out var value);
		int num = workstation.produceUpToValue - value;
		if (num <= 0)
		{
			return false;
		}
		float num2 = (float)num / scaledOutputAmount;
		if (num2 < ratio)
		{
			ratio = num2;
		}
		producedItems = Mathf.RoundToInt(scaledOutputAmount * ((ratio < 1f) ? ratio : 1f));
		return producedItems >= 1;
	}

	private bool TryApplyOutputCapacityCap(FactoryWorkstationInstance workstation, float scaledOutputAmount, string producedItemName, ref float ratio, ref int producedItems)
	{
		PlanIngredientsForRatio(workstation, ratio);
		int availableSpaceForProduced = GetAvailableSpaceForProduced(producedItemName);
		RevertPlannedIngredients(workstation, ratio);
		if (availableSpaceForProduced >= producedItems)
		{
			return true;
		}
		for (int num = availableSpaceForProduced; num > 0; num--)
		{
			float num2 = (float)num / scaledOutputAmount;
			PlanIngredientsForRatio(workstation, num2);
			bool num3 = GetAvailableSpaceForProduced(producedItemName) >= num;
			RevertPlannedIngredients(workstation, num2);
			if (num3)
			{
				producedItems = num;
				ratio = num2;
				return true;
			}
		}
		return false;
	}

	private void PlanIngredientsForRatio(FactoryWorkstationInstance workstation, float ratio)
	{
		_plannedIngredients.Clear();
		List<RecipeItem> ingredients = workstation.SelectedRecipe.ingredients;
		int i = 0;
		for (int count = ingredients.Count; i < count; i++)
		{
			RecipeItem recipeItem = ingredients[i];
			int num = Mathf.RoundToInt(ratio * (float)recipeItem.amount);
			if (num > 0)
			{
				_shelfCargoDict.ReduceOrRemove(recipeItem.item, num);
				_plannedIngredients.SumOrAdd(recipeItem.item, num);
			}
		}
	}

	private void RevertPlannedIngredients(FactoryWorkstationInstance workstation, float ratio)
	{
		List<RecipeItem> ingredients = workstation.SelectedRecipe.ingredients;
		int i = 0;
		for (int count = ingredients.Count; i < count; i++)
		{
			RecipeItem recipeItem = ingredients[i];
			int num = Mathf.RoundToInt(ratio * (float)recipeItem.amount);
			if (num > 0)
			{
				_shelfCargoDict.SumOrAdd(recipeItem.item, num);
				_plannedIngredients.ReduceOrRemove(recipeItem.item, num);
			}
		}
	}

	private int GetAvailableSpaceForProduced(string producedItemName)
	{
		Item byName = ItemsGetter.GetByName(producedItemName);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int i = 0;
		for (int count = _palletShelves.Count; i < count; i++)
		{
			num2 += _palletShelves[i].ItemCached.cargoCapacity;
		}
		foreach (var (text2, num5) in _shelfCargoDict)
		{
			Item byName2 = ItemsGetter.GetByName(text2);
			num += Mathf.CeilToInt((float)num5 / (float)byName2.boxSize);
			if (!(text2 != producedItemName) && num5 > 0)
			{
				int num6 = Mathf.CeilToInt((float)num5 / (float)byName2.boxSize);
				num3 += byName2.boxSize * num6 - num5;
			}
		}
		return (num2 - num) * byName.boxSize + num3;
	}
}
