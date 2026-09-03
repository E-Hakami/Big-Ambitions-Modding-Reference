using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings;
using Buildings.Factory;
using Controllers;
using Entities;
using Extensions;
using IngameDebugConsole;
using Localizor;
using Streets;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Helpers;

public static class ProductMarketHelper
{
	private const string AddressableLabel = "Market";

	private const float ImportPriceIndexBaseDemand = 80f;

	private const float ImportPriceIndexDemandMultiplier = 1.2f;

	private const float ImportPriceIndexRandomMinOffset = -0.1f;

	private const float ImportPriceIndexRandomMaxOffset = 0.2f;

	private const float MinImportPriceIndex = 0.5f;

	private const float MaxImportPriceIndex = 1.3f;

	private const float ExportImportPriceIndexImpact = 0.25f;

	private const int MarketDemandMax = 100;

	private static bool Initialized;

	private static ProductMarketSettings productMarketSettings;

	private static readonly Dictionary<MarketEventType, MarketEventInfo> MarketEventInfos = new Dictionary<MarketEventType, MarketEventInfo>();

	private static Dictionary<string, HashSet<string>> ItemsInWholesalersPerNeighbourhood;

	private static readonly MarketEventType[] ShortageAndBackorderArray = new MarketEventType[2]
	{
		MarketEventType.ProductShortage,
		MarketEventType.ProductBackorder
	};

	private static readonly Dictionary<(string, string), int> ProvidersPerItemPerNeighborhood = new Dictionary<(string, string), int>();

	public static ProductMarketSettings ProductMarketSettings
	{
		get
		{
			if (productMarketSettings == null)
			{
				productMarketSettings = GetProductMarketSettings();
			}
			return productMarketSettings;
		}
	}

	public static void Init()
	{
		if (!Initialized)
		{
			productMarketSettings = GetProductMarketSettings();
			FillMarketEventInfosDictionary();
			GlobalEvents.RegisterOnGameLoadedCallback(delegate
			{
				FillProvidersDictionary();
			});
			Initialized = true;
		}
	}

	public static void RunDaily()
	{
		CreateHypeEventsForNewItemsAddedToANeighbourhood();
		CreateShortagesAfterHypeEvents();
		CreateMaxProvidersExceededEvents();
		ItemHelper.ClearPriceCaches();
	}

	private static void CreateHypeEventsForNewItemsAddedToANeighbourhood()
	{
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			if (!CanItemNameHaveMarketEvent(productMarketEntry.itemName))
			{
				continue;
			}
			foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
			{
				if (!string.IsNullOrEmpty(demandValue.neighborhood))
				{
					if (demandValue.providers > 0 && demandValue.IsHypeEventAvailable())
					{
						string itemName = productMarketEntry.itemName;
						string neighborhood = demandValue.neighborhood;
						ProductMarketEntry entry = productMarketEntry;
						CreateItemRelatedMarketEvent(MarketEventType.Hype, itemName, neighborhood, null, -1, null, -1, entry);
					}
					if (demandValue.providers > 0)
					{
						demandValue.lastDaySold = SaveGameManager.Current.Day;
					}
				}
			}
		}
	}

	private static void CreateShortagesAfterHypeEvents()
	{
		if (!CanGenerateShortages())
		{
			return;
		}
		for (int i = 0; i < SaveGameManager.Current.marketEvents.Count; i++)
		{
			MarketEvent marketEvent = SaveGameManager.Current.marketEvents[i];
			if (marketEvent.type == MarketEventType.Hype && marketEvent.startDay + productMarketSettings.daysToTriggerShortageAfterHypeStarts == TimeHelper.CurrentDay && !IsProductInMarketEvent(marketEvent.itemName, ShortageAndBackorderArray, marketEvent.neighbourhood) && IsItemInWholesaler(marketEvent.neighbourhood, marketEvent.itemName))
			{
				CreateItemRelatedMarketEvent(MarketEventType.ProductShortage, marketEvent.itemName, marketEvent.neighbourhood);
			}
		}
	}

	private static void CreateMaxProvidersExceededEvents()
	{
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			if (!CanItemNameHaveMarketEvent(productMarketEntry.itemName))
			{
				continue;
			}
			foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
			{
				if ((!string.IsNullOrEmpty(demandValue.neighborhood) && !IsItemInWholesaler(demandValue.neighborhood, productMarketEntry.itemName)) || GetProviderRatio(productMarketEntry.itemName, demandValue.neighborhood) <= 1f)
				{
					continue;
				}
				if (demandValue.IsMaxProvidersExceededEventAvailable())
				{
					if (string.IsNullOrEmpty(demandValue.neighborhood))
					{
						Address importAddressContainingItem = GetImportAddressContainingItem(productMarketEntry.itemName);
						if (!importAddressContainingItem.IsUndefined())
						{
							CreateItemRelatedMarketEvent(MarketEventType.MaxProvidersExceeded, productMarketEntry.itemName, demandValue.neighborhood, importAddressContainingItem);
						}
					}
					else
					{
						string itemName = productMarketEntry.itemName;
						string neighborhood = demandValue.neighborhood;
						ProductMarketEntry entry = productMarketEntry;
						CreateItemRelatedMarketEvent(MarketEventType.MaxProvidersExceeded, itemName, neighborhood, null, -1, null, -1, entry);
					}
				}
				demandValue.lastDayProvidersExceeded = SaveGameManager.Current.Day;
			}
		}
	}

	private static bool IsItemInWholesaler(string neighborhood, string itemName)
	{
		FillItemsInWholesalersPerNeighborhoodIfNeeded();
		return ItemsInWholesalersPerNeighbourhood[neighborhood].Contains(itemName);
	}

	private static HashSet<string> GetItemsInWholesalersPerNeighborhood(string neighborhood)
	{
		FillItemsInWholesalersPerNeighborhoodIfNeeded();
		return ItemsInWholesalersPerNeighbourhood[neighborhood];
	}

	public static void OnInitializeCity()
	{
		FillProvidersDictionary();
		UpdateMarketDemands();
	}

	private static ProductMarketSettings GetProductMarketSettings()
	{
		return Addressables.LoadAssetsAsync<ProductMarketSettings>("Market", null).WaitForCompletion().FirstOrDefault();
	}

	private static void FillMarketEventInfosDictionary()
	{
		foreach (MarketEventInfo item in Addressables.LoadAssetsAsync<MarketEventInfo>("Market", null).WaitForCompletion())
		{
			if (!(item == null))
			{
				MarketEventInfos.Add(item.type, item);
			}
		}
	}

	public static MarketEventInfo GetMarketEventInfo(this MarketEventType marketEventType)
	{
		if (MarketEventInfos.Count == 0)
		{
			FillMarketEventInfosDictionary();
		}
		return MarketEventInfos[marketEventType];
	}

	public static void UpdateMarketDemands(GameInstance gameInstance = null)
	{
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			UpdateMarketDemand(allItem.itemName, allItem, gameInstance);
		}
	}

	public static void UpdateMarketDemand(string itemName, Item item = null, GameInstance gameInstance = null)
	{
		if (item == null)
		{
			item = ItemsGetter.GetByName(itemName);
		}
		if (gameInstance == null)
		{
			gameInstance = SaveGameManager.Current;
		}
		if (!CanItemHaveMarketDemand(item))
		{
			return;
		}
		List<string> neighborhoods = NeighborhoodHelper.Neighborhoods;
		ProductMarketEntry productMarketEntry = GetProductMarketEntry(itemName, gameInstance);
		float num = 0f;
		int num2 = 0;
		ProductMarketEntry productMarketEntry2 = productMarketEntry;
		if (productMarketEntry2.demandValues == null)
		{
			productMarketEntry2.demandValues = new List<NeighborhoodDemand>();
		}
		foreach (string neighborhood in neighborhoods)
		{
			if (!CanNeighborhoodHaveItemDemand(neighborhood, itemName))
			{
				continue;
			}
			int providers = GetProviders(itemName, neighborhood);
			int num3 = CalculateDemand(gameInstance, item, neighborhood, providers);
			NeighborhoodDemand neighborhoodDemand = productMarketEntry.demandValues.FirstOrDefault((NeighborhoodDemand x) => x.neighborhood == neighborhood);
			if (neighborhoodDemand != null)
			{
				neighborhoodDemand.demand = num3;
				if (neighborhoodDemand.providers != providers)
				{
					neighborhoodDemand.providers = providers;
					neighborhoodDemand.RecalculateIfPlayerHasMonopoly(itemName);
				}
			}
			else
			{
				neighborhoodDemand = new NeighborhoodDemand
				{
					neighborhood = neighborhood,
					demand = num3,
					providers = providers
				};
				productMarketEntry.demandValues.Add(neighborhoodDemand);
				neighborhoodDemand.RecalculateIfPlayerHasMonopoly(itemName);
			}
			if (!(neighborhood == "ba:neighborhood_global"))
			{
				num += (float)num3;
				if (num3 > 0)
				{
					num2++;
				}
			}
		}
		if ((item.type & ItemType.ServiceProduct) != 0)
		{
			productMarketEntry.importPriceIndex = 0f;
			return;
		}
		float num4 = ((num2 > 0) ? (num / (float)num2) : 0f);
		productMarketEntry.importPriceIndex = ((num4 > 0f) ? (80f / (num4 * 1.2f)) : 1f);
		productMarketEntry.importPriceIndex = UnityEngine.Random.Range(productMarketEntry.importPriceIndex + -0.1f, productMarketEntry.importPriceIndex + 0.2f);
		productMarketEntry.importPriceIndex = Mathf.Clamp(productMarketEntry.importPriceIndex, 0.5f, 1.3f);
		if (item.maxOrderAmountPerImporter > 0)
		{
			productMarketEntry.importPriceIndex = Mathf.Max(0.5f, productMarketEntry.importPriceIndex - (float)GetAmountExportedThisWeek(itemName) / (float)item.maxOrderAmountPerImporter * 0.25f);
		}
	}

	public static bool CanNeighborhoodHaveItemDemand(string neighborhood, string itemName)
	{
		Item byName = ItemsGetter.GetByName(itemName);
		if (!CanItemHaveMarketDemand(byName))
		{
			return false;
		}
		if (byName.limitDemandToNeighbourhoods != null && byName.limitDemandToNeighbourhoods.Length != 0 && Array.IndexOf(byName.limitDemandToNeighbourhoods, neighborhood) == -1)
		{
			return false;
		}
		if (NeighborhoodHelper.GetData(neighborhood).hasOfficeBusinesses)
		{
			return true;
		}
		return !ItemHelper.IsItemSoldInBuildingType(itemName, "ba:buildingtype_office");
	}

	private static ProductMarketEntry GetProductMarketEntry(string itemName, GameInstance gameInstance)
	{
		ProductMarketEntry productMarketEntry = gameInstance.productMarketEntries.FirstOrDefault((ProductMarketEntry x) => x.itemName == itemName);
		if (productMarketEntry != null)
		{
			return productMarketEntry;
		}
		productMarketEntry = new ProductMarketEntry
		{
			itemName = itemName
		};
		gameInstance.productMarketEntries.Add(productMarketEntry);
		return productMarketEntry;
	}

	public static void FillProvidersDictionary(bool checkFillState = true)
	{
		ProvidersPerItemPerNeighborhood.Clear();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.AvailableForRent || buildingRegistration.cachedAvailableProducts.Count <= 0)
			{
				continue;
			}
			Building building = BuildingHelper.GetBuilding(buildingRegistration.Address);
			if (building.SpecialService != null)
			{
				continue;
			}
			foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
			{
				string neighbourhood = building.Neighbourhood;
				if (!ProvidersPerItemPerNeighborhood.ContainsKey((cachedAvailableProduct, neighbourhood)))
				{
					ProvidersPerItemPerNeighborhood[(cachedAvailableProduct, neighbourhood)] = 0;
				}
				if (buildingRegistration.RentedByPlayer || !checkFillState || PlayerItemPurchaser.GetShelfFillState(cachedAvailableProduct, buildingRegistration) > 0f)
				{
					ProvidersPerItemPerNeighborhood[(cachedAvailableProduct, neighbourhood)]++;
				}
			}
		}
	}

	private static int GetProviders(string itemName, string neighborhood)
	{
		return ProvidersPerItemPerNeighborhood.GetValueOrDefault((itemName, neighborhood), 0);
	}

	private static int GetAmountExportedThisWeek(string itemName)
	{
		int num = 0;
		int num2 = (int)(TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay) - 1 + 7) % 7;
		int num3 = TimeHelper.CurrentDay - num2;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			List<FactoryExport> factoryExports = buildingRegistration.factoryExports;
			if (factoryExports != null && factoryExports.Count > 0)
			{
				foreach (FactoryExport factoryExport in buildingRegistration.factoryExports)
				{
					if (factoryExport.itemName == itemName)
					{
						num += factoryExport.amount;
					}
				}
			}
			foreach (OrderHistoryEntry item in buildingRegistration.orderHistory)
			{
				if (item.dayNumber < num3 || item.itemSales == null)
				{
					continue;
				}
				foreach (OrderHistoryEntry.ItemReport itemSale in item.itemSales)
				{
					if (itemSale.itemName == itemName)
					{
						num += itemSale.amountSold;
					}
				}
			}
		}
		return num;
	}

	private static int CalculateDemand(GameInstance gameInstance, Item item, string neighborhood, int providers)
	{
		int optimalProviders = item.GetOptimalProviders();
		int num = 100 - providers * 100 / optimalProviders;
		num = ((num < 30) ? UnityEngine.Random.Range(29, 35) : Mathf.Min(UnityEngine.Random.Range(num - 1, num + 1), 100));
		num += gameInstance.marketEvents.Where((MarketEvent x) => x.IsActive && (string.IsNullOrEmpty(x.neighbourhood) || x.neighbourhood == neighborhood) && x.itemName == item.itemName).Sum((MarketEvent x) => x.demandImpact);
		return Mathf.Min(100, num);
	}

	public static void GenerateHypeEventOnRandomWholesalerItems(string neighborhoodToHype = null, int customDemandBoost = -1)
	{
		if (!string.IsNullOrEmpty(neighborhoodToHype))
		{
			GenerateHypeEventOnRandomWholesalerItemsOnNeighbourhood(neighborhoodToHype, customDemandBoost);
			return;
		}
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (!string.IsNullOrEmpty(neighborhood))
			{
				GenerateHypeEventOnRandomWholesalerItemsOnNeighbourhood(neighborhood, customDemandBoost);
			}
		}
	}

	private static void GenerateHypeEventOnRandomWholesalerItemsOnNeighbourhood(string neighborhoodToHype, int customDemandBoost = -1)
	{
		List<string> second = (from x in SaveGameManager.Current.marketEvents
			where (x.IsActive && x.neighbourhood == neighborhoodToHype) || string.IsNullOrEmpty(x.neighbourhood)
			select x.itemName).ToList();
		foreach (string item in GetItemsInWholesalersPerNeighborhood(neighborhoodToHype).Except(second).GetRandom(2))
		{
			CreateItemRelatedMarketEvent(MarketEventType.Hype, item, neighborhoodToHype, null, -1, (customDemandBoost != -1) ? new int?(customDemandBoost) : ((int?)null));
		}
	}

	public static void GenerateShortagesAndBackorders()
	{
		if (!CanGenerateShortages() && !CanGenerateBackorders())
		{
			RemoveAllLargePurchaseEventsFromLastWeek();
			return;
		}
		GenerateShortagesAndBackorderFromLargePlayerPurchases();
		IList<ProductMarketEntry> productMarketEntries = SaveGameManager.Current.productMarketEntries.ToList().Shuffle();
		GenerateShortagesAndBackordersForWholesalers(productMarketEntries);
		GenerateShortagesAndBackordersForImporters(productMarketEntries);
		RemoveAllLargePurchaseEventsFromLastWeek();
	}

	private static void GenerateShortagesAndBackorderFromLargePlayerPurchases()
	{
		for (int i = 0; i < SaveGameManager.Current.marketEvents.Count; i++)
		{
			MarketEvent marketEvent = SaveGameManager.Current.marketEvents[i];
			if (marketEvent.startDay < TimeHelper.CurrentDay && marketEvent.type == MarketEventType.LargePlayerPurchase && RngHelper.Chance(ProductMarketSettings.chanceOfShortageOrBackOrder))
			{
				bool flag = IsProductInMarketEvent(marketEvent.itemName, MarketEventType.ProductShortage, marketEvent.neighbourhood);
				bool flag2 = IsProductInMarketEvent(marketEvent.itemName, MarketEventType.ProductBackorder, marketEvent.neighbourhood);
				if (CanGenerateShortages() && !flag && !flag2)
				{
					CreateItemRelatedMarketEvent(MarketEventType.ProductShortage, marketEvent.itemName, marketEvent.neighbourhood, marketEvent.address);
				}
				else if (CanGenerateBackorders() & flag)
				{
					CreateItemRelatedMarketEvent(MarketEventType.ProductBackorder, marketEvent.itemName, marketEvent.neighbourhood, marketEvent.address);
					StopMarketEvent(marketEvent.itemName, MarketEventType.ProductShortage, marketEvent.neighbourhood);
				}
			}
		}
	}

	private static void RemoveAllLargePurchaseEventsFromLastWeek()
	{
		for (int num = SaveGameManager.Current.marketEvents.Count - 1; num >= 0; num--)
		{
			MarketEvent marketEvent = SaveGameManager.Current.marketEvents[num];
			if (marketEvent.type == MarketEventType.LargePlayerPurchase && marketEvent.startDay < TimeHelper.CurrentDay)
			{
				SaveGameManager.Current.marketEvents.RemoveAt(num);
			}
		}
	}

	private static void FillItemsInWholesalersPerNeighborhoodIfNeeded()
	{
		if (ItemsInWholesalersPerNeighbourhood != null)
		{
			return;
		}
		ItemsInWholesalersPerNeighbourhood = new Dictionary<string, HashSet<string>>();
		List<BuildingRegistration> source = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_wholesalestore").ToList();
		foreach (string neighbourhood in NeighborhoodHelper.Neighborhoods)
		{
			if (string.IsNullOrEmpty(neighbourhood))
			{
				continue;
			}
			BuildingRegistration buildingRegistration = source.FirstOrDefault((BuildingRegistration x) => x.Neighborhood == neighbourhood);
			if (buildingRegistration == null)
			{
				ItemsInWholesalersPerNeighbourhood[neighbourhood] = new HashSet<string>();
				continue;
			}
			List<string> list = buildingRegistration.cachedAvailableProducts;
			if (list.Count == 0)
			{
				list = buildingRegistration.GetListOfItemsForSale();
			}
			ItemsInWholesalersPerNeighbourhood[neighbourhood] = list.ToHashSet();
		}
	}

	private static bool CanGenerateShortages()
	{
		if (SaveGameManager.Current.gameVariables.difficulty == Difficulty.Custom)
		{
			return true;
		}
		return SaveGameManager.Current.Day >= ProductMarketSettings.startDayOfShortages;
	}

	private static bool CanGenerateBackorders()
	{
		if (SaveGameManager.Current.gameVariables.difficulty == Difficulty.Custom)
		{
			return true;
		}
		return SaveGameManager.Current.Day >= ProductMarketSettings.startDayOfBackOrders;
	}

	private static void GenerateShortagesAndBackordersForWholesalers(IList<ProductMarketEntry> productMarketEntries)
	{
		bool flag = CanGenerateShortages();
		bool flag2 = CanGenerateBackorders();
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (string.IsNullOrEmpty(neighborhood))
			{
				continue;
			}
			int num = 0;
			int num2 = 0;
			foreach (ProductMarketEntry productMarketEntry in productMarketEntries)
			{
				if (num >= ProductMarketSettings.maxShortagesPerAddress && num2 >= ProductMarketSettings.maxBackordersPerAddress)
				{
					break;
				}
				if (!CanItemNameHaveMarketEvent(productMarketEntry.itemName))
				{
					continue;
				}
				float providerRatio = GetProviderRatio(productMarketEntry.itemName, neighborhood);
				if (!(providerRatio > 1f) || !RngHelper.Chance(ProductMarketSettings.chanceOfShortageOrBackOrder) || !IsItemInWholesaler(neighborhood, productMarketEntry.itemName))
				{
					continue;
				}
				bool flag3 = IsProductInMarketEvent(productMarketEntry.itemName, MarketEventType.ProductShortage, neighborhood);
				bool flag4 = IsProductInMarketEvent(productMarketEntry.itemName, MarketEventType.ProductBackorder, neighborhood);
				if (((providerRatio >= 1.5f) & flag2) && num2 < ProductMarketSettings.maxBackordersPerAddress && !flag4)
				{
					if (flag3)
					{
						StopMarketEvent(productMarketEntry.itemName, MarketEventType.ProductShortage, neighborhood);
					}
					CreateItemRelatedMarketEvent(MarketEventType.ProductBackorder, productMarketEntry.itemName, neighborhood);
					num2++;
				}
				else if (flag && num < ProductMarketSettings.maxShortagesPerAddress && !flag3 && !flag4)
				{
					CreateItemRelatedMarketEvent(MarketEventType.ProductShortage, productMarketEntry.itemName, neighborhood);
					num++;
				}
			}
		}
	}

	private static void GenerateShortagesAndBackordersForImporters(IList<ProductMarketEntry> productMarketEntries)
	{
		bool flag = CanGenerateShortages();
		bool flag2 = CanGenerateBackorders();
		Dictionary<Address, (int, int)> dictionary = new Dictionary<Address, (int, int)>();
		List<BuildingRegistration> list = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_importexport").ToList();
		foreach (ProductMarketEntry productMarketEntry in productMarketEntries)
		{
			if (!CanItemNameHaveMarketEvent(productMarketEntry.itemName))
			{
				continue;
			}
			float providerRatio = GetProviderRatio(productMarketEntry.itemName, string.Empty);
			if (!(providerRatio > 1f) || !RngHelper.Chance(ProductMarketSettings.chanceOfShortageOrBackOrder))
			{
				continue;
			}
			Address address = null;
			foreach (BuildingRegistration item3 in list)
			{
				SpecialService specialService = item3.BuildingCached.SpecialService;
				if (!(specialService == null) && specialService.settings is ImportExportSettings importExportSettings && specialService.productsCanGoOnShortage && importExportSettings.GetItemsAvailable().Contains(productMarketEntry.itemName))
				{
					if (!dictionary.TryGetValue(item3.Address, out var value))
					{
						dictionary[item3.Address] = (0, 0);
					}
					else if (value.Item2 >= ProductMarketSettings.maxBackordersPerAddress && value.Item1 >= ProductMarketSettings.maxShortagesPerAddress)
					{
						continue;
					}
					address = item3.Address;
					break;
				}
			}
			if (address.IsUndefined())
			{
				continue;
			}
			bool flag3 = IsProductInMarketEvent(productMarketEntry.itemName, MarketEventType.ProductShortage, string.Empty);
			bool flag4 = IsProductInMarketEvent(productMarketEntry.itemName, MarketEventType.ProductBackorder, string.Empty);
			int item = dictionary[address].Item1;
			int item2 = dictionary[address].Item2;
			if (((providerRatio >= 1.5f) & flag2) && item2 < ProductMarketSettings.maxBackordersPerAddress && !flag4)
			{
				if (flag3)
				{
					StopMarketEvent(productMarketEntry.itemName, MarketEventType.ProductShortage, string.Empty);
				}
				CreateItemRelatedMarketEvent(MarketEventType.ProductBackorder, productMarketEntry.itemName, null, address);
				dictionary[address] = (item, item2 + 1);
			}
			else if (flag && item < ProductMarketSettings.maxShortagesPerAddress && !flag3 && !flag4)
			{
				CreateItemRelatedMarketEvent(MarketEventType.ProductShortage, productMarketEntry.itemName, null, address);
				dictionary[address] = (item + 1, item2);
			}
		}
	}

	private static float GetGlobalProviderRatio(string itemName)
	{
		int num = ItemsGetter.GetByName(itemName).providersNeededToStartGeneratingEvents * NeighborhoodHelper.Neighborhoods.Count - 1;
		int num2 = 0;
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (!string.IsNullOrEmpty(neighborhood))
			{
				num2 += GetProviders(itemName, neighborhood);
			}
		}
		return (float)num2 / (float)num;
	}

	private static float GetNeighborhoodProviderRatio(string itemName, string neighborhood)
	{
		int providersNeededToStartGeneratingEvents = ItemsGetter.GetByName(itemName).providersNeededToStartGeneratingEvents;
		return (float)GetProviders(itemName, neighborhood) / (float)providersNeededToStartGeneratingEvents;
	}

	private static float GetProviderRatio(string itemName, string neighborhood)
	{
		if (!string.IsNullOrEmpty(neighborhood))
		{
			return GetNeighborhoodProviderRatio(itemName, neighborhood);
		}
		return GetGlobalProviderRatio(itemName);
	}

	private static Address GetImportAddressContainingItem(string itemName)
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!(buildingRegistration.businessTypeName != "ba:businesstype_importexport"))
			{
				SpecialService specialService = buildingRegistration.BuildingCached.SpecialService;
				if (!(specialService == null) && specialService.settings is ImportExportSettings importExportSettings && importExportSettings.GetItemsAvailable().Contains(itemName))
				{
					return buildingRegistration.Address;
				}
			}
		}
		return null;
	}

	[ConsoleMethod("CreateMarketEvent", "Instantly creates a market event. Needs the event type, the item name and the neighborhood", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods", "itemName=Items" })]
	public static void Command_CreateMarketEvent(MarketEventType type, string neighborhood, string itemName)
	{
		CreateItemRelatedMarketEvent(type, itemName, neighborhood, null, SaveGameManager.Current.Day);
	}

	public static void CreateItemRelatedMarketEvent(MarketEventType type, string itemName = null, string neighborhood = null, Address address = null, int startDate = -1, int? customDemandBoost = null, int durationInDays = -1, ProductMarketEntry entry = null)
	{
		MarketEventInfo marketEventInfo = type.GetMarketEventInfo();
		int demandImpact = marketEventInfo.demandImpact;
		durationInDays = ((durationInDays == -1) ? marketEventInfo.durationInDays : durationInDays);
		if (customDemandBoost.HasValue)
		{
			demandImpact = customDemandBoost.Value;
		}
		startDate = ((startDate != -1) ? startDate : SaveGameManager.Current.Day);
		if (string.IsNullOrEmpty(itemName))
		{
			List<MarketEvent> source = SaveGameManager.Current.marketEvents.Where((MarketEvent x) => x.IsPlannedOrActive).ToList();
			List<string> currentShortageItems = source.Select((MarketEvent x) => x.itemName).ToList();
			List<string> list = (from x in (from x in BusinessTypeHelper.GetAllPlayerAvailableBusinesses().SelectMany((BusinessType x) => x.businessProducts)
					select x.itemName).Distinct()
				where !currentShortageItems.Contains(x) && CanItemNameHaveMarketEvent(x)
				select x).ToList();
			if (list.Count > 0)
			{
				itemName = list.GetRandom();
			}
		}
		if (!string.IsNullOrEmpty(itemName))
		{
			SaveGameManager.Current.marketEvents.Add(new MarketEvent(type, startDate, durationInDays, neighborhood, address, itemName, demandImpact));
			UpdateMarketDemand(itemName);
		}
	}

	private static bool CanItemNameHaveMarketEvent(string itemName)
	{
		Item byName = ItemsGetter.GetByName(itemName);
		if (byName.GetWholesalePrice() > 0f)
		{
			return (byName.type & ItemType.RetailProduct) != 0;
		}
		return false;
	}

	private static bool CanItemHaveMarketDemand(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if ((item.type & (ItemType.RetailProduct | ItemType.ServiceProduct)) != 0)
		{
			return item.isADemandedProduct;
		}
		return false;
	}

	public static NeighborhoodDemand GetNeighborhoodDemand(string itemName, string neighbourhood)
	{
		if (!CanItemHaveMarketDemand(ItemsGetter.GetByName(itemName)))
		{
			return new NeighborhoodDemand
			{
				neighborhood = neighbourhood,
				demand = 0
			};
		}
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			if (productMarketEntry.itemName != itemName)
			{
				continue;
			}
			foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
			{
				if (demandValue.neighborhood == neighbourhood)
				{
					return demandValue;
				}
			}
			Debug.LogError("No demand found for " + itemName + " in " + neighbourhood.GetLocalization());
			return new NeighborhoodDemand
			{
				neighborhood = neighbourhood,
				demand = 50
			};
		}
		Debug.LogError("No demand found for " + itemName + " in " + neighbourhood.GetLocalization());
		return new NeighborhoodDemand
		{
			neighborhood = neighbourhood,
			demand = 50
		};
	}

	public static float GetProductMarketEventMultiplier(string product, string neighbourhood = null)
	{
		if (SaveGameManager.Current == null || SaveGameManager.Current.marketEvents == null)
		{
			return 1f;
		}
		if (IsProductInMarketEvent(product, MarketEventType.ProductShortage, neighbourhood))
		{
			return ProductMarketSettings.productShortagePriceMultiplier;
		}
		if (IsProductInMarketEvent(product, MarketEventType.ProductBackorder, neighbourhood))
		{
			return ProductMarketSettings.productBackorderPriceMultiplier;
		}
		return 1f;
	}

	public static bool IsProductInMarketEvent(string product, MarketEventType marketEventType, string neighborhood = null, bool ignoreToday = false)
	{
		if (SaveGameManager.Current.marketEvents == null)
		{
			return false;
		}
		foreach (MarketEvent marketEvent in SaveGameManager.Current.marketEvents)
		{
			if (marketEvent.IsActive && (!ignoreToday || marketEvent.startDay != TimeHelper.CurrentDay) && marketEvent.type == marketEventType && !string.IsNullOrEmpty(marketEvent.itemName) && !(marketEvent.neighbourhood != neighborhood) && marketEvent.itemName == product)
			{
				return true;
			}
		}
		return false;
	}

	public static MarketEvent GetMarketEvent(string product, MarketEventType marketEventType, string neighbourhood, bool ignoreToday = false)
	{
		if (SaveGameManager.Current.marketEvents == null)
		{
			return null;
		}
		foreach (MarketEvent marketEvent in SaveGameManager.Current.marketEvents)
		{
			if (marketEvent.IsActive && (!ignoreToday || marketEvent.startDay != TimeHelper.CurrentDay) && marketEvent.type == marketEventType && !string.IsNullOrEmpty(marketEvent.itemName) && !(marketEvent.neighbourhood != neighbourhood) && marketEvent.itemName == product)
			{
				return marketEvent;
			}
		}
		return null;
	}

	public static bool IsProductInMarketEvent(string product, MarketEventType[] marketEventTypes, string neighborhood)
	{
		foreach (MarketEvent marketEvent in SaveGameManager.Current.marketEvents)
		{
			if (marketEvent.IsActive && marketEventTypes.Contains(marketEvent.type) && !string.IsNullOrEmpty(marketEvent.itemName) && !(marketEvent.neighbourhood != neighborhood) && marketEvent.itemName == product)
			{
				return true;
			}
		}
		return false;
	}

	public static void StopMarketEvent(string productItemName, MarketEventType marketEventType, string neighborhood)
	{
		for (int num = SaveGameManager.Current.marketEvents.Count - 1; num >= 0; num--)
		{
			MarketEvent marketEvent = SaveGameManager.Current.marketEvents[num];
			if (marketEvent.IsActive && marketEvent.type == marketEventType && !string.IsNullOrEmpty(marketEvent.itemName) && !(marketEvent.neighbourhood != neighborhood) && !(marketEvent.itemName != productItemName))
			{
				marketEvent.stopped = true;
				marketEvent.dayStopped = TimeHelper.CurrentDay;
			}
		}
	}

	public static bool ShouldShortageMakeShelvesBeEmpty(MarketEvent shortageEvent)
	{
		if (shortageEvent.type != MarketEventType.ProductShortage)
		{
			return false;
		}
		if (shortageEvent.IsActive)
		{
			return shortageEvent.startDay + ProductMarketSettings.startDayToEmptyShelvesWhenThereIsAShortage <= TimeHelper.CurrentDay;
		}
		return false;
	}

	public static ProductMarketEntry GetProductMarketEntry(string itemName)
	{
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			if (!(productMarketEntry.itemName != itemName))
			{
				return productMarketEntry;
			}
		}
		return null;
	}

	public static float GetProductExportPrice(string product)
	{
		float exportMultiplier = SaveGameManager.Current.gameVariables.exportMultiplier;
		return ItemsGetter.GetByName(product).GetWholesalePrice() * exportMultiplier;
	}

	[ConsoleMethod("CreateMarketEvent", "Creates a market event (does not actually open/ close a business)", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods", "itemName=Items" })]
	public static void Command_CreateMarketEvent(MarketEventType type, string neighborhood, string itemName, int duration)
	{
		CreateItemRelatedMarketEvent(type, itemName, neighborhood, null, SaveGameManager.Current.Day, null, duration);
	}

	[ConsoleMethod("CreateMarketEvent", "Creates a market event (does not actually open/ close a business)", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void Command_CreateMarketEvent(MarketEventType type, string neighborhood, int duration)
	{
		CreateItemRelatedMarketEvent(type, null, neighborhood, null, SaveGameManager.Current.Day, null, duration);
	}

	[ConsoleMethod("UpdateMarketDemand", "Updates the market demand", new string[] { }, AutoCompleteMap = new string[] { "itemName=Items" })]
	public static void Command_UpdateMarketDemand(string itemName)
	{
		UpdateMarketDemand(itemName);
	}

	[ConsoleMethod("UpdateMarketDemand", "Updates the market demand", new string[] { })]
	public static void Command_UpdateMarketDemand()
	{
		UpdateMarketDemands();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		MarketEventInfos.Clear();
		ProvidersPerItemPerNeighborhood.Clear();
		ItemsInWholesalersPerNeighbourhood = null;
	}
}
