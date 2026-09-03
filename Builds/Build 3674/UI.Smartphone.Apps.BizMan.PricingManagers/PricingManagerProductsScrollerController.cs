using System;
using System.Collections.Generic;
using System.Reflection;
using BaTable;
using Buildings.Office.Headquarters;
using EnhancedUI.EnhancedScroller;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerProductsScrollerController : BaTable<PricingManagerProductCellView, PricingManagerProductModel>
{
	private const string DefaultSortColumn = "ProductName";

	[SerializeField]
	private PricingManagerFilterController filterController;

	[SerializeField]
	private Badge filterBadge;

	[SerializeField]
	private GameObject noProductsRoot;

	[SerializeField]
	private Button applySuggestedPricesButton;

	[SerializeField]
	private float cellHeight = 100f;

	private readonly List<PricingManagerProductModel> _allModels = new List<PricingManagerProductModel>();

	private string _sortColumn = "ProductName";

	private bool _isStarted;

	private List<PricingManagerProductModel> ApplyTargets
	{
		get
		{
			if (!PricingManagerHelper.Settings.applyOnlyToVisibleProducts)
			{
				return _allModels;
			}
			return data;
		}
	}

	private void OnEnable()
	{
		applySuggestedPricesButton.onClick.AddListener(OnClickApplySuggestedPrices);
	}

	private void OnDisable()
	{
		applySuggestedPricesButton.onClick.RemoveListener(OnClickApplySuggestedPrices);
	}

	public override void Start()
	{
		base.Start();
		filterController.SetUp(RefreshFilterAndSort);
		_isStarted = true;
		RefreshFilterAndSort();
	}

	public void LoadPlan(PricingManagerPlan plan)
	{
		_allModels.Clear();
		foreach (PriceSuggestion cachedSuggestion in plan.cachedSuggestions)
		{
			_allModels.Add(new PricingManagerProductModel(cachedSuggestion, plan));
		}
		filterController.RebuildToggles();
		RefreshFilterAndSort();
	}

	private void OnClickApplySuggestedPrices()
	{
		LanguageChangeEventDataHolder bodyData = "bizman_pricingmanagers_apply_suggested_confirm".Localize(new
		{
			numberOfProducts = ApplyTargets.Count
		});
		Action onConfirmAction = ApplySuggestedPrices;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	private void ApplySuggestedPrices()
	{
		foreach (PricingManagerProductModel applyTarget in ApplyTargets)
		{
			applyTarget.Plan.ApplySuggestedPrice(applyTarget.Suggestion.itemName, applyTarget.Suggestion.suggestedMax);
		}
		SaveGameManager.MarkChange();
		RefreshFilterAndSort();
	}

	public override void ChangeSortOrder(string propertyName = null, bool flipOrder = true)
	{
		if (string.IsNullOrEmpty(propertyName) || IsSortableColumn(propertyName))
		{
			base.ChangeSortOrder(propertyName, flipOrder);
			if (!string.IsNullOrEmpty(propertyName))
			{
				_sortColumn = propertyName;
			}
		}
	}

	private static bool IsSortableColumn(string propertyName)
	{
		return (object)typeof(PricingManagerProductModel).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public) != null;
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return cellHeight;
	}

	private void RefreshFilterAndSort()
	{
		if (_isStarted)
		{
			List<PricingManagerProductModel> items = new List<PricingManagerProductModel>(_allModels);
			filterController.ApplyFilters(ref items);
			filterBadge.UpdateBadge(filterController.ActiveFilterCount);
			data = items;
			noProductsRoot.SetActive(data.Count == 0);
			applySuggestedPricesButton.interactable = ApplyTargets.Count > 0;
			if (data.Count == 0)
			{
				scroller.ReloadData();
			}
			else
			{
				ChangeSortOrder(_sortColumn, flipOrder: false);
			}
		}
	}
}
