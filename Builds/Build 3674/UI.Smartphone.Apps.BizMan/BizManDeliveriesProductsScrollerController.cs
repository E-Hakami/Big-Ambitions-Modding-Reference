using System;
using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Tags;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;

namespace UI.Smartphone.Apps.BizMan;

public class BizManDeliveriesProductsScrollerController : BaTable<BizManDeliveriesProductCellView, BizManDeliveriesProductModel>
{
	private int _selectedDataIndex = -1;

	public void LoadProducts(DeliveryContract deliveryContract, Action updateContractLabels)
	{
		data.Clear();
		_selectedDataIndex = -1;
		ResetSelectedRow();
		bool isContractActive = deliveryContract.enabled;
		AddMissingProducts(deliveryContract);
		for (int i = 0; i < deliveryContract.items.Count; i++)
		{
			DeliveryContractItem deliveryContractItem = deliveryContract.items[i];
			bool toSelect = _selectedDataIndex == i;
			data.Add(new BizManDeliveriesProductModel(deliveryContractItem, isContractActive, toSelect, updateContractLabels, OnNewCellSelected, deliveryContract.businessAddress, deliveryContract.wholesaleAddress));
		}
		scroller.ReloadData();
	}

	private void AddMissingProducts(DeliveryContract deliveryContract)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(deliveryContract.wholesaleAddress);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(deliveryContract.businessAddress);
		List<string> listOfItemsForSale = buildingRegistration.GetListOfItemsForSale();
		List<string> list = buildingRegistration2.GetListOfItemsForSale().ToList();
		BusinessType businessType = BusinessTypeHelper.GetData(buildingRegistration2);
		if (businessType.HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			list.Add("ba:itemname_paperbag");
		}
		List<string> list2 = deliveryContract.items.Select((DeliveryContractItem x) => x.itemName).ToList();
		HashSet<string> primaryProducts = businessType.GetPrimaryProducts();
		foreach (string itemName in listOfItemsForSale)
		{
			if (primaryProducts.Contains(itemName) || list.Contains(itemName))
			{
				if (!list2.Contains(itemName))
				{
					DeliveryContractItem item = new DeliveryContractItem
					{
						itemName = itemName,
						amount = 0
					};
					deliveryContract.items.Add(item);
				}
			}
			else
			{
				if (!list2.Contains(itemName))
				{
					continue;
				}
				if (!deliveryContract.enabled)
				{
					deliveryContract.items.RemoveAll((DeliveryContractItem x) => x.itemName == itemName);
				}
				else
				{
					deliveryContract.items.RemoveAll((DeliveryContractItem x) => x.itemName == itemName && x.amount == 0);
				}
			}
		}
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		if (_selectedDataIndex != dataIndex)
		{
			return 100f;
		}
		return 200f;
	}

	protected override string GetDataId(BizManDeliveriesProductModel model)
	{
		return model.productName;
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
			foreach (BizManDeliveriesProductModel datum in data)
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
