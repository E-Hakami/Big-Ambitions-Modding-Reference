using System;
using Entities;
using Helpers;
using Localizor;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BizManDeliveriesProductModel
{
	public DeliveryContractItem deliveryContractItem;

	public bool isContractActive;

	public bool toSelect;

	public bool isSelected;

	public Action updateContractLabels;

	public Action<int> onNewCellSelected;

	public Address businessAddress;

	public Address wholesalerAddress;

	public string productName;

	public float price;

	public float pricePerUnit;

	public int stock;

	public int CargoAmount => Mathf.CeilToInt((float)deliveryContractItem.amount / (float)deliveryContractItem.ItemCached.boxSize);

	public int Amount => deliveryContractItem.amount;

	public BizManDeliveriesProductModel(DeliveryContractItem deliveryContractItem, bool isContractActive, bool toSelect, Action updateContractLabels, Action<int> onNewCellSelected, Address businessAddress, Address wholesalerAddress)
	{
		this.deliveryContractItem = deliveryContractItem;
		this.isContractActive = isContractActive;
		this.toSelect = toSelect;
		this.updateContractLabels = updateContractLabels;
		this.onNewCellSelected = onNewCellSelected;
		this.businessAddress = businessAddress;
		this.wholesalerAddress = wholesalerAddress;
		productName = deliveryContractItem.itemName.GetLocalization();
		SetPricePerUnit();
		UpdatePrice();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(businessAddress);
		stock = BuildingHelper.CountTotalResourcesInStock(buildingRegistration, deliveryContractItem.itemName, includeProducers: true, includePallets: false);
	}

	private void SetPricePerUnit()
	{
		float num = (float)wholesalerAddress.GetPriceIndex() / 100f;
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(wholesalerAddress);
		pricePerUnit = deliveryContractItem.ItemCached.GetWholesalePrice() * num * ProductMarketHelper.GetProductMarketEventMultiplier(deliveryContractItem.itemName, buildingRegistration.Neighborhood);
	}

	public void UpdatePrice()
	{
		price = (float)deliveryContractItem.amount * pricePerUnit;
	}

	public void UpdateAmount(int newAmount)
	{
		if (DeliveryHelper.AreWholesaleAndImportLimitsDisabled())
		{
			deliveryContractItem.amount = newAmount;
		}
		else
		{
			int max = deliveryContractItem.ItemCached.maxWholesaleOrderAmount - deliveryContractItem.amountOrderedThisWeek;
			deliveryContractItem.amount = Mathf.Clamp(newAmount, 0, max);
		}
		updateContractLabels?.Invoke();
	}
}
