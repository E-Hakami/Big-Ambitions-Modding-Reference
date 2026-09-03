using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using Localizor;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.PurchasingAgent;

public class PurchasingAgentProductModel
{
	private const int MaxTargetAmount = 9999999;

	public ImportProduct productRef;

	public List<BuildingRegistration> warehouses;

	public Address importerAddress;

	public bool isTarget;

	public bool isContractActive;

	public bool toSelect;

	public bool isSelected;

	public Action updateContractLabels;

	public Action<int> onNewCellSelected;

	public string ProductName;

	public float Price;

	public float PricePerUnit;

	public string AssignedWarehouse;

	public int WarehouseStock;

	public int Target => productRef.amount;

	public int CargoAmount => Mathf.CeilToInt((float)productRef.amount / (float)productRef.ItemCached.boxSize);

	public PurchasingAgentProductModel(ImportProduct productRef, List<BuildingRegistration> warehouses, Address importerAddress, bool isTarget, bool isContractActive, bool toSelect, Action updateContractLabels, Action<int> onNewCellSelected, float discount)
	{
		this.productRef = productRef;
		this.warehouses = warehouses;
		this.importerAddress = importerAddress;
		this.isTarget = isTarget;
		this.isContractActive = isContractActive;
		this.toSelect = toSelect;
		this.updateContractLabels = updateContractLabels;
		this.onNewCellSelected = onNewCellSelected;
		ProductName = productRef.itemName.GetLocalization();
		PricePerUnit = productRef.Price * discount;
		UpdatePrice();
		UpdateWarehouse();
	}

	public void UpdateWarehouse()
	{
		AssignedWarehouse = warehouses.FirstOrDefault((BuildingRegistration x) => x.Address == productRef.assignedWarehouse)?.BusinessName ?? "common_unassigned".GetLocalization();
		WarehouseStock = ((!(productRef.assignedWarehouse == null)) ? BuildingHelper.CountResourcesInPallets(productRef.assignedWarehouse, productRef.itemName) : 0);
	}

	public void UpdatePrice()
	{
		Price = (float)productRef.GetAmountToBuy(isTarget) * PricePerUnit;
	}

	public void UpdateAmount(int newAmount)
	{
		if (DeliveryHelper.AreWholesaleAndImportLimitsDisabled())
		{
			productRef.amount = newAmount;
		}
		else
		{
			int maxTargetAmount = GetMaxTargetAmount();
			productRef.amount = Mathf.Clamp(newAmount, 0, maxTargetAmount);
		}
		updateContractLabels?.Invoke();
	}

	private int GetMaxTargetAmount()
	{
		int result = 9999999;
		if (!isTarget && DeliveryHelper.ShouldLimitImporterMaxAmount(productRef.itemName, importerAddress))
		{
			int itemAmountOrderedThisWeek = ImportPartnership.GetItemAmountOrderedThisWeek(importerAddress, productRef.itemName);
			result = Mathf.Max(0, productRef.ItemCached.maxOrderAmountPerImporter - itemAmountOrderedThisWeek);
		}
		return result;
	}
}
