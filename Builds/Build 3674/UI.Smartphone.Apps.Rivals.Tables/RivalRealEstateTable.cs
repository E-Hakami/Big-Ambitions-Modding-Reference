using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Helpers;
using JimmysUnityUtilities;
using UI.Smartphone.Apps.MarketInsider;

namespace UI.Smartphone.Apps.Rivals.Tables;

public class RivalRealEstateTable : BaTable<RealEstateCellView, RealEstateCellView.RealEstateModel>
{
	private void OnEnable()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			Load(InstanceBehavior<RivalsApp>.Instance.selectedRival?.ownedBuildings);
		});
	}

	public void Load(List<BuildingRegistration> registrations)
	{
		if (registrations != null)
		{
			data.Clear();
			foreach (BuildingRegistration registration in registrations)
			{
				data.Add(new RealEstateCellView.RealEstateModel(registration.Address, registration.BuildingCached.BuildingType, registration.BuildingCached.totalSqm, registration.BuildingCached.GetMarketValue(), registration.Address.GetBuildingSalePrice(), registration.BuildingCached.Neighbourhood));
			}
		}
		ResetFilters();
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
