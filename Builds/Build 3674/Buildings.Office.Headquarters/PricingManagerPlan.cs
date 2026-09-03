using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Controllers;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Notification;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public class PricingManagerPlan
{
	public readonly string id = UuidHelper.GenerateBase64Uuid();

	public string assignedEmployeeId;

	public Address headquartersAddress;

	public string supervisedNeighborhood;

	public List<OriginalStorePrice> originalStorePrices = new List<OriginalStorePrice>();

	public List<RetailPrice> appliedPrices = new List<RetailPrice>();

	public HashSet<string> manuallyPricedItems = new HashSet<string>();

	public int nextUpdateDay;

	public int nextUpdateHour;

	public List<PriceSuggestion> cachedSuggestions = new List<PriceSuggestion>();

	private List<BuildingRegistration> _supervisedStoresCache;

	private int _supervisedStoresCacheFrame = -1;

	private HashSet<(Address, string)> _snapshottedPrices;

	public EmployeeInstance AnalystInstance
	{
		get
		{
			if (!assignedEmployeeId.IsNullOrEmpty())
			{
				return EmployeeHelper.GetEmployeeById(assignedEmployeeId, showError: false);
			}
			return null;
		}
	}

	public float PricingManagerSkillValue => AnalystInstance?.GetSkillValue("ba:skill_pricingmanager") ?? 0f;

	public void AssignEmployee(string employeeId)
	{
		assignedEmployeeId = employeeId;
		SnapshotCurrentPrices();
		ReapplyPrices();
		RecomputeSuggestions();
	}

	public void UnAssignEmployee()
	{
		RestoreOriginalPrices();
		assignedEmployeeId = null;
		cachedSuggestions.Clear();
	}

	public void SetSupervisedNeighborhood(string neighborhood)
	{
		RestoreOriginalPrices();
		appliedPrices?.Clear();
		manuallyPricedItems?.Clear();
		supervisedNeighborhood = neighborhood;
		_supervisedStoresCacheFrame = -1;
		SnapshotCurrentPrices();
		RecomputeSuggestions();
	}

	public void Delete()
	{
		RestoreOriginalPrices();
		List<PricingManagerPlan> pricingManagerPlans = SaveGameManager.Current.pricingManagerPlans;
		for (int num = pricingManagerPlans.Count - 1; num >= 0; num--)
		{
			if (pricingManagerPlans[num].id == id)
			{
				pricingManagerPlans.RemoveAt(num);
			}
		}
	}

	public bool IsUpdateDue()
	{
		return TimeHelper.IsInThePast(nextUpdateDay, nextUpdateHour);
	}

	public void RunUpdate()
	{
		(nextUpdateDay, nextUpdateHour) = PricingManagerHelper.GetNextUpdateTime();
		RecomputeSuggestions();
		ReapplyManagedPrices();
		NotifyMispricedItems();
	}

	public bool IsManuallyPriced(string itemName)
	{
		if (manuallyPricedItems != null)
		{
			return manuallyPricedItems.Contains(itemName);
		}
		return false;
	}

	public void ApplyManualPrice(string itemName, float price)
	{
		if (manuallyPricedItems == null)
		{
			manuallyPricedItems = new HashSet<string>();
		}
		manuallyPricedItems.Add(itemName);
		ApplyPrice(itemName, price);
	}

	public void ApplySuggestedPrice(string itemName, float price)
	{
		manuallyPricedItems?.Remove(itemName);
		ApplyPrice(itemName, price);
	}

	private void ReapplyManagedPrices()
	{
		foreach (PriceSuggestion cachedSuggestion in cachedSuggestions)
		{
			if (!IsManuallyPriced(cachedSuggestion.itemName))
			{
				ReapplyPriceWhereSold(cachedSuggestion.itemName, cachedSuggestion.suggestedMax);
			}
		}
	}

	private void ReapplyPriceWhereSold(string itemName, float price)
	{
		bool flag = false;
		foreach (BuildingRegistration supervisedStore in GetSupervisedStores())
		{
			if (DoesStoreStockItem(supervisedStore, itemName) || TryGetPriceFromList(supervisedStore.retailPrices, itemName, out var _))
			{
				SnapshotOriginalPrice(supervisedStore, itemName);
				SetPrice(supervisedStore, itemName, price);
				flag = true;
			}
		}
		if (flag)
		{
			if (appliedPrices == null)
			{
				appliedPrices = new List<RetailPrice>();
			}
			SetPriceInList(appliedPrices, itemName, price);
		}
	}

	public void RecomputeSuggestions()
	{
		cachedSuggestions.Clear();
		if (assignedEmployeeId.IsNullOrEmpty())
		{
			return;
		}
		float normalizedSkill = Mathf.Clamp01(PricingManagerSkillValue / 100f);
		List<BuildingRegistration> supervisedStores = GetSupervisedStores();
		Dictionary<string, float> lowestRivalPrices = BuildLowestRivalPrices();
		foreach (string sellableItem in GetSellableItems(supervisedStores))
		{
			PriceSuggestion priceSuggestion = BuildSuggestion(sellableItem, normalizedSkill, supervisedStores, lowestRivalPrices);
			if (priceSuggestion != null)
			{
				cachedSuggestions.Add(priceSuggestion);
			}
		}
	}

	public void NotifyMispricedItems()
	{
		if (manuallyPricedItems != null && manuallyPricedItems.Count != 0)
		{
			List<BuildingRegistration> stores = GetSupervisedStores();
			int num = manuallyPricedItems.CountWhere((string itemName) => IsPricedAboveCeiling(itemName, stores));
			if (num != 0)
			{
				Notifications.Show(NotificationType.Info, "notifications_PricingManager_mispriced_items", new Dictionary<string, string>
				{
					{
						"employeeName",
						AnalystInstance?.characterData.name
					},
					{
						"amount",
						num.ToString()
					}
				}, 4f, null, OnClickShowPricingManager);
			}
		}
	}

	private void OnClickShowPricingManager()
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.canvas.isActiveAndEnabled)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(headquartersAddress, "PricingManagers");
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.pricingManagersPlanList.SelectPlanById(id);
		}
	}

	private void ApplyPrice(string itemName, float price)
	{
		if (appliedPrices == null)
		{
			appliedPrices = new List<RetailPrice>();
		}
		SetPriceInList(appliedPrices, itemName, price);
		ApplyPriceToStores(itemName, price);
	}

	private void ApplyPriceToStores(string itemName, float price)
	{
		bool flag = false;
		foreach (BuildingRegistration supervisedStore in GetSupervisedStores())
		{
			if (CanStoreSellItem(supervisedStore, itemName))
			{
				SnapshotOriginalPrice(supervisedStore, itemName);
				if (!TryGetPriceFromList(supervisedStore.retailPrices, itemName, out var _))
				{
					flag = true;
				}
				SetPrice(supervisedStore, itemName, price);
			}
		}
		if (flag)
		{
			ProductMarketHelper.UpdateMarketDemand(itemName);
		}
	}

	private void ReapplyPrices()
	{
		if (appliedPrices == null)
		{
			return;
		}
		foreach (RetailPrice appliedPrice in appliedPrices)
		{
			ApplyPriceToStores(appliedPrice.itemName, appliedPrice.price);
		}
	}

	public bool TryGetUniformPrice(string itemName, out float price)
	{
		price = 0f;
		bool flag = false;
		foreach (BuildingRegistration supervisedStore in GetSupervisedStores())
		{
			if (TryGetCurrentPrice(supervisedStore, itemName, out var price2))
			{
				if (!flag)
				{
					price = price2;
					flag = true;
				}
				else if (!Mathf.Approximately(price, price2))
				{
					return false;
				}
			}
		}
		return flag;
	}

	private static bool TryGetCurrentPrice(BuildingRegistration store, string itemName, out float price)
	{
		price = 0f;
		if (!CanStoreSellItem(store, itemName))
		{
			return false;
		}
		if (TryGetPriceFromList(store.storedRetailPrices, itemName, out price))
		{
			return true;
		}
		if (!DoesStoreStockItem(store, itemName))
		{
			return false;
		}
		price = ItemHelper.GetPrice(itemName, store);
		return true;
	}

	private PriceSuggestion BuildSuggestion(string itemName, float normalizedSkill, List<BuildingRegistration> stores, Dictionary<string, float> lowestRivalPrices)
	{
		if (!ItemsGetter.GetByName(itemName).isADemandedProduct)
		{
			return null;
		}
		var (suggestedMin, num) = PricingManagerHelper.ComputeSuggestion(itemName, supervisedNeighborhood, normalizedSkill, RngHelper.StableValue01(id + assignedEmployeeId + itemName));
		if (num <= 0f)
		{
			return null;
		}
		return new PriceSuggestion
		{
			itemName = itemName,
			suggestedMin = suggestedMin,
			suggestedMax = num,
			rivalReferencePrice = lowestRivalPrices.GetValueOrDefault(itemName, -1f),
			isPlayerSelling = IsItemSoldByAnyStore(itemName, stores),
			sellingBusinessTypes = GetSellingBusinessTypes(itemName, stores)
		};
	}

	private static HashSet<string> GetSellingBusinessTypes(string itemName, List<BuildingRegistration> stores)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (BuildingRegistration store in stores)
		{
			if (CanStoreSellItem(store, itemName))
			{
				hashSet.Add(store.businessTypeName);
			}
		}
		return hashSet;
	}

	private Dictionary<string, float> BuildLowestRivalPrices()
	{
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer || buildingRegistration.retailPrices == null || buildingRegistration.Neighborhood != supervisedNeighborhood || BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.hidepriceindex))
			{
				continue;
			}
			List<string> listOfItemsForSale = buildingRegistration.GetListOfItemsForSale();
			if (listOfItemsForSale == null)
			{
				continue;
			}
			foreach (string item in listOfItemsForSale)
			{
				if (!(PlayerItemPurchaser.GetShelfFillState(item, buildingRegistration) <= 0f))
				{
					float price = ItemHelper.GetPrice(item, buildingRegistration);
					if (!dictionary.TryGetValue(item, out var value) || price < value)
					{
						dictionary[item] = price;
					}
				}
			}
		}
		return dictionary;
	}

	private static bool TryGetPriceFromList(List<RetailPrice> prices, string itemName, out float price)
	{
		if (prices != null)
		{
			foreach (RetailPrice price2 in prices)
			{
				if (!(price2.itemName != itemName))
				{
					price = price2.price;
					return true;
				}
			}
		}
		price = 0f;
		return false;
	}

	private List<BuildingRegistration> GetSupervisedStores()
	{
		if (_supervisedStoresCacheFrame == Time.frameCount)
		{
			return _supervisedStoresCache;
		}
		_supervisedStoresCacheFrame = Time.frameCount;
		_supervisedStoresCache = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (IsSupervisedStore(buildingRegistration))
			{
				_supervisedStoresCache.Add(buildingRegistration);
			}
		}
		return _supervisedStoresCache;
	}

	private bool IsSupervisedStore(BuildingRegistration registration)
	{
		if (PricingManagerHelper.IsManageableBusiness(registration))
		{
			return registration.Neighborhood == supervisedNeighborhood;
		}
		return false;
	}

	private static HashSet<string> GetSellableItems(List<BuildingRegistration> stores)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (BuildingRegistration store in stores)
		{
			foreach (string allProduct in BusinessTypeHelper.GetAllProducts(store.businessTypeName))
			{
				hashSet.Add(allProduct);
			}
			if (store.cachedAvailableProducts == null)
			{
				continue;
			}
			foreach (string cachedAvailableProduct in store.cachedAvailableProducts)
			{
				hashSet.Add(cachedAvailableProduct);
			}
		}
		return hashSet;
	}

	private static bool CanStoreSellItem(BuildingRegistration store, string itemName)
	{
		if (DoesStoreStockItem(store, itemName))
		{
			return true;
		}
		return BusinessTypeHelper.GetAllProducts(store.businessTypeName).Contains(itemName);
	}

	private static bool DoesStoreStockItem(BuildingRegistration store, string itemName)
	{
		if (store.cachedAvailableProducts != null)
		{
			return store.cachedAvailableProducts.Contains(itemName);
		}
		return false;
	}

	private static bool IsItemSoldByAnyStore(string itemName, List<BuildingRegistration> stores)
	{
		foreach (BuildingRegistration store in stores)
		{
			if (DoesStoreStockItem(store, itemName))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsPricedAboveCeiling(string itemName, List<BuildingRegistration> stores)
	{
		float num = (float)PricingManagerHelper.GetHighestAcceptableCents(itemName, supervisedNeighborhood) / 100f;
		foreach (BuildingRegistration store in stores)
		{
			if (TryGetPriceFromList(store.retailPrices, itemName, out var price) && price > num)
			{
				return true;
			}
		}
		return false;
	}

	private void SnapshotCurrentPrices()
	{
		if (assignedEmployeeId.IsNullOrEmpty())
		{
			return;
		}
		List<BuildingRegistration> supervisedStores = GetSupervisedStores();
		HashSet<string> sellableItems = GetSellableItems(supervisedStores);
		foreach (BuildingRegistration item in supervisedStores)
		{
			foreach (string item2 in sellableItems)
			{
				if (CanStoreSellItem(item, item2))
				{
					SnapshotOriginalPrice(item, item2);
				}
			}
		}
	}

	private void SnapshotOriginalPrice(BuildingRegistration store, string itemName)
	{
		if (_snapshottedPrices == null)
		{
			_snapshottedPrices = BuildSnapshottedPricesLookup();
		}
		if (!_snapshottedPrices.Add((store.Address, itemName)))
		{
			return;
		}
		OriginalStorePrice originalStorePrice = new OriginalStorePrice
		{
			storeAddress = store.Address,
			itemName = itemName
		};
		foreach (RetailPrice retailPrice in store.retailPrices)
		{
			if (!(retailPrice.itemName != itemName))
			{
				originalStorePrice.hadPrice = true;
				originalStorePrice.price = retailPrice.price;
				break;
			}
		}
		foreach (RetailPrice storedRetailPrice in store.storedRetailPrices)
		{
			if (!(storedRetailPrice.itemName != itemName))
			{
				originalStorePrice.hadStoredPrice = true;
				originalStorePrice.storedPrice = storedRetailPrice.price;
				break;
			}
		}
		originalStorePrices.Add(originalStorePrice);
	}

	private HashSet<(Address, string)> BuildSnapshottedPricesLookup()
	{
		HashSet<(Address, string)> hashSet = new HashSet<(Address, string)>();
		foreach (OriginalStorePrice originalStorePrice in originalStorePrices)
		{
			hashSet.Add((originalStorePrice.storeAddress, originalStorePrice.itemName));
		}
		return hashSet;
	}

	private void RestoreOriginalPrices()
	{
		foreach (OriginalStorePrice originalStorePrice in originalStorePrices)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(originalStorePrice.storeAddress);
			if (buildingRegistration?.retailPrices != null && buildingRegistration.RentedByPlayer)
			{
				if (originalStorePrice.hadPrice)
				{
					SetPriceInList(buildingRegistration.retailPrices, originalStorePrice.itemName, originalStorePrice.price);
				}
				else
				{
					RemovePriceFromList(buildingRegistration.retailPrices, originalStorePrice.itemName);
				}
				if (originalStorePrice.hadStoredPrice)
				{
					SetPriceInList(buildingRegistration.storedRetailPrices, originalStorePrice.itemName, originalStorePrice.storedPrice);
				}
				else
				{
					RemovePriceFromList(buildingRegistration.storedRetailPrices, originalStorePrice.itemName);
				}
			}
		}
		originalStorePrices.Clear();
		_snapshottedPrices = null;
	}

	private static void SetPrice(BuildingRegistration store, string itemName, float price)
	{
		SetPriceInList(store.retailPrices, itemName, price);
		SetPriceInList(store.storedRetailPrices, itemName, price);
	}

	private static void SetPriceInList(List<RetailPrice> prices, string itemName, float price)
	{
		foreach (RetailPrice price2 in prices)
		{
			if (!(price2.itemName != itemName))
			{
				price2.price = price;
				return;
			}
		}
		prices.Add(new RetailPrice
		{
			itemName = itemName,
			price = price
		});
	}

	private static void RemovePriceFromList(List<RetailPrice> prices, string itemName)
	{
		for (int num = prices.Count - 1; num >= 0; num--)
		{
			if (prices[num].itemName == itemName)
			{
				prices.RemoveAt(num);
			}
		}
	}
}
