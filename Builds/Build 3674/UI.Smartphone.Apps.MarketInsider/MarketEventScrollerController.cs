using System.Collections.Generic;
using System.Linq;
using EnhancedUI.EnhancedScroller;
using Entities;
using UnityEngine;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketEventScrollerController : MonoBehaviour, IEnhancedScrollerDelegate
{
	public EnhancedScroller enhancedScroller;

	private List<MarketEventCellView.EventModel> _data = new List<MarketEventCellView.EventModel>();

	public MarketEventCellView prefab;

	private void Start()
	{
		enhancedScroller.Delegate = this;
	}

	public void LoadEvents(string neighborhood)
	{
		_data = (from n in SaveGameManager.Current.marketEvents
			where n.startDay <= SaveGameManager.Current.Day && (string.IsNullOrEmpty(n.neighbourhood) || n.neighbourhood == neighborhood)
			select new MarketEventCellView.EventModel(n.type, n.startDay, n)).ToList();
		_data = _data.OrderByDescending((MarketEventCellView.EventModel x) => x.Day).ToList();
		enhancedScroller.ReloadData();
	}

	public int GetNumberOfCells(EnhancedScroller scroller)
	{
		return _data.Count;
	}

	public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 250f;
	}

	public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
	{
		MarketEventCellView obj = scroller.GetCellView(prefab) as MarketEventCellView;
		obj.SetData(_data[dataIndex]);
		return obj;
	}
}
