using System;
using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;

namespace UI.Smartphone.Apps.BizMan.PurchasingAgent;

public class PurchasingAgentProductsScrollerController : BaTable<PurchasingAgentProductCellView, PurchasingAgentProductModel>
{
	private int _selectedDataIndex = -1;

	public void LoadProducts(ImportPartnership currentImportPartnership, Action updateContractLabels, PurchasingAgentProductsMassActionsUI massActionsUI)
	{
		data.Clear();
		_selectedDataIndex = -1;
		ResetSelectedRow();
		List<BuildingRegistration> playerBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterForWarehouses);
		if ((bool)massActionsUI)
		{
			massActionsUI.UpdateDropdown(new List<BuildingRegistration>(playerBuildingRegistrations));
		}
		float getDiscount = currentImportPartnership.GetDiscount;
		bool isTarget = currentImportPartnership.isTarget;
		bool isActive = currentImportPartnership.isActive;
		currentImportPartnership.AddMissingImportProducts();
		for (int i = 0; i < currentImportPartnership.products.Count; i++)
		{
			ImportProduct productRef = currentImportPartnership.products[i];
			bool toSelect = _selectedDataIndex == i;
			data.Add(new PurchasingAgentProductModel(productRef, playerBuildingRegistrations, currentImportPartnership.importAddress, isTarget, isActive, toSelect, updateContractLabels, OnNewCellSelected, getDiscount));
		}
		scroller.ReloadData();
	}

	private bool PlayerBuildingFilterForWarehouses(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.businessTypeName != "ba:businesstype_empty")
		{
			return buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse";
		}
		return false;
	}

	public void UpdateIsTarget(bool isTarget)
	{
		foreach (PurchasingAgentProductModel datum in data)
		{
			datum.isTarget = isTarget;
			datum.UpdateAmount(datum.productRef.amount);
		}
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		if (_selectedDataIndex != dataIndex)
		{
			return 100f;
		}
		return 200f;
	}

	protected override string GetDataId(PurchasingAgentProductModel model)
	{
		return model.ProductName;
	}

	private void OnNewCellSelected(int dataIndex)
	{
		float scrollPosition = scroller.ScrollPosition;
		if (_selectedDataIndex == dataIndex)
		{
			UnSelectRowAndReload(dataIndex, scrollPosition);
		}
		else
		{
			SelectCellAndReload(dataIndex, scrollPosition);
		}
	}

	private void SelectCellAndReload(int dataIndex, float scrollPosition)
	{
		_selectedDataIndex = dataIndex;
		for (int i = 0; i < data.Count; i++)
		{
			bool flag = _selectedDataIndex == i;
			data[i].toSelect = flag;
			data[i].isSelected = flag;
		}
		scroller.ReloadData();
		scroller.SetScrollPositionImmediately(scrollPosition);
	}

	private void UnSelectRowAndReload(int dataIndex, float scrollPosition)
	{
		_selectedDataIndex = -1;
		data[dataIndex].isSelected = false;
		data[dataIndex].toSelect = false;
		ResetSelectedRow();
		scroller.ReloadData();
		scroller.SetScrollPositionImmediately(scrollPosition);
	}

	protected override void OnDataOrdered()
	{
		if (_selectedDataIndex != -1)
		{
			foreach (PurchasingAgentProductModel datum in data)
			{
				if (datum.isSelected)
				{
					_selectedDataIndex = data.IndexOf(datum);
					break;
				}
			}
		}
		base.OnDataOrdered();
	}
}
