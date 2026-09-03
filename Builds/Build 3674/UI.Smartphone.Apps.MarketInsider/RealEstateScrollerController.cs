using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Helpers;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MarketInsider;

public class RealEstateScrollerController : BaTable<RealEstateCellView, RealEstateCellView.RealEstateModel>
{
	[SerializeField]
	private RealEstateFilterController filterController;

	[SerializeField]
	private Badge filterBadge;

	[SerializeField]
	private Toggle onlyForSaleToggle;

	private readonly List<RealEstateCellView.RealEstateModel> _allModels = new List<RealEstateCellView.RealEstateModel>();

	private string _selectedNeighborhood;

	private bool _showOnlyForSale;

	private bool _isStarted;

	private void Awake()
	{
		if ((bool)onlyForSaleToggle)
		{
			onlyForSaleToggle.onValueChanged.AddListener(OnOnlyForSaleToggled);
		}
	}

	private void OnEnable()
	{
		CoroutineUtility.RunAfterOneFrame(Refresh);
	}

	public override void Start()
	{
		base.Start();
		filterController.SetUp(RefreshFilterAndSort);
		_isStarted = true;
		RefreshFilterAndSort();
	}

	public void ClearFilters()
	{
		filterController.OnClearFilters();
	}

	public void Refresh()
	{
		LoadRealEstate(_selectedNeighborhood);
	}

	public void LoadRealEstate(string neighborhood)
	{
		_selectedNeighborhood = neighborhood;
		_allModels.Clear();
		_allModels.AddRange(from x in SaveGameManager.Current.BuildingRegistrations
			where x.GetBuildingType() != "ba:buildingtype_special" && x.Neighborhood == neighborhood
			select new RealEstateCellView.RealEstateModel(x.Address, x.BuildingCached.BuildingType, x.BuildingCached.totalSqm, x.BuildingCached.GetMarketValue(), x.Address.GetBuildingSalePrice(), x.Neighborhood)
			{
				IsForSale = SaveGameManager.Current.buildingsForSale.Exists((BuildingForSale y) => y.address == x.Address)
			});
		RefreshFilterAndSort();
		ResetFilters();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}

	private void RefreshFilterAndSort()
	{
		if (!_isStarted)
		{
			return;
		}
		List<RealEstateCellView.RealEstateModel> items = new List<RealEstateCellView.RealEstateModel>(_allModels);
		filterController.ApplyFilters(ref items);
		if (_showOnlyForSale)
		{
			items.RemoveAll((RealEstateCellView.RealEstateModel x) => !x.IsForSale);
		}
		filterBadge.UpdateBadge(filterController.ActiveFilterCount);
		data = items;
		if (data.IsEmpty() || base.CurrentSortColumn.IsNullOrEmpty())
		{
			scroller.ReloadData();
		}
		else
		{
			ChangeSortOrder(base.CurrentSortColumn, flipOrder: false);
		}
	}

	private void OnOnlyForSaleToggled(bool isOn)
	{
		_showOnlyForSale = isOn;
		RefreshFilterAndSort();
	}
}
