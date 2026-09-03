using System;
using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Items;
using EnhancedUI.EnhancedScroller;
using Localizor;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleScrollerController : BaTable<ScheduleCellView, ScheduleWorkstationModel>
{
	[SerializeField]
	private float cellSize = 180f;

	private List<ScheduleWorkstationModel> _fullData = new List<ScheduleWorkstationModel>();

	private List<ItemInstance> _stations;

	public override void Start()
	{
		base.Start();
		ScheduleHelper.RequestScheduleScrollerReload.AddListener(ReloadDataInPosition);
	}

	private void OnDestroy()
	{
		ScheduleHelper.RequestScheduleScrollerReload.RemoveListener(ReloadDataInPosition);
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return cellSize;
	}

	public void FilterReload(string filterText)
	{
		if (string.IsNullOrEmpty(filterText))
		{
			data = _fullData.Copy();
			scroller.ReloadData(1f - WorkShiftHelper.ScrollRect.verticalNormalizedPosition);
			return;
		}
		data = _fullData.Where((ScheduleWorkstationModel x) => x.workstationName.ToLower().Contains(filterText.ToLower())).ToList();
		scroller.ReloadData(1f - WorkShiftHelper.ScrollRect.verticalNormalizedPosition);
	}

	public void LoadList(List<ItemInstance> stations)
	{
		_fullData.Clear();
		data.Clear();
		_stations = stations;
		foreach (ItemInstance station in stations)
		{
			LoadStation(station);
		}
		_fullData = data.Copy();
		scroller.ReloadData();
	}

	private void LoadStation(ItemInstance station)
	{
		WorkShiftType shiftType = ((station?.ItemCached.suitableSkills == null || !station.ItemCached.suitableSkills.Contains("ba:skill_cleaning")) ? WorkShiftType.Default : WorkShiftType.Cleaning);
		string workstationName = ((station != null) ? (string.IsNullOrEmpty(station.alias) ? station.itemName.GetLocalization() : station.alias) : "theater_stage_label".GetLocalization());
		ScheduleWorkstationModel item = new ScheduleWorkstationModel
		{
			workstationName = workstationName,
			workstationId = ((station == null) ? string.Empty : station.id),
			customersPerHour = ((!ScheduleHelper.IsHeadquarters && station != null) ? station.ItemCached.addedCustomersPerHour : 0),
			attachedItems = ((station == null) ? null : ScheduleHelper.GetAttachedItems(station)),
			isFactoryMachine = (station != null && (station.ItemCached.type & ItemType.FactoryMachine) != 0),
			shiftType = shiftType
		};
		data.Add(item);
	}

	private void ReloadDataInPosition(bool reloadStations)
	{
		if (reloadStations)
		{
			_fullData.Clear();
			data.Clear();
			foreach (ItemInstance station in _stations)
			{
				LoadStation(station);
			}
			_fullData = data.Copy();
		}
		scroller.ReloadData(1f - WorkShiftHelper.ScrollRect.verticalNormalizedPosition);
	}
}
