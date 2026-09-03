using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using EnhancedUI.EnhancedScroller;
using Extensions;
using Helpers;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Factory.Table;

public class BizManFactoryWorkstationGroupScrollerController : BaTable<BizManFactoryWorkstationGroupCellView, BizManFactoryWorkstationGroupModel>
{
	private const float CellViewHeight = 100f;

	private const float CellSpacing = 15f;

	private readonly Dictionary<string, List<FactoryWorkstationInstance>> _groupedWorkstations = new Dictionary<string, List<FactoryWorkstationInstance>>();

	private readonly Dictionary<string, int> _itemsOnPalletShelves = new Dictionary<string, int>();

	public readonly Dictionary<int, bool> foldoutStates = new Dictionary<int, bool>();

	public void Load(List<FactoryWorkstationInstance> workstations, BuildingRegistration registration)
	{
		data.Clear();
		foldoutStates.Clear();
		_itemsOnPalletShelves.Clear();
		foreach (ItemInstance value2 in registration.itemInstances.Values)
		{
			if (!value2.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value2.cargoInstances)
			{
				_itemsOnPalletShelves.SumOrAdd(cargoInstance.itemName, cargoInstance.amount);
			}
		}
		_groupedWorkstations.Clear();
		foreach (FactoryWorkstationInstance workstation in workstations)
		{
			if (!_groupedWorkstations.TryGetValue(workstation.CreatedItemName, out var value))
			{
				value = new List<FactoryWorkstationInstance>();
				_groupedWorkstations[workstation.CreatedItemName] = value;
			}
			value.Add(workstation);
		}
		int num = 0;
		foreach (var (text2, list2) in _groupedWorkstations)
		{
			Recipe selectedRecipe = list2[0].SelectedRecipe;
			int producedPerHour = GetProducedPerHour(list2);
			int valueOrDefault = _itemsOnPalletShelves.GetValueOrDefault(text2);
			List<BizManFactoryWorkstationGroupModelIngredient> list3 = new List<BizManFactoryWorkstationGroupModelIngredient>();
			foreach (RecipeItem ingredient in selectedRecipe.ingredients)
			{
				int valueOrDefault2 = _itemsOnPalletShelves.GetValueOrDefault(ingredient.item);
				int runsOutInDays = GetRunsOutInDays(ingredient);
				list3.Add(new BizManFactoryWorkstationGroupModelIngredient(ingredient.item, valueOrDefault2, runsOutInDays));
			}
			int runsOutInDays2 = list3.Min((BizManFactoryWorkstationGroupModelIngredient x) => x.runsOutInDays);
			data.Add(new BizManFactoryWorkstationGroupModel(num, this, text2, producedPerHour, valueOrDefault, runsOutInDays2, list3));
			num++;
		}
		ResetFilters();
		scroller.ReloadData();
	}

	private static int GetProducedPerHour(List<FactoryWorkstationInstance> workstations)
	{
		Recipe selectedRecipe = workstations[0].SelectedRecipe;
		int num = 0;
		foreach (FactoryWorkstationInstance workstation in workstations)
		{
			foreach (WorkShift todayWorkShift in workstation.GetTodayWorkShifts())
			{
				float skillValue = EmployeeHelper.GetEmployeeById(todayWorkShift.employeeId).GetSkillValue("ba:skill_factoryworker");
				int scaledOutputAmount = selectedRecipe.GetScaledOutputAmount(skillValue);
				num += todayWorkShift.DurationInHours * scaledOutputAmount;
			}
		}
		return Mathf.RoundToInt((float)num / 24f);
	}

	private int GetRunsOutInDays(RecipeItem ingredient)
	{
		int valueOrDefault = _itemsOnPalletShelves.GetValueOrDefault(ingredient.item);
		if (valueOrDefault == 0)
		{
			return -1;
		}
		int num = ingredient.amount * 24;
		return Mathf.FloorToInt((float)valueOrDefault / (float)num);
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		if (!foldoutStates.GetValueOrDefault(dataIndex, defaultValue: false))
		{
			return 100f;
		}
		float num = 100f;
		int count = data[dataIndex].ingredients.Count;
		if (count == 0)
		{
			return num;
		}
		num += (float)count * 100f;
		num += (float)(count - 1) * 15f;
		return num + 20f;
	}
}
