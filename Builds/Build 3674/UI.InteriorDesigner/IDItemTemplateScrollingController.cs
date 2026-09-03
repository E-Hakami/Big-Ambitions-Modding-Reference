using System;
using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class IDItemTemplateScrollingController : BaTable<IDItemTemplateCellView, IDItemTemplatesModel>
{
	public const int ItemsPerCell = 9;

	[SerializeField]
	private float cellSize = 185f;

	[SerializeField]
	private ScrollRect scrollRect;

	[FormerlySerializedAs("searchEngine")]
	public FurnitureItemSearch search;

	private Func<bool> _isFavoriteItemsToggleOn;

	private Action _showFavorites;

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return cellSize;
	}

	public void SetUp(Action showFavorites, Func<bool> isFavoriteItemsToggleOn)
	{
		_showFavorites = showFavorites;
		_isFavoriteItemsToggleOn = isFavoriteItemsToggleOn;
		if (search == null)
		{
			search = base.gameObject.AddComponent<FurnitureItemSearch>();
		}
		search.Initialize(showFavorites, isFavoriteItemsToggleOn);
		search.onResultsFiltered = PackageItemsIntoRows;
	}

	public void LoadList(List<string> itemNames, List<string> visibleTags, Action<IDItemTemplateBase, string> onItemSelected)
	{
		search.LoadItemData(itemNames, visibleTags, onItemSelected);
	}

	public void ClearFilterAndReload()
	{
		float scrollPositionFactor = 1f - scrollRect.verticalNormalizedPosition;
		search.ShowAllItems();
		scroller.ReloadData(scrollPositionFactor);
	}

	public void ShowNoItems()
	{
		search.ShowNoItems();
		data.Clear();
		scroller.ReloadData();
	}

	public void FilterReload(HashSet<string> itemNamesSet)
	{
		search.FilterByItemNames(itemNamesSet);
		scroller.ReloadData();
	}

	public void FilterReload(List<string> includedTags, List<string> excludedTags)
	{
		search.FilterByTags(includedTags, excludedTags);
		scroller.ReloadData();
	}

	public void FilterReload(string filterText)
	{
		search.FilterByText(filterText);
		scroller.ReloadData();
	}

	private void PackageItemsIntoRows(List<IDItemUiTemplateData> items)
	{
		data.Clear();
		int num = Mathf.CeilToInt((float)items.Count / 9f);
		data.Capacity = num;
		for (int i = 0; i < num; i++)
		{
			IDItemTemplatesModel iDItemTemplatesModel = new IDItemTemplatesModel();
			int num2 = i * 9;
			int num3 = Mathf.Min(9, items.Count - num2);
			for (int j = 0; j < num3; j++)
			{
				iDItemTemplatesModel.itemTemplates.Add(items[num2 + j]);
			}
			data.Add(iDItemTemplatesModel);
		}
	}
}
