using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketDemandScrollerController : BaTable<MarketDemandCellView, MarketDemandCellView.DemandModel>
{
	private const string DefaultSort = "ProductName";

	[SerializeField]
	private MarketDemandFilterController filterController;

	[SerializeField]
	private Badge filterBadge;

	private readonly List<MarketDemandCellView.DemandModel> _allModels = new List<MarketDemandCellView.DemandModel>();

	private string _selectedNeighbourhood;

	private bool _isStarted;

	public override void Start()
	{
		base.Start();
		filterController.SetUp(RefreshFilterAndSort);
		_isStarted = true;
		RefreshFilterAndSort();
	}

	public void UnloadDemands()
	{
		_selectedNeighbourhood = string.Empty;
		_allModels.Clear();
		data.Clear();
	}

	public void ClearFilters()
	{
		filterController.OnClearFilters();
	}

	public void LoadDemands(string neighborhood)
	{
		if (neighborhood == _selectedNeighbourhood && !_allModels.IsEmpty())
		{
			return;
		}
		_selectedNeighbourhood = neighborhood;
		_allModels.Clear();
		foreach (ProductMarketEntry marketEntry in SaveGameManager.Current.productMarketEntries)
		{
			if (!ProductMarketHelper.CanNeighborhoodHaveItemDemand(neighborhood, marketEntry.itemName))
			{
				continue;
			}
			NeighborhoodDemand neighborhoodDemand = marketEntry.demandValues.FirstOrDefault((NeighborhoodDemand v) => v.neighborhood == neighborhood);
			if (neighborhoodDemand != null)
			{
				MarketDemandCellView.DemandModel item = new MarketDemandCellView.DemandModel(marketEntry.itemName, neighborhoodDemand.demand, ItemHelper.GetLowestMarketPrice(marketEntry.itemName, neighborhood), marketEntry.importPriceIndex, neighborhoodDemand.providers, SaveGameManager.Current.marketEvents.Where((MarketEvent me) => me.IsActive && (string.IsNullOrEmpty(me.neighbourhood) || me.neighbourhood == neighborhood) && me.itemName == marketEntry.itemName).Sum((MarketEvent me) => me.demandImpact));
				_allModels.Add(item);
			}
		}
		RefreshFilterAndSort();
		ResetFilters();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}

	public override void ChangeSortOrder(string propertyName = null, bool flipOrder = true)
	{
		base.ChangeSortOrder(propertyName, flipOrder);
		if (propertyName != "Demand" || data.IsEmpty())
		{
			return;
		}
		data = (orderByAsc ? (from x in data
			orderby x.Demand descending, SaveGameManager.Current.marketEvents.Count((MarketEvent me) => me.IsActive && (string.IsNullOrEmpty(me.neighbourhood) || me.neighbourhood == _selectedNeighbourhood) && me.itemName == x.ItemName && me.type == MarketEventType.Hype) descending
			select x) : (from x in data
			orderby x.Demand, SaveGameManager.Current.marketEvents.Count((MarketEvent me) => me.IsActive && (string.IsNullOrEmpty(me.neighbourhood) || me.neighbourhood == _selectedNeighbourhood) && me.itemName == x.ItemName && me.type == MarketEventType.Hype)
			select x)).ToList();
		scroller.ReloadData();
		if (flipOrder)
		{
			GameEvent.Invoke("ba:gameevent_marketinsidersortbydemand");
		}
	}

	private void RefreshFilterAndSort()
	{
		if (!_isStarted)
		{
			return;
		}
		List<MarketDemandCellView.DemandModel> items = new List<MarketDemandCellView.DemandModel>(_allModels);
		filterController.ApplyFilters(ref items);
		filterBadge.UpdateBadge(filterController.ActiveFilterCount);
		data = items;
		if (data.IsEmpty())
		{
			scroller.ReloadData();
			return;
		}
		string text = base.CurrentSortColumn;
		if (text.IsNullOrEmpty())
		{
			text = "ProductName";
		}
		ChangeSortOrder(text, flipOrder: false);
	}
}
