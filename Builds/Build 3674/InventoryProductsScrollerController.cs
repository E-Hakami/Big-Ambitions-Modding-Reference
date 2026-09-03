using System;
using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using EnhancedUI.EnhancedScroller;
using Entities;
using Extensions;
using Helpers;
using Localizor;

public class InventoryProductsScrollerController : BaTable<InventoryProductCellView, InventoryProductCellView.InventoryProductModel>
{
	private BizManBusiness _bizManBusiness;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
	}

	public void LoadProducts()
	{
		data.Clear();
		List<OrderHistoryEntry> source = _bizManBusiness.buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)).ToList();
		var source2 = (from x in source.SelectMany((OrderHistoryEntry x) => x.itemSales)
			group x by x.itemName into x
			select new
			{
				itemName = x.Key,
				amountSold = x.Sum((OrderHistoryEntry.ItemReport c) => c.amountSold),
				a = x.Sum((OrderHistoryEntry.ItemReport c) => c.totalPrice)
			}).ToList();
		foreach (string itemName in _bizManBusiness.buildingRegistration.cachedAvailableProducts)
		{
			if (string.IsNullOrEmpty(itemName))
			{
				continue;
			}
			Item byName = ItemsGetter.GetByName(itemName);
			if (byName.GetWholesalePrice() == 0f && (byName.type & ItemType.ServiceProduct) == 0)
			{
				continue;
			}
			RetailPrice retailPrice = _bizManBusiness.buildingRegistration.retailPrices.SingleOrDefault((RetailPrice x) => x.itemName == itemName);
			RetailPrice retailPrice2 = _bizManBusiness.buildingRegistration.storedRetailPrices.FirstOrDefault((RetailPrice x) => x.itemName == itemName);
			if (retailPrice == null)
			{
				retailPrice = new RetailPrice
				{
					price = ItemHelper.GetDefaultMarketPrice(itemName),
					itemName = itemName
				};
				if (retailPrice2 != null)
				{
					retailPrice.price = retailPrice2.price;
				}
				else
				{
					retailPrice2 = new RetailPrice
					{
						price = retailPrice.price,
						itemName = itemName
					};
					_bizManBusiness.buildingRegistration.storedRetailPrices.Add(retailPrice2);
				}
				_bizManBusiness.buildingRegistration.retailPrices.Add(retailPrice);
				ProductMarketHelper.UpdateMarketDemand(itemName);
			}
			else if (retailPrice2 == null)
			{
				retailPrice2 = new RetailPrice
				{
					price = retailPrice.price,
					itemName = itemName
				};
				_bizManBusiness.buildingRegistration.storedRetailPrices.Add(retailPrice2);
			}
			var anon = source2.FirstOrDefault(x => x.itemName == itemName);
			List<ItemSoldPerPriceEntry> list = new List<ItemSoldPerPriceEntry>();
			foreach (OrderHistoryEntry.ItemReport item in from x in source.SelectMany((OrderHistoryEntry x) => x.itemSales)
				where x.itemName == itemName
				select x)
			{
				if (item.itemSoldBreakdownEntries == null)
				{
					continue;
				}
				ItemSoldPerPriceEntry[] itemSoldBreakdownEntries = item.itemSoldBreakdownEntries;
				foreach (ItemSoldPerPriceEntry itemSoldBreakdownEntry in itemSoldBreakdownEntries)
				{
					ItemSoldPerPriceEntry itemSoldPerPriceEntry = list.FirstOrDefault((ItemSoldPerPriceEntry x) => Math.Abs(x.price - itemSoldBreakdownEntry.price) < 0.01f);
					if (itemSoldPerPriceEntry != null)
					{
						itemSoldPerPriceEntry.amount += itemSoldBreakdownEntry.amount;
						continue;
					}
					itemSoldPerPriceEntry = new ItemSoldPerPriceEntry
					{
						price = itemSoldBreakdownEntry.price,
						amount = itemSoldBreakdownEntry.amount
					};
					list.Add(itemSoldPerPriceEntry);
				}
			}
			data.Add(new InventoryProductCellView.InventoryProductModel(itemName.GetLocalization(), retailPrice, retailPrice2, byName, anon?.amountSold ?? 0, ItemHelper.GetLowestMarketPrice(itemName, _bizManBusiness.buildingRegistration.Neighborhood, forceUpdate: true), BuildingHelper.CountTotalResourcesInStock(_bizManBusiness.buildingRegistration, itemName, includeProducers: true, includePallets: false), _bizManBusiness.buildingRegistration.Neighborhood, list.OrderByDescending((ItemSoldPerPriceEntry x) => x.price).ToList()));
		}
		if (BusinessTypeHelper.GetData(_bizManBusiness).HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			var anon2 = source2.FirstOrDefault(x => x.itemName == "ba:itemname_paperbag");
			Item byName2 = ItemsGetter.GetByName("ba:itemname_paperbag");
			RetailPrice retailPrice3 = _bizManBusiness.buildingRegistration.retailPrices.SingleOrDefault((RetailPrice x) => x.itemName == "ba:itemname_paperbag");
			RetailPrice retailPrice4 = _bizManBusiness.buildingRegistration.storedRetailPrices.FirstOrDefault((RetailPrice x) => x.itemName == "ba:itemname_paperbag");
			if (retailPrice3 == null)
			{
				retailPrice3 = new RetailPrice
				{
					price = 0f,
					itemName = "ba:itemname_paperbag"
				};
				if (retailPrice4 != null)
				{
					retailPrice3.price = retailPrice4.price;
				}
				else
				{
					retailPrice4 = new RetailPrice
					{
						price = retailPrice3.price,
						itemName = "ba:itemname_paperbag"
					};
					_bizManBusiness.buildingRegistration.storedRetailPrices.Add(retailPrice4);
				}
				_bizManBusiness.buildingRegistration.retailPrices.Add(retailPrice3);
			}
			else if (retailPrice4 == null)
			{
				retailPrice4 = new RetailPrice
				{
					price = retailPrice3.price,
					itemName = "ba:itemname_paperbag"
				};
				_bizManBusiness.buildingRegistration.storedRetailPrices.Add(retailPrice4);
			}
			List<ItemSoldPerPriceEntry> amountSoldPerPrice = new List<ItemSoldPerPriceEntry>
			{
				new ItemSoldPerPriceEntry
				{
					price = 0f,
					amount = (anon2?.amountSold ?? 0)
				}
			};
			data.Add(new InventoryProductCellView.InventoryProductModel("ba:itemname_paperbag".GetLocalization(), retailPrice3, retailPrice4, byName2, anon2?.amountSold ?? 0, 0f, BuildingHelper.CountTotalResourcesInStock(_bizManBusiness.buildingRegistration, "ba:itemname_paperbag", includeProducers: true, includePallets: false), _bizManBusiness.buildingRegistration.Neighborhood, amountSoldPerPrice));
		}
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
