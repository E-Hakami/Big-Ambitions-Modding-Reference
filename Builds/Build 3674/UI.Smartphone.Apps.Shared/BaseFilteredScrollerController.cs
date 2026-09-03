using System;
using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;

namespace UI.Smartphone.Apps.Shared;

public abstract class BaseFilteredScrollerController<TCellView, TModel> : BaTable<TCellView, TModel> where TCellView : BaTableCellView<TModel>
{
	public Action onDataChanged;

	private readonly List<TModel> _allModels = new List<TModel>();

	public int ActiveFilterCount => FilterController.ActiveFilterCount;

	protected abstract BaseFilterController<TModel> FilterController { get; }

	public void ClearFilters()
	{
		FilterController.OnClearFilters();
	}

	private void Awake()
	{
		FilterController.SetUp(SetData);
	}

	public void LoadList()
	{
		_allModels.Clear();
		PopulateAllModels(_allModels);
		SetData();
	}

	public void RemoveModels(Predicate<TModel> match)
	{
		_allModels.RemoveAll(match);
		data.RemoveAll(match);
		scroller.ReloadData();
		onDataChanged?.Invoke();
	}

	public void RefreshFilters()
	{
		float scrollPosition = scroller.ScrollPosition;
		SetData();
		scroller.SetScrollPositionImmediately(scrollPosition);
	}

	protected void ReplaceModel(TModel updatedModel)
	{
		string id = GetDataId(updatedModel);
		int num = _allModels.FindIndex((TModel model) => GetDataId(model) == id);
		if (num >= 0)
		{
			_allModels[num] = updatedModel;
		}
		int num2 = data.FindIndex((TModel model) => GetDataId(model) == id);
		if (num2 >= 0)
		{
			data[num2] = updatedModel;
		}
	}

	protected abstract void PopulateAllModels(List<TModel> allModels);

	private void SetData()
	{
		List<TModel> items = new List<TModel>(_allModels);
		FilterController.ApplyFilters(ref items);
		FilterController.SortItems(ref items);
		data.Clear();
		foreach (TModel item in items)
		{
			data.Add(item);
		}
		scroller.ReloadData();
		onDataChanged?.Invoke();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
