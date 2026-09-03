using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Extensions;
using Helpers;
using Localizor;
using UI;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Entities;

[Serializable]
public class ImportPartnership
{
	public const int MinDiscountPercentage = 0;

	public const int MaxDiscountPercentage = 25;

	private static int DeliveryBatchCount;

	private static string DeliveryBatchLastBusiness;

	private static Dictionary<string, int> ImporterAmountPerItem = new Dictionary<string, int>();

	private static Dictionary<string, int> ContractAmountPerItem = new Dictionary<string, int>();

	private static HashSet<string> AmountExceededItems = new HashSet<string>();

	private static Dictionary<Address, List<ImportPartnership>> ImportPartnershipsGroupByImporter = new Dictionary<Address, List<ImportPartnership>>();

	public string id = UuidHelper.GenerateBase64Uuid();

	public Address headquartersAddress;

	public Address importAddress;

	public string employeeInstanceId;

	public int nextDeliveryDay;

	public List<ImportProduct> products = new List<ImportProduct>();

	public bool isRepeatingOrder;

	[Obsolete]
	public int daysUntilRepeat;

	public bool isActive;

	public bool isUrgentOrder;

	public bool isTarget;

	public float GetDiscount
	{
		get
		{
			float num = (string.IsNullOrEmpty(employeeInstanceId) ? 0f : EmployeeHelper.GetEmployeeById(employeeInstanceId).GetSkillValue("ba:skill_purchasingagent"));
			return 1f - Mathf.Lerp(0f, 25f, num / 100f) / 100f;
		}
	}

	public EmployeeInstance PurchasingAgentInstance => EmployeeHelper.GetEmployeeById(employeeInstanceId);

	public float NextDeliveryTotal => products.Where((ImportProduct product) => !ProductMarketHelper.IsProductInMarketEvent(product.itemName, MarketEventType.ProductBackorder)).Sum((Func<ImportProduct, float>)GetImportProductTotalPrice);

	public static void DoAllDeliveries()
	{
		GameInstance current = SaveGameManager.Current;
		if (current.itemsOrderedThisWeekByImporter == null)
		{
			current.itemsOrderedThisWeekByImporter = new Dictionary<Address, List<ItemAmountTarget>>();
		}
		bool flag = DayOfWeekOrdered.Monday == TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay);
		bool flag2 = false;
		foreach (KeyValuePair<Address, List<ImportPartnership>> item in GetImportPartnershipsGroupByImporter())
		{
			ImporterAmountPerItem.Clear();
			AmountExceededItems.Clear();
			foreach (ImportPartnership item2 in item.Value)
			{
				flag2 = item2.DoDeliveries() | flag2;
				if (!flag)
				{
					continue;
				}
				foreach (ImportProduct product in item2.products)
				{
					product.amountOrderedLastWeek = product.amountOrderedThisWeek;
					product.amountOrderedThisWeek = 0;
				}
			}
			SendAmountExceededNotification(item.Key);
			CreateLargePurchaseEvents(item.Key);
		}
		if (flag)
		{
			SaveGameManager.Current.itemsOrderedThisWeekByImporter.Clear();
		}
		NotifyDeliveries();
		if (flag2)
		{
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
			if (InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.isPanelOpen)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.ReloadCargo();
			}
		}
	}

	private static Dictionary<Address, List<ImportPartnership>> GetImportPartnershipsGroupByImporter()
	{
		foreach (List<ImportPartnership> value in ImportPartnershipsGroupByImporter.Values)
		{
			value.Clear();
		}
		foreach (ImportPartnership importPartnership in SaveGameManager.Current.importPartnerships)
		{
			Address key = importPartnership.importAddress;
			if (!ImportPartnershipsGroupByImporter.ContainsKey(key))
			{
				ImportPartnershipsGroupByImporter[key] = new List<ImportPartnership>();
			}
			ImportPartnershipsGroupByImporter[key].Add(importPartnership);
		}
		return ImportPartnershipsGroupByImporter;
	}

	public static int GetItemAmountOrderedThisWeek(Address importerAddress, string itemName)
	{
		if (SaveGameManager.Current.itemsOrderedThisWeekByImporter == null || !SaveGameManager.Current.itemsOrderedThisWeekByImporter.ContainsKey(importerAddress))
		{
			return 0;
		}
		return SaveGameManager.Current.itemsOrderedThisWeekByImporter[importerAddress].FirstOrDefault((ItemAmountTarget x) => x.itemName == itemName)?.targetAmount ?? 0;
	}

	public float GetImportProductTotalPrice(ImportProduct importProduct)
	{
		return importProduct.Price * (float)importProduct.GetAmountToBuy(isTarget) * GetDiscount;
	}

	public bool DoDeliveries()
	{
		if (isActive)
		{
			nextDeliveryDay = DeliveryHelper.EnsureDeliveryDayIsNotInPast(nextDeliveryDay);
		}
		if (nextDeliveryDay != TimeHelper.CurrentDay || !isActive)
		{
			return false;
		}
		bool flag = isUrgentOrder;
		SetNextDeliveryDay();
		isActive = isRepeatingOrder;
		isUrgentOrder = false;
		if (products.Count == 0)
		{
			return false;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(importAddress);
		Contact contact = Contact.GetContact(buildingRegistration, ContactCategoryName.ImportsAndGoods);
		EmployeeInstance purchasingAgentInstance = PurchasingAgentInstance;
		if (purchasingAgentInstance == null || purchasingAgentInstance.isBeingReplaced)
		{
			contact.SendMessage(new TextMessage("ba:messagetype_phone_import_partnership_delivery_employee_not_found"));
			if (purchasingAgentInstance == null)
			{
				isActive = false;
			}
			return false;
		}
		float num = 0f;
		ContractAmountPerItem.Clear();
		foreach (ImportProduct product in products)
		{
			Warehouse warehouse = (Warehouse)BuildingHelper.GetBuildingRegistration(product.assignedWarehouse);
			if (warehouse == null || !warehouse.RentedByPlayer || ProductMarketHelper.IsProductInMarketEvent(product.itemName, MarketEventType.ProductBackorder, string.Empty))
			{
				continue;
			}
			int num2 = product.GetAmountToBuy(isTarget);
			if (num2 > 0)
			{
				int num3 = int.MaxValue;
				if (!DeliveryHelper.AreWholesaleAndImportLimitsDisabled() && DeliveryHelper.ShouldLimitImporterMaxAmount(product.itemName, importAddress))
				{
					num3 = GetMaxOrderAmountPerImporter(product.itemName);
				}
				int itemAmountOrderedThisWeek = GetItemAmountOrderedThisWeek(importAddress, product.itemName);
				num3 = Mathf.Max(0, num3 - itemAmountOrderedThisWeek);
				ImporterAmountPerItem.TryGetValue(product.itemName, out var value);
				if (num2 > num3)
				{
					AmountExceededItems.Add(product.itemName);
					num2 = num3;
				}
				ImporterAmountPerItem[product.itemName] = value + num2;
				if (num2 > 0)
				{
					ContractAmountPerItem.TryGetValue(product.itemName, out var value2);
					ContractAmountPerItem[product.itemName] = value2 + num2;
					num += product.Price * (float)num2 * GetDiscount;
				}
			}
		}
		if (num == 0f)
		{
			foreach (ImportProduct product2 in products)
			{
				if (product2.amount != 0 && ProductMarketHelper.IsProductInMarketEvent(product2.itemName, MarketEventType.ProductBackorder, string.Empty))
				{
					Warehouse warehouse2 = (Warehouse)BuildingHelper.GetBuildingRegistration(product2.assignedWarehouse);
					if (warehouse2 != null && warehouse2.RentedByPlayer)
					{
						Dictionary<string, string> messageData = new Dictionary<string, string>
						{
							{
								"itemName",
								product2.itemName.GetLocalization()
							},
							{ "businessName", warehouse2.BusinessName }
						};
						contact.SendMessage(new TextMessage("ba:messagetype_phone_import_product_on_backorder", messageData));
					}
				}
			}
			return false;
		}
		if (flag)
		{
			num *= DeliveryHelper.GetImporterUrgentFeeMultiplier();
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
		bool num4 = buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases ?? false;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_importdelivery", data);
		if (num4)
		{
			transactionInfo.SetTaxDeductibleName("tax_import");
		}
		if (!GameManager.ChangeMoneySafe(0f - num, transactionInfo))
		{
			string value3 = num.ToCurrencyFormat();
			Dictionary<string, string> messageData2 = new Dictionary<string, string> { { "amount", value3 } };
			contact.SendMessage(new TextMessage("ba:messagetype_phone_import_partnership_delivery_not_enough_funds", messageData2));
			if (!isRepeatingOrder)
			{
				isActive = false;
			}
			else if (flag)
			{
				isActive = true;
				isUrgentOrder = true;
				nextDeliveryDay = TimeHelper.CurrentDay + 1;
			}
			return false;
		}
		foreach (ImportProduct importProduct in products)
		{
			ContractAmountPerItem.TryGetValue(importProduct.itemName, out var value4);
			if (value4 <= 0)
			{
				continue;
			}
			Warehouse warehouse3 = (Warehouse)BuildingHelper.GetBuildingRegistration(importProduct.assignedWarehouse);
			if (warehouse3 == null || !warehouse3.RentedByPlayer)
			{
				continue;
			}
			string businessName = warehouse3.BusinessName;
			CargoInstance cargoInstance = new CargoInstance(importProduct.itemName, value4, importProduct.Price * GetDiscount);
			ItemHelper.DeliverCargoToBuilding(cargoInstance, warehouse3, (ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage));
			int num5 = value4 - cargoInstance.amount;
			if (num5 > 0)
			{
				DeliveryTransaction deliveryTransaction = new DeliveryTransaction();
				deliveryTransaction.SetItems(new List<DeliveryItem>
				{
					new DeliveryItem(importProduct.itemName, num5)
				});
				warehouse3.deliveryTransactions.Add(deliveryTransaction);
				importProduct.amountOrderedThisWeek += num5;
				Dictionary<Address, List<ItemAmountTarget>> itemsOrderedThisWeekByImporter = SaveGameManager.Current.itemsOrderedThisWeekByImporter;
				if (!itemsOrderedThisWeekByImporter.TryGetValue(importAddress, out var value5))
				{
					value5 = new List<ItemAmountTarget>();
					itemsOrderedThisWeekByImporter[importAddress] = value5;
				}
				ItemAmountTarget itemAmountTarget = value5.FirstOrDefault((ItemAmountTarget x) => x.itemName == importProduct.itemName);
				if (itemAmountTarget == null)
				{
					itemAmountTarget = new ItemAmountTarget(importProduct.itemName, num5);
					value5.Add(itemAmountTarget);
				}
				else
				{
					itemAmountTarget.targetAmount += num5;
				}
			}
			if (cargoInstance.amount > 0)
			{
				ImporterAmountPerItem[importProduct.itemName] -= cargoInstance.amount;
				float num6 = (float)cargoInstance.amount * importProduct.Price * GetDiscount;
				if (flag)
				{
					num6 *= DeliveryHelper.GetImporterUrgentFeeMultiplier();
				}
				string itemName = importProduct.itemName;
				Dictionary<string, string> data2 = new Dictionary<string, string>
				{
					{
						"itemQuantityFormat",
						LocalizationHelper.GetItemLabel(itemName, cargoInstance.amount).ToString()
					},
					{ "warehouseName", businessName },
					{ "businessName", buildingRegistration.BusinessName }
				};
				TransactionInfo transactionInfo2 = new TransactionInfo("ba:transaction_importdeliveryrefund", data2);
				float amount = num6;
				Address address = warehouse3.Address;
				GameManager.ChangeMoneySafe(amount, transactionInfo2, null, address);
				Dictionary<string, string> messageData3 = new Dictionary<string, string>
				{
					{
						"itemName",
						itemName.GetLocalization()
					},
					{ "businessName", businessName }
				};
				contact.SendMessage(new TextMessage("ba:messagetype_phone_import_partnership_delivery_no_available_space", messageData3), notify: false);
			}
		}
		DeliveryBatchCount++;
		DeliveryBatchLastBusiness = buildingRegistration.BusinessName;
		return true;
	}

	private static int GetMaxOrderAmountPerImporter(string itemName)
	{
		int num = ItemsGetter.GetByName(itemName).maxOrderAmountPerImporter;
		if (ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.ProductShortage))
		{
			num = Mathf.RoundToInt((float)num * 0.66f);
		}
		return num;
	}

	private static void SendAmountExceededNotification(Address address)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (AmountExceededItems.Count > 0)
		{
			Contact contact = Contact.GetContact(buildingRegistration, ContactCategoryName.ImportsAndGoods);
			string value = string.Join(", ", AmountExceededItems.Select((string x) => x.GetLocalization()));
			Dictionary<string, string> messageData = new Dictionary<string, string> { { "products", value } };
			contact.SendMessage(new TextMessage("ba:messagetype_phone_import_partnership_delivery_weekly_limit_exceeded", messageData));
		}
	}

	private static void CreateLargePurchaseEvents(Address importAddress)
	{
		foreach (KeyValuePair<string, int> item in ImporterAmountPerItem)
		{
			if (IsLargePurchase(item.Key, item.Value))
			{
				CreateLargePurchaseEventIfNeeded(item.Key, importAddress);
			}
		}
	}

	private static void CreateLargePurchaseEventIfNeeded(string itemName, Address importAddress)
	{
		if (!ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.LargePlayerPurchase))
		{
			ProductMarketHelper.CreateItemRelatedMarketEvent(MarketEventType.LargePlayerPurchase, itemName, null, importAddress);
		}
	}

	private static bool IsLargePurchase(string itemName, int amountOrdered)
	{
		if (!DeliveryHelper.ShouldLimitImporterMaxAmount(itemName))
		{
			return false;
		}
		int maxOrderAmountPerImporter = GetMaxOrderAmountPerImporter(itemName);
		return (float)amountOrdered / (float)maxOrderAmountPerImporter >= (float)ProductMarketHelper.ProductMarketSettings.percentageOrderedToConsiderALargePurchase / 100f;
	}

	private void SetNextDeliveryDay()
	{
		nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay();
	}

	public void AddMissingImportProducts()
	{
		foreach (string itemName in ((ImportExportSettings)BuildingHelper.GetBuilding(importAddress).SpecialService.settings).GetItemsAvailable())
		{
			if (!products.Any((ImportProduct x) => x.itemName == itemName))
			{
				ImportProduct item = new ImportProduct
				{
					itemName = itemName
				};
				products.Add(item);
			}
		}
	}

	public void UnAssignEmployee()
	{
		employeeInstanceId = null;
		isActive = false;
		isUrgentOrder = false;
		SaveGameManager.MarkChange();
	}

	private static void NotifyDeliveries()
	{
		if (DeliveryBatchCount != 0)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string headerKey;
			if (DeliveryBatchCount > 1)
			{
				headerKey = "import_partnership_notification_shipment_multiple";
				dictionary["amount"] = DeliveryBatchCount.ToString();
			}
			else
			{
				headerKey = "import_partnership_notification_shipment_arrived";
				dictionary["name"] = DeliveryBatchLastBusiness;
			}
			Notifications.Show(NotificationType.Success, headerKey, dictionary);
			DeliveryBatchCount = 0;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DeliveryBatchCount = 0;
		DeliveryBatchLastBusiness = null;
		ImporterAmountPerItem.Clear();
		ContractAmountPerItem.Clear();
		AmountExceededItems.Clear();
		ImportPartnershipsGroupByImporter.Clear();
	}
}
