using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Buildings.Factory;
using Buildings.Office.Headquarters;
using Helpers;
using UnityEngine.Serialization;

namespace Entities;

[Serializable]
public class LogisticsManagerPlanDestination
{
	private static readonly List<ItemAmountTarget> UndeliveredNoStock = new List<ItemAmountTarget>();

	private static readonly List<ItemAmountTarget> UndeliveredNoShelves = new List<ItemAmountTarget>();

	private static readonly Dictionary<string, FactoryExport> FactoryExportCache = new Dictionary<string, FactoryExport>();

	private static readonly List<DeliveryItem> ItemsInTransaction = new List<DeliveryItem>();

	private static readonly List<CargoInstance> StockCargoInstances = new List<CargoInstance>();

	[FormerlySerializedAs("targetBusinessAddress")]
	public Address deliveryTargetAddress;

	public List<ItemAmountTarget> stockTargets = new List<ItemAmountTarget>();

	public bool isUiCollapsed = true;

	public bool IsTargetExporter => BuildingHelper.GetBuildingRegistration(deliveryTargetAddress)?.businessTypeName == "ba:businesstype_importexport";

	public void Deliver(Warehouse warehouse, bool isImportExport, out List<ItemAmountTarget> undeliveredNoStock, out List<ItemAmountTarget> undeliveredNoShelves)
	{
		undeliveredNoStock = null;
		undeliveredNoShelves = null;
		UndeliveredNoStock.Clear();
		UndeliveredNoShelves.Clear();
		FactoryExportCache.Clear();
		ItemsInTransaction.Clear();
		float importExportProfits = 0f;
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(deliveryTargetAddress);
		if (buildingRegistration == null)
		{
			return;
		}
		foreach (ItemAmountTarget stockTarget in stockTargets)
		{
			int num = ProcessStockTarget(stockTarget, warehouse, isImportExport, buildingRegistration, ref importExportProfits);
			if (num > 0)
			{
				ItemsInTransaction.Add(new DeliveryItem(stockTarget.itemName, -num));
			}
		}
		if (ItemsInTransaction.Count > 0)
		{
			DeliveryTransaction deliveryTransaction = new DeliveryTransaction();
			deliveryTransaction.SetItems(ItemsInTransaction);
			warehouse.deliveryTransactions.Add(deliveryTransaction);
			if (isImportExport && importExportProfits > 0f)
			{
				Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
				TransactionInfo item = new TransactionInfo("ba:transaction_exportedgoods", data);
				LogisticsManagerPlan.PendingTransactions.Add((importExportProfits, item));
				Dictionary<string, FactoryExport> factoryExportCache = FactoryExportCache;
				if (factoryExportCache != null && factoryExportCache.Count > 0)
				{
					foreach (FactoryExport export in FactoryExportCache.Values)
					{
						FactoryExport factoryExport = warehouse.factoryExports.Find((FactoryExport x) => x.itemName == export.itemName);
						if (factoryExport != null)
						{
							factoryExport.totalPrice += export.totalPrice;
							factoryExport.amount += export.amount;
						}
						else
						{
							warehouse.factoryExports.Add(export);
						}
						ProductMarketHelper.UpdateMarketDemand(export.itemName);
					}
				}
				UndeliveredNoStock.Clear();
				UndeliveredNoShelves.Clear();
			}
			if (buildingRegistration is Warehouse warehouse2)
			{
				DeliveryTransaction deliveryTransaction2 = new DeliveryTransaction
				{
					dayOfDelivery = SaveGameManager.Current.Day
				};
				foreach (DeliveryItem item2 in ItemsInTransaction)
				{
					deliveryTransaction2.deliveryItems.Add(new DeliveryItem(item2.itemName, -item2.amountDelivered));
				}
				warehouse2.deliveryTransactions.Add(deliveryTransaction2);
				CleanupOldDeliveries(warehouse2);
			}
			CleanupOldDeliveries(warehouse);
		}
		if (!isImportExport)
		{
			BusinessHelper.FillUpEmptyShowcaseShelvesAndPointsOfSales(deliveryTargetAddress);
		}
		if (UndeliveredNoStock.Count > 0)
		{
			undeliveredNoStock = new List<ItemAmountTarget>(UndeliveredNoStock);
		}
		if (UndeliveredNoShelves.Count > 0)
		{
			undeliveredNoShelves = new List<ItemAmountTarget>(UndeliveredNoShelves);
		}
		UndeliveredNoStock.Clear();
		UndeliveredNoShelves.Clear();
		static void CleanupOldDeliveries(Warehouse w)
		{
			if (w.deliveryTransactions.Count > 60)
			{
				w.deliveryTransactions.RemoveRange(0, w.deliveryTransactions.Count - 60);
			}
		}
	}

	private int ProcessStockTarget(ItemAmountTarget stockTarget, Warehouse warehouse, bool isImportExport, BuildingRegistration destinationTargetBusiness, ref float importExportProfits)
	{
		if (stockTarget.targetAmount <= 0)
		{
			return 0;
		}
		CargoInstance cargoInstance;
		if (isImportExport)
		{
			BuildingHelper.GetItemsWithStock(warehouse, stockTarget.itemName, StockCargoInstances);
			int targetAmount = stockTarget.targetAmount;
			cargoInstance = BuildingHelper.WithdrawFromCargo(StockCargoInstances, targetAmount);
			if (cargoInstance == null || cargoInstance.amount < targetAmount)
			{
				UndeliveredNoStock.Add(new ItemAmountTarget(stockTarget.itemName, targetAmount - (cargoInstance?.amount ?? 0)));
			}
			if (cargoInstance == null || cargoInstance.amount == 0)
			{
				return 0;
			}
			float productExportPrice = ProductMarketHelper.GetProductExportPrice(stockTarget.itemName);
			importExportProfits += productExportPrice * (float)cargoInstance.amount;
			if (!FactoryExportCache.TryGetValue(stockTarget.itemName, out var value))
			{
				value = new FactoryExport
				{
					itemName = stockTarget.itemName
				};
				FactoryExportCache[stockTarget.itemName] = value;
			}
			value.amount += cargoInstance.amount;
			value.totalPrice += productExportPrice * (float)cargoInstance.amount;
			BuildingHelper.RemoveEmptyCargo(StockCargoInstances);
			return cargoInstance.amount;
		}
		int num = BuildingHelper.CountTotalResourcesInStock(deliveryTargetAddress, stockTarget.itemName);
		int num2 = stockTarget.targetAmount - num;
		if (num2 <= 0)
		{
			return 0;
		}
		BuildingHelper.GetItemsWithStock(warehouse, stockTarget.itemName, StockCargoInstances);
		cargoInstance = BuildingHelper.WithdrawFromCargo(StockCargoInstances, num2);
		if (cargoInstance == null || cargoInstance.amount < num2)
		{
			UndeliveredNoStock.Add(new ItemAmountTarget(stockTarget.itemName, num2 - (cargoInstance?.amount ?? 0)));
		}
		if (cargoInstance == null || cargoInstance.amount == 0)
		{
			return 0;
		}
		int amount = cargoInstance.amount;
		ItemHelper.DeliverCargoToBuilding(cargoInstance, destinationTargetBusiness, (ItemInstance x) => (x.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0 && x.GetStockInstance().itemName == stockTarget.itemName);
		if (cargoInstance.amount == 0)
		{
			BuildingHelper.RemoveEmptyCargo(StockCargoInstances);
			return amount;
		}
		ItemHelper.DeliverCargoToBuilding(cargoInstance, destinationTargetBusiness, (ItemInstance x) => (x.ItemCached.type & ItemType.StorageShelf) != 0);
		if (cargoInstance.amount > 0)
		{
			UndeliveredNoShelves.Add(new ItemAmountTarget(stockTarget.itemName, cargoInstance.amount));
			BuildingHelper.ReturnToCargo(StockCargoInstances, cargoInstance);
		}
		BuildingHelper.RemoveEmptyCargo(StockCargoInstances);
		return amount - cargoInstance.amount;
	}

	public void Reset()
	{
		deliveryTargetAddress = null;
		stockTargets = new List<ItemAmountTarget>();
	}
}
