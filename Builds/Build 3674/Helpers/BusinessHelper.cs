using System;
using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Factory;
using Buildings.Office.Headquarters;
using Buildings.Retail.Businesses.CinemaTheater;
using Buildings.Retail.Simulation;
using Entities;
using Enums;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UI;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Contacts;
using UI.Smartphone.Apps.MyEmployees;
using UI.Smartphone.Apps.Persona;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace Helpers;

public static class BusinessHelper
{
	private static readonly List<BuildingRegistration> EmployeeStationWarnings = new List<BuildingRegistration>();

	private static readonly List<Order> TempCompletedOrders = new List<Order>();

	private static readonly List<Address> HamptonsBlockerRefreshAddresses = new List<Address>();

	private static readonly List<ItemInstance> EmptyRestockableItems = new List<ItemInstance>();

	public static bool IsTaxDeductibleBusinessBuilding(BuildingRegistration registration)
	{
		string buildingType = registration.GetBuildingType();
		switch (buildingType)
		{
		default:
			return buildingType == "ba:buildingtype_warehouse";
		case "ba:buildingtype_retail":
		case "ba:buildingtype_office":
		case "ba:buildingtype_cinema":
		case "ba:buildingtype_theater":
			return true;
		}
	}

	public static bool IsTaxDeductibleBusinessServiceBuilding(BuildingRegistration registration)
	{
		if (!IsTaxDeductibleBusinessBuilding(registration))
		{
			return registration.businessTypeName == "ba:businesstype_hospital";
		}
		return true;
	}

	public static void RunDaily()
	{
		bool flag = !TutorialHelper.HasCompletedObjective("tutorial_quest_get_some_sleep_objective_4");
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
			if (!buildingRegistration.BuildingOwnedByPlayer && buildingRegistration.RentPerDay > 0f && (!flag || buildingRegistration.GetBuildingType() != "ba:buildingtype_residential"))
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string> { 
				{
					"address",
					buildingRegistration.Address.ToFormattedString()
				} };
				bool num = IsTaxDeductibleBusinessBuilding(buildingRegistration);
				if (num)
				{
					string value = (string.IsNullOrEmpty(buildingRegistration.BusinessName) ? buildingRegistration.Address.ToFormattedString() : buildingRegistration.BusinessName);
					dictionary.Add("rentedName", value);
				}
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_rent", "ba:transactioncategory_rent", dictionary);
				if (num)
				{
					transactionInfo.SetTaxDeductibleName("tax_rent");
				}
				GameManager.ChangeMoneySafe(0f - buildingRegistration.RentPerDay, transactionInfo, SaveGameManager.Current.Day - 1, buildingRegistration.Address, force: true);
			}
			if (data != null && data.HasTag(TagRef.Businesstag.generatesrevenue))
			{
				UpdateSatisfaction(buildingRegistration);
				UpdatePromotion(buildingRegistration);
				CustomerDemandHelper.ReloadCachedFulfilled(buildingRegistration);
			}
			if (buildingRegistration.businessTypeName == "ba:businesstype_factory")
			{
				ProcessDailyFactoryOrders(buildingRegistration);
			}
			else
			{
				ProcessDailyOrders(buildingRegistration);
			}
		}
		SaveGameManager.Current.paidLicensingFeesToday.Clear();
		LicensingFeesHelper.ShownLicensingFeeWarnings.Clear();
		foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration2.RentedByPlayer)
			{
				LicensingFeesHelper.PayLicensingFees(buildingRegistration2);
			}
		}
		CustomerEntriesHelper.UpdateCustomerEntriesForAllPlayerBusinesses();
		LoanHelper.RunDaily();
		MarketingHelper.RunDaily();
		FinancialSummaryHelper.CreateFinancialSummary(SaveGameManager.Current.Day - 1);
		for (int num2 = SaveGameManager.Current.interiorInstallationFirmContracts.Count - 1; num2 >= 0; num2--)
		{
			InteriorInstallationFirmContract interiorInstallationFirmContract = SaveGameManager.Current.interiorInstallationFirmContracts[num2];
			if (interiorInstallationFirmContract.IsInstallationDay)
			{
				interiorInstallationFirmContract.DoInstallation();
			}
		}
		RefreshHamptonsBlockerColliders();
		GameInstance current3 = SaveGameManager.Current;
		if (current3.playerWeeklyIncomeHistory == null)
		{
			current3.playerWeeklyIncomeHistory = new List<Tuple<int, float>>();
		}
		SaveGameManager.Current.playerWeeklyIncomeHistory.RemoveAll((Tuple<int, float> x) => x.Item1 < SaveGameManager.Current.Day - 7);
		SaveGameManager.Current.playerWeeklyIncomeHistory.Add(new Tuple<int, float>(SaveGameManager.Current.Day, FinancialSummaryHelper.GetLastFinancialSummaries(7).Sum((FinancialSummary x) => x.totalProfit)));
		current3 = SaveGameManager.Current;
		if (current3.playerNumberOfBusinessesHistory == null)
		{
			current3.playerNumberOfBusinessesHistory = new List<Tuple<int, int>>();
		}
		SaveGameManager.Current.playerNumberOfBusinessesHistory.RemoveAll((Tuple<int, int> x) => x.Item1 < SaveGameManager.Current.Day - 7);
		SaveGameManager.Current.playerNumberOfBusinessesHistory.Add(new Tuple<int, int>(SaveGameManager.Current.Day, SaveGameManager.Current.BuildingRegistrations.Count((BuildingRegistration x) => x.RentedByPlayer && (BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.generatesrevenue) || x.businessTypeName == "ba:businesstype_factory"))));
		PersonalGoalsUI.UpdatePersonalGoals("ba:gameevent_moneychange");
		FinancialSummaryHelper.CleanupOldSummaries();
		PrivateDriverHelpers.PayForActiveContract();
	}

	private static void RefreshHamptonsBlockerColliders()
	{
		HamptonsBlockerRefreshAddresses.Clear();
		foreach (InteriorInstallationFirmContract interiorInstallationFirmContract in SaveGameManager.Current.interiorInstallationFirmContracts)
		{
			HamptonsBlockerRefreshAddresses.Add(interiorInstallationFirmContract.addressToDoTheInstallation);
		}
		foreach (MovingServiceContract movingServiceContract in SaveGameManager.Current.movingServiceContracts)
		{
			HamptonsBlockerRefreshAddresses.Add(movingServiceContract.originMovingAddress);
			HamptonsBlockerRefreshAddresses.Add(movingServiceContract.destinationMovingAddress);
		}
		foreach (Address hamptonsBlockerRefreshAddress in HamptonsBlockerRefreshAddresses)
		{
			BuildingManager.RefreshHamptonsHouseBlockerCollider(hamptonsBlockerRefreshAddress);
		}
	}

	private static void ProcessDailyOrders(BuildingRegistration registration)
	{
		TempCompletedOrders.Clear();
		TempCompletedOrders.AddRange(registration.unprocessedCompletedOrders.FindAll((Order x) => x.completed && x.entries.Exists((OrderEntry y) => y.paid)));
		IEnumerable<IGrouping<string, OrderEntry>> source = from x in TempCompletedOrders.SelectMany((Order x) => x.entries)
			where x.available && x.priceAccceptable && x.paid
			group x by x.itemName;
		OrderHistoryEntry orderHistoryEntry = new OrderHistoryEntry
		{
			dayNumber = SaveGameManager.Current.Day - 1,
			totalCustomers = TempCompletedOrders.Count,
			itemSales = source.Select((IGrouping<string, OrderEntry> group) => new OrderHistoryEntry.ItemReport(group.Key, group.Count(), group.Sum((OrderEntry e) => e.price), group.Sum((OrderEntry e) => e.wholesalePrice), (from x in @group
				group x by x.price into x
				select new ItemSoldPerPriceEntry
				{
					amount = x.Count(),
					price = x.Key
				}).ToArray())).ToList(),
			hourReports = (from x in TempCompletedOrders
				where x.timestamp != null
				group x by x.timestamp.Hour into x
				select new OrderHistoryEntry.HourReport(x.Key, x.Count())).ToList()
		};
		orderHistoryEntry.totalRevenue = orderHistoryEntry.itemSales.Sum((OrderHistoryEntry.ItemReport x) => x.totalPrice);
		registration.orderHistory.RemoveAll((OrderHistoryEntry x) => x.dayNumber < SaveGameManager.Current.Day - 16);
		registration.orderHistory.Add(orderHistoryEntry);
		Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", registration.BusinessName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_revenue", data);
		GameManager.ChangeMoneySafe(orderHistoryEntry.totalRevenue, transactionInfo, SaveGameManager.Current.Day - 1, registration.Address);
		registration.unprocessedCompletedOrders.Clear();
		TempCompletedOrders.Clear();
	}

	private static void ProcessDailyFactoryOrders(BuildingRegistration registration)
	{
		List<OrderHistoryEntry.ItemReport> list = new List<OrderHistoryEntry.ItemReport>(registration.factoryExports.Count);
		foreach (FactoryExport factoryExport in registration.factoryExports)
		{
			list.Add(new OrderHistoryEntry.ItemReport
			{
				amountSold = factoryExport.amount,
				itemName = factoryExport.itemName,
				totalPrice = factoryExport.totalPrice,
				totalWholesalePrice = factoryExport.totalIngredientsCost
			});
		}
		OrderHistoryEntry orderHistoryEntry = new OrderHistoryEntry
		{
			dayNumber = SaveGameManager.Current.Day - 1,
			itemSales = list
		};
		orderHistoryEntry.totalRevenue = orderHistoryEntry.itemSales.Sum((OrderHistoryEntry.ItemReport x) => x.totalPrice);
		registration.orderHistory.RemoveAll((OrderHistoryEntry x) => x.dayNumber < SaveGameManager.Current.Day - 16);
		registration.orderHistory.Add(orderHistoryEntry);
		registration.factoryExports.Clear();
	}

	private static bool DeliverWholesaleContract(DeliveryContract deliveryContract)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(deliveryContract.wholesaleAddress);
		Contact contact = Contact.GetContact(buildingRegistration, ContactCategoryName.ImportsAndGoods);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(deliveryContract.businessAddress);
		string businessName = buildingRegistration2.BusinessName;
		if (deliveryContract.TotalPricePerDelivery == 0f)
		{
			foreach (DeliveryContractItem item in deliveryContract.items)
			{
				if (item.amount != 0 && ProductMarketHelper.IsProductInMarketEvent(item.itemName, MarketEventType.ProductBackorder, buildingRegistration.Neighborhood))
				{
					Dictionary<string, string> messageData = new Dictionary<string, string>
					{
						{
							"itemName",
							item.itemName.GetLocalization()
						},
						{ "businessName", businessName }
					};
					contact.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_product_on_backorder", messageData));
				}
			}
			return true;
		}
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{ "warehouseName", buildingRegistration.BusinessName },
			{ "businessName", businessName }
		};
		bool num = buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases ?? false;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deliverycontract", data);
		if (num)
		{
			transactionInfo.SetTaxDeductibleName(buildingRegistration.BusinessName);
		}
		float amount = 0f - deliveryContract.TotalPricePerDelivery;
		Address address = buildingRegistration2.Address;
		if (!GameManager.ChangeMoneySafe(amount, transactionInfo, null, address))
		{
			string value = deliveryContract.TotalPricePerDelivery.ToCurrencyFormat();
			Dictionary<string, string> messageData2 = new Dictionary<string, string>
			{
				{ "amount", value },
				{ "businessName", businessName }
			};
			contact.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_delivery_not_enough_funds", messageData2));
			if (!deliveryContract.repeatingOrder)
			{
				deliveryContract.enabled = false;
			}
			else if (deliveryContract.isUrgentOrder)
			{
				deliveryContract.enabled = true;
				deliveryContract.nextDeliveryDay = TimeHelper.CurrentDay + 1;
			}
			return false;
		}
		bool flag = TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay) == DayOfWeekOrdered.Monday;
		foreach (DeliveryContractItem item2 in deliveryContract.items)
		{
			Item byName = ItemsGetter.GetByName(item2.itemName);
			int orderAmount = DeliveryHelper.GetOrderAmount(item2, byName, buildingRegistration);
			if (orderAmount == 0)
			{
				continue;
			}
			Dictionary<string, string> messageData3 = new Dictionary<string, string>
			{
				{
					"itemName",
					byName.itemName.GetLocalization()
				},
				{ "businessName", businessName }
			};
			if (ProductMarketHelper.IsProductInMarketEvent(item2.itemName, MarketEventType.ProductBackorder, buildingRegistration.Neighborhood))
			{
				contact.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_product_on_backorder", messageData3));
				continue;
			}
			float num2 = byName.GetWholesalePrice() * 1.05f;
			CargoInstance cargoToDeliver = new CargoInstance(item2.itemName, orderAmount, num2);
			ItemHelper.DeliverCargoToBuilding(cargoToDeliver, buildingRegistration2, (ItemInstance x) => (x.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0 && x.GetStockInstance().itemName == cargoToDeliver.itemName);
			if (cargoToDeliver.amount > 0)
			{
				ItemHelper.DeliverCargoToBuilding(cargoToDeliver, buildingRegistration2, (ItemInstance x) => x.ItemCached.HasTag(TagRef.Itemtag.isbusinessstorage));
			}
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
			item2.amountOrderedThisWeek += orderAmount - cargoToDeliver.amount;
			if (!flag && !DeliveryHelper.AreWholesaleAndImportLimitsDisabled())
			{
				int val = item2.ItemCached.maxWholesaleOrderAmount - item2.amountOrderedThisWeek;
				item2.amount = Math.Min(item2.amount, val);
			}
			int amount2 = cargoToDeliver.amount;
			if (cargoToDeliver.amount > 0)
			{
				float num3 = num2 * ProductMarketHelper.GetProductMarketEventMultiplier(byName.itemName, buildingRegistration.Neighborhood);
				float num4 = (float)amount2 * num3;
				if (deliveryContract.isUrgentOrder)
				{
					num4 *= DeliveryHelper.GetWholesaleUrgentFeeMultiplier();
				}
				Dictionary<string, string> data2 = new Dictionary<string, string>
				{
					{
						"itemQuantityFormat",
						LocalizationHelper.GetItemLabel(item2.itemName, orderAmount).ToString()
					},
					{ "businessName", businessName }
				};
				TransactionInfo transactionInfo2 = new TransactionInfo("ba:transaction_deliverycontractrefund", data2);
				GameManager.ChangeMoneySafe(num4, transactionInfo2);
				contact.SendMessage(new TextMessage("ba:messagetype_phone_wholesale_store_delivery_no_available_space", messageData3));
			}
		}
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{ "fromname", buildingRegistration.BusinessName },
			{ "toname", buildingRegistration2.BusinessName }
		};
		Notifications.Show(NotificationType.Success, "notification_delivery_contract_arrived", notificationData);
		FillUpEmptyShowcaseShelvesAndPointsOfSales(buildingRegistration2.Address);
		return true;
	}

	public static void RunHourly()
	{
		EmployeeStationWarnings.Clear();
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			Building building = BuildingHelper.GetBuilding(buildingRegistration.Address);
			ScheduleDay todaySchedule = BuildingHelper.GetTodaySchedule(buildingRegistration);
			if (todaySchedule == null)
			{
				continue;
			}
			if (IsAbleToBeWarnedAboutEmployeeStationsBeingEmpty(buildingRegistration, building, todaySchedule))
			{
				if (WarnAboutEmployeeStationsBeingEmptyIfNeeded(buildingRegistration))
				{
					num++;
					if (EmployeeStationWarnings.Count < 2)
					{
						EmployeeStationWarnings.Add(buildingRegistration);
					}
				}
			}
			else
			{
				buildingRegistration.warnedLastHourAboutNoEmployee = false;
			}
			if (buildingRegistration.RentedByPlayer)
			{
				JobBoardCandidateGenerator.GenerateJobBoardCandidateIfNeeded(buildingRegistration);
				if (BuildingTypeHelper.GetData(building).NeedsCleaning)
				{
					CheckIfNeedsToBeCleaned(buildingRegistration);
				}
				if (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
				{
					buildingRegistration.SimulateTheft();
				}
				LicensingFeesHelper.PayLicensingFees(buildingRegistration);
			}
		}
		ShowEmployeeStationWarnings(num);
		HandleMovingContracts();
	}

	public static void UpdateAllSecurityLevels()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft) && BuildingHelper.GetTodaySchedule(buildingRegistration) != null)
			{
				buildingRegistration.UpdateSecurityLevel();
			}
		}
	}

	private static void HandleMovingContracts()
	{
		for (int num = SaveGameManager.Current.movingServiceContracts.Count - 1; num >= 0; num--)
		{
			MovingServiceContract movingServiceContract = SaveGameManager.Current.movingServiceContracts[num];
			if (movingServiceContract.IsMovingDay && movingServiceContract.IsMovingHour)
			{
				movingServiceContract.DoMove();
			}
		}
	}

	public static void HandleWholesaleDeliveries()
	{
		if (8 != SaveGameManager.Current.Hour)
		{
			return;
		}
		bool flag = DayOfWeekOrdered.Monday == TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay);
		foreach (DeliveryContract deliveryContract in SaveGameManager.Current.DeliveryContracts)
		{
			if (deliveryContract.enabled)
			{
				deliveryContract.nextDeliveryDay = DeliveryHelper.EnsureDeliveryDayIsNotInPast(deliveryContract.nextDeliveryDay);
			}
			if (deliveryContract.nextDeliveryDay == TimeHelper.CurrentDay && deliveryContract.enabled)
			{
				deliveryContract.UpdateNextDeliveryDay();
				deliveryContract.enabled = deliveryContract.repeatingOrder;
				if (deliveryContract.HasItemsToDeliver() && DeliverWholesaleContract(deliveryContract))
				{
					deliveryContract.isUrgentOrder = false;
				}
			}
			if (!flag)
			{
				continue;
			}
			foreach (DeliveryContractItem item in deliveryContract.items)
			{
				item.amountOrderedLastWeek = item.amountOrderedThisWeek;
				item.amountOrderedThisWeek = 0;
			}
		}
	}

	private static bool WarnAboutEmployeeStationsBeingEmptyIfNeeded(BuildingRegistration registration)
	{
		bool result = false;
		List<ItemInstance> list = registration.itemInstances.Values.Where((ItemInstance x) => x.ItemCached.assignable && !x.ItemCached.HasTag(TagRef.Itemtag.iscleaningstation)).ToList();
		bool flag = list.Count > 0 && list.All((ItemInstance x) => !EmployeeHelper.IsEmployeeStationEmployedAtHour(registration, x.id, SaveGameManager.Current.Hour));
		if (flag && !registration.warnedLastHourAboutNoEmployee)
		{
			result = true;
		}
		registration.warnedLastHourAboutNoEmployee = flag;
		return result;
	}

	private static void ShowEmployeeStationWarnings(int total)
	{
		if (total > 0)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string headerKey;
			switch (total)
			{
			case 1:
				headerKey = "businesshelper_notification_no_employee_assigned";
				dictionary.Add("name", EmployeeStationWarnings[0].BusinessName);
				break;
			case 2:
				headerKey = "businesshelper_notification_no_employee_assigned_two";
				dictionary.Add("fromname", EmployeeStationWarnings[0].BusinessName);
				dictionary.Add("toname", EmployeeStationWarnings[1].BusinessName);
				break;
			default:
				headerKey = "businesshelper_notification_no_employee_assigned_more";
				dictionary.Add("businessName", EmployeeStationWarnings[0].BusinessName);
				dictionary.Add("amount", (total - 1).ToString());
				break;
			}
			Notifications.Show(NotificationType.Error, headerKey, dictionary, 10f);
			EmployeeStationWarnings.Clear();
		}
	}

	private static bool IsAbleToBeWarnedAboutEmployeeStationsBeingEmpty(BuildingRegistration registration, Building building, ScheduleDay scheduleDay)
	{
		if (registration.RentedByPlayer && !BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.ignoreemptyemployeestationwarnings) && registration.businessTypeName != "ba:businesstype_headquarters" && !registration.temporarilyClosed && TutorialHelper.HasCompletedObjective("tutorial_quest_open_the_store_objective_2") && scheduleDay.isOpen)
		{
			return scheduleDay.openingHourSlots.Exists((OpeningHourSlot x) => SaveGameManager.Current.Hour.InRange(x.startingHour, x.endingHour - 1));
		}
		return false;
	}

	public static void CheckIfNeedsToBeCleaned(BuildingRegistration registration)
	{
		if (TutorialHelper.HasCompletedObjective("tutorial_quest_cleaning_objective_3"))
		{
			float cleanliness = registration.GetCleanliness();
			bool flag = registration.scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek()).workShifts.Exists((WorkShift x) => x.type == WorkShiftType.Cleaning);
			if (!(cleanliness > BuildingCleanlinessHelper.FloorTileCleanlinessStates[0]) && (!flag || !(cleanliness > 50f)))
			{
				Priority priority = ((!(cleanliness > BuildingCleanlinessHelper.FloorTileCleanlinessStates[0])) ? ((cleanliness > BuildingCleanlinessHelper.FloorTileCleanlinessStates[1]) ? Priority.Medium : Priority.High) : Priority.Low);
				InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.DirtyFloors, registration.Address, null, null, null, priority);
			}
		}
	}

	public static void UpdateSatisfaction(BuildingRegistration registration)
	{
		List<Order> list = registration.unprocessedCompletedOrders.FindAll((Order x) => x.completed && x.timestamp != null && x.timestamp.Day >= SaveGameManager.Current.Day - 1);
		if (list.Count == 0)
		{
			return;
		}
		List<OrderEntry> list2 = list.SelectMany((Order x) => x.entries).ToList();
		if (list2.Count == 0)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = Mathf.RoundToInt(list2.Average((OrderEntry x) => (!x.priceAccceptable) ? 0f : 100f));
		List<Order> list3 = list.FindAll((Order y) => y.entries.Exists((OrderEntry e) => e.paid));
		if (list3.Count > 0)
		{
			num = Mathf.RoundToInt(list3.Average((Order x) => x.customerServiceSkill));
			num2 = Mathf.RoundToInt(list3.Average((Order x) => x.cleanliness));
			num3 = Mathf.RoundToInt(list3.Average((Order x) => x.customerDemandScore));
		}
		else if (registration.satisfaction != null)
		{
			num = registration.satisfaction.customerService;
			num2 = registration.satisfaction.cleanliness;
			num3 = registration.satisfaction.facility;
		}
		int overall = (int)new List<int> { num, num4, num2, num3 }.Average();
		registration.satisfaction = new Satisfaction
		{
			customerService = num,
			pricing = num4,
			cleanliness = num2,
			facility = num3,
			overall = overall
		};
	}

	public static void GenerateMissingTodoTasksForBusiness(BuildingRegistration registration)
	{
		if (ShouldGenerateTasks(registration))
		{
			GenerateRequirementTasks(registration, BusinessTypeHelper.GetData(registration));
			GenerateMissingScheduleTask(registration);
			GenerateTemporarilyClosedTask(registration);
			GenerateItemsWithoutStockTasks(registration);
			GenerateIdleEmployeesTasks(registration);
		}
	}

	private static bool ShouldGenerateTasks(BuildingRegistration registration)
	{
		Building building = BuildingHelper.GetBuilding(registration.Address);
		if ((bool)building && registration.RentedByPlayer && !string.IsNullOrEmpty(registration.businessTypeName) && registration.businessTypeName != "ba:businesstype_empty" && !BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.dontgeneratetodotasks) && registration.businessTypeName != "ba:businesstype_warehouse")
		{
			return registration.HasEstablishedBusiness;
		}
		return false;
	}

	private static void GenerateTemporarilyClosedTask(BuildingRegistration registration)
	{
		if (registration.temporarilyClosed)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.BusinessTemporarilyClosed, registration.Address);
		}
	}

	private static void GenerateMissingScheduleTask(BuildingRegistration registration)
	{
		if (!registration.scheduleDays.Any((ScheduleDay x) => x.isOpen))
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.MissingSchedule, registration.Address, null, null, null, Priority.High);
		}
	}

	private static void GenerateRequirementTasks(BuildingRegistration registration, BusinessType businessType)
	{
		foreach (BusinessRequirement businessRequirement in businessType.businessRequirements)
		{
			if (!IsRequirementMet(registration, businessRequirement))
			{
				TodoTaskType todoTaskType = businessRequirement.GetTodoTaskType();
				InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(todoTaskType, registration.Address, businessRequirement.GetTodoTaskItemName(), null, null, Priority.High, 0, 0, businessRequirement.businessRequirementName);
			}
		}
	}

	public static void GenerateItemsWithoutStockTasks(BuildingRegistration registration, List<ItemInstance> itemsInBusiness = null)
	{
		if (itemsInBusiness == null)
		{
			itemsInBusiness = registration.itemInstances.Values.ToList();
		}
		bool flag = BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.customersneedpaperbags);
		foreach (ItemInstance item in itemsInBusiness)
		{
			if (!item.CanRestockItem() || ((item.ItemCached.type & ItemType.PointOfSale) != 0 && !flag))
			{
				continue;
			}
			CargoInstance stockInstance = item.GetStockInstance();
			int maxStockCapacity = stockInstance.GetMaxStockCapacity(item);
			if (maxStockCapacity != 0)
			{
				int num = stockInstance.amount + BuildingHelper.CountTotalResourcesInStockCached(registration, stockInstance.itemName, includeProducers: false, includePalletShelves: false);
				if (stockInstance.amount <= 0 && num > 0)
				{
					CompleteStockTask(TodoTaskType.EmptyStock, item);
					ReStockingHelper.RedistributeStockByPercentage(item);
				}
				else if (num <= 0)
				{
					CompleteStockTask(TodoTaskType.LowStock, item);
					InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.EmptyStock, registration.Address, stockInstance.itemName, item.id, null, Priority.High);
				}
				else if ((float)num <= (float)maxStockCapacity * 0.25f)
				{
					CompleteStockTask(TodoTaskType.EmptyStock, item);
					InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.LowStock, registration.Address, stockInstance.itemName, item.id, null, Priority.Medium);
				}
			}
		}
	}

	private static void CompleteStockTask(TodoTaskType todoTaskType, ItemInstance itemInstance)
	{
		TodoTask todoTask = SaveGameManager.Current.TodoTasks.Find((TodoTask x) => x.type == todoTaskType && x.itemInstanceId == itemInstance.id);
		if (todoTask != null)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteTodoTask(todoTask);
		}
	}

	public static void GenerateIdleEmployeesTasks(BuildingRegistration registration)
	{
		foreach (EmployeeInstance item in from employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = registration.Address
			})
			where employeeInstance.trainingSession == null && !employeeInstance.IsAssignedToAnyWorkShift()
			select employeeInstance)
		{
			item.AddTodoTask(TodoTaskType.EmployeeIdle);
		}
	}

	public static bool IsRequirementMet(BuildingRegistration buildingRegistration, BusinessRequirement requirement)
	{
		try
		{
			return requirement.IsRequirementMet(buildingRegistration);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error in BusinessHelper.IsRequirementMet: {arg}");
			return false;
		}
	}

	public static bool IsRequirementMet(BuildingRegistration buildingRegistration, string requirementName)
	{
		try
		{
			foreach (BusinessRequirement businessRequirement in BusinessTypeHelper.GetData(buildingRegistration).businessRequirements)
			{
				if (businessRequirement.businessRequirementName == requirementName)
				{
					return IsRequirementMet(buildingRegistration, businessRequirement);
				}
			}
			return true;
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error in BusinessHelper.IsRequirementMet: {arg}");
			return false;
		}
	}

	public static bool AreAllRequirementsMet(this BuildingRegistration registration)
	{
		foreach (BusinessRequirement businessRequirement in BusinessTypeHelper.GetData(registration).businessRequirements)
		{
			if (!IsRequirementMet(registration, businessRequirement))
			{
				return false;
			}
		}
		return true;
	}

	public static bool AreWorkCriticalRequirementsMet(this BuildingRegistration registration)
	{
		if (BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.mustfulfillrequirementstowork))
		{
			return registration.AreAllRequirementsMet();
		}
		return true;
	}

	public static bool IsThereAtLeastOnePrimaryProduct(BuildingRegistration buildingRegistration)
	{
		HashSet<string> primaryProducts = BusinessTypeHelper.GetData(buildingRegistration).GetPrimaryProducts();
		if (primaryProducts == null)
		{
			return true;
		}
		foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
		{
			if (primaryProducts.Contains(cachedAvailableProduct) && (ItemsGetter.GetByName(cachedAvailableProduct).type & (ItemType.RetailProduct | ItemType.ServiceProduct)) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public static LanguageChangeEventDataHolder GetBusinessOwnerDescription(BuildingRegistration registration)
	{
		if (!BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.isfromgovernment))
		{
			return "business_rival_owner_description".Localize(new
			{
				rivalName = (registration.RentedByPlayer ? PlayerHelper.CharacterData.name : ((!string.IsNullOrEmpty(registration.businessOwnerRivalId)) ? registration.businessOwnerRivalId.GetRivalName() : "itemname_undefined"))
			});
		}
		return "ownerdescription_government_building".Localize();
	}

	public static LanguageChangeEventDataHolder GetBuildingOwnerDescription(BuildingRegistration registration)
	{
		if (!BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.isfromgovernment))
		{
			return "building_rival_owner_description".Localize(new
			{
				rivalName = (registration.BuildingOwnedByPlayer ? PlayerHelper.CharacterData.name : ((!string.IsNullOrEmpty(registration.buildingOwnerRivalId)) ? registration.buildingOwnerRivalId.GetRivalName() : (registration.IsOnSale() ? "the_city_of_new_york" : (registration.BuildingCached.IsHamptonsAIVilla() ? registration.BuildingCached.hamptonsAIVillaOwner : "ba:itemname_undefined"))))
			});
		}
		return "ownerdescription_government_building".Localize();
	}

	public static void UpdateCustomerCapacity(BuildingRegistration registration)
	{
		if (!registration.HasValidAddress)
		{
			return;
		}
		Building building = BuildingHelper.GetBuilding(registration.Address);
		if (building.GetCustomerCapacity > 0)
		{
			int customerCapacity = BuildingSizeHelper.GetData(building).GetCustomerCapacity(building.BuildingType, building.BuildingVersion);
			registration.customerCapacity = (from entry in registration.itemInstances.Values.GetItemsSortedByCapacity(registration)
				select entry.CustomersLimit).Prepend(customerCapacity).Min();
			registration.UpdateCachedAvailableProducts();
		}
	}

	public static void UpdateCachedAvailableProducts(this BuildingRegistration registration)
	{
		BusinessType data = BusinessTypeHelper.GetData(registration);
		BuildingTypeData data2 = BuildingTypeHelper.GetData(registration);
		if (data.suitableBuildingType == "ba:buildingtype_office")
		{
			registration.cachedAvailableProducts = data.GetPrimaryProducts().ToList();
			registration.RemoveUnusedRetailPrices();
			return;
		}
		bool flag = data2.HasTag(TagRef.Buildingtypetag.includenonphysicalproducts);
		bool flag2 = data2.HasTag(TagRef.Buildingtypetag.nonphysicalfromproducers);
		List<string> list = new List<string>();
		if (flag)
		{
			list = ((!flag2) ? (from x in data.GetPrimaryProducts()
				where (ItemsGetter.GetByName(x).type & ItemType.ServiceProduct) != 0
				select x).ToList() : (from x in (from x in registration.itemInstances.Values.Select((ItemInstance x) => ItemsGetter.GetByName(x.itemName)).Distinct()
					where x.isProducer
					select x).SelectMany((Item x) => x.producerSettings.itemsToProduce)
				where (ItemsGetter.GetByName(x).type & ItemType.ServiceProduct) != 0
				select x).ToList());
		}
		if (data.hasEntranceFee)
		{
			list.Add(data.defaultEntranceFee);
			if (data.hasWeekendOnlyEntranceFee)
			{
				list.Add(data.weekendOnlyEntranceFee);
			}
		}
		List<string> list2 = registration.cachedAvailableProducts.ToList();
		registration.cachedAvailableProducts = (from x in registration.itemInstances.Values
			where (x.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0
			select x.GetStockInstance() into x
			where !string.IsNullOrEmpty(x.itemName) && (x.ItemCached.type & (ItemType.RetailProduct | ItemType.ServiceProduct)) != 0
			select x.itemName).Distinct().ToList();
		if (flag && list.Count > 0)
		{
			registration.cachedAvailableProducts = registration.cachedAvailableProducts.Union(list).ToList();
		}
		string buildingType = registration.GetBuildingType();
		if (buildingType == "ba:buildingtype_cinema")
		{
			registration.cachedAvailableProducts.Insert(0, "ba:itemname_cinematicket");
		}
		if (buildingType == "ba:buildingtype_theater")
		{
			registration.cachedAvailableProducts.Insert(0, "ba:itemname_theaterticket");
		}
		if (registration.HasValidAddress)
		{
			foreach (string previousCachedAvailableProduct in list2)
			{
				if (!registration.cachedAvailableProducts.Contains(previousCachedAvailableProduct))
				{
					NeighborhoodDemand neighborhoodDemand = SaveGameManager.Current.productMarketEntries.FirstOrDefault((ProductMarketEntry x) => x.itemName == previousCachedAvailableProduct)?.demandValues.FirstOrDefault((NeighborhoodDemand x) => x.neighborhood == registration.Neighborhood);
					if (neighborhoodDemand != null)
					{
						neighborhoodDemand.providers--;
						neighborhoodDemand.RecalculateIfPlayerHasMonopoly(previousCachedAvailableProduct);
					}
				}
			}
			foreach (string currentCachedAvailableProduct in registration.cachedAvailableProducts)
			{
				if (!list2.Contains(currentCachedAvailableProduct))
				{
					NeighborhoodDemand neighborhoodDemand2 = SaveGameManager.Current.productMarketEntries.FirstOrDefault((ProductMarketEntry x) => x.itemName == currentCachedAvailableProduct)?.demandValues.FirstOrDefault((NeighborhoodDemand x) => x.neighborhood == registration.Neighborhood);
					if (neighborhoodDemand2 != null)
					{
						neighborhoodDemand2.providers++;
						neighborhoodDemand2.RecalculateIfPlayerHasMonopoly(currentCachedAvailableProduct);
					}
				}
			}
		}
		registration.RemoveUnusedRetailPrices();
	}

	public static void UpdatePromotion(BuildingRegistration reg)
	{
		if (!string.IsNullOrEmpty(reg.businessTypeName) && !(reg.businessTypeName == "ba:businesstype_empty") && reg.HasValidAddress)
		{
			Building building = BuildingHelper.GetBuilding(reg.Address);
			if (BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.hasmarketingpromotion))
			{
				float f = ((BuildingSizeHelper.GetData(building).GetCustomerCapacity(building.BuildingType, building.BuildingVersion) == 0) ? 0f : reg.GetMarketingEfficiency());
				reg.promotion = new Promotion
				{
					trafficIndex = building.trafficIndex,
					marketing = Mathf.RoundToInt(f)
				};
				reg.promotion.total = Math.Min(Mathf.RoundToInt((float)reg.promotion.trafficIndex + (float)reg.promotion.marketing * NeighborhoodHelper.GetData(reg.BuildingCached.Neighbourhood).marketingStrength), 100);
			}
		}
	}

	public static void FillUpEmptyShowcaseShelvesAndPointsOfSales(Address address)
	{
		EmptyRestockableItems.Clear();
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(address).Values)
		{
			if (value.CanRestockItem() && value.GetStockInstance().amount <= 0)
			{
				EmptyRestockableItems.Add(value);
			}
		}
		foreach (ItemInstance emptyRestockableItem in EmptyRestockableItems)
		{
			ReStockingHelper.RedistributeStockByPercentage(emptyRestockableItem);
		}
	}

	public static float GetMaxHcpsqmForRegistration(BuildingRegistration registration)
	{
		HashSet<string> primaryProducts = BusinessTypeHelper.GetData(registration).GetPrimaryProducts();
		if (registration.cachedAvailableProducts.Count <= 0)
		{
			return 0f;
		}
		return (from x in registration.cachedAvailableProducts.Where(primaryProducts.Contains).Select(ItemsGetter.GetByName)
			orderby x.productSalesRatio descending
			select x).FirstOrDefault()?.productSalesRatio ?? 0f;
	}

	public static bool IsBusinessOpen(BuildingRegistration buildingRegistration, int hour = -1)
	{
		if (buildingRegistration == null || buildingRegistration.temporarilyClosed)
		{
			return false;
		}
		ScheduleDay todaySchedule = BuildingHelper.GetTodaySchedule(buildingRegistration);
		if (todaySchedule == null || !todaySchedule.isOpen)
		{
			return false;
		}
		if (hour < 0)
		{
			hour = SaveGameManager.Current.Hour;
		}
		foreach (OpeningHourSlot openingHourSlot in todaySchedule.openingHourSlots)
		{
			if (hour >= openingHourSlot.startingHour && hour < openingHourSlot.endingHour)
			{
				return true;
			}
		}
		return false;
	}

	public static void ShutdownBusiness(BuildingRegistration registration, bool updateMarket = true, bool updateRivals = true)
	{
		BizManSchedule.AbortAutoFillForBusiness(registration);
		if (registration.businessTypeName == "ba:businesstype_headquarters")
		{
			DeleteHQPlans(registration);
		}
		registration.BusinessName = null;
		registration.businessTypeName = "ba:businesstype_empty";
		registration.cachedAvailableProducts.Clear();
		registration.unprocessedCompletedOrders.Clear();
		registration.orderHistory.Clear();
		registration.marketingCampaigns.Clear();
		registration.businessOwnerRivalId = null;
		registration.creationDay = -1;
		registration.stolenItemsCost = 0f;
		registration.retailPrices?.Clear();
		registration.storedRetailPrices?.Clear();
		IEnumerable<TodoTask> enumerable = SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.address == registration.Address);
		if ((bool)InstanceBehavior<UIs>.Instance)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(enumerable);
		}
		else
		{
			foreach (TodoTask item in enumerable)
			{
				SaveGameManager.Current.TodoTasks.Remove(item);
			}
		}
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = registration.Address
		}))
		{
			EmployeeHelper.UnassignEmployeeFromAllWorkshifts(employeeInstance);
		}
		registration.ResetScheduleDays();
		SaveGameManager.Current.DeliveryContracts.RemoveAll((DeliveryContract x) => x.businessAddress == registration.Address);
		SaveGameManager.Current.disabledLicensingFees?.RemoveAll((GameInstance.DisabledLicensingFee x) => x.address == registration.Address);
		LogisticsManagerHelper.CancelAllDeliveriesForAddress(registration.Address);
		LogisticsManagerHelper.ShutdownBusiness(registration.Address);
		if (registration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			PurchasingAgentHelper.CancelPlansThatDeliverToAddress(registration.Address);
		}
		if (updateMarket)
		{
			ProductMarketHelper.UpdateMarketDemands();
		}
		if (updateRivals)
		{
			RivalsHelper.CheckRivalTimelines();
		}
		DeliveryJobStartController.OnBusinessChange(registration.Address);
	}

	public static void DeleteHQPlans(BuildingRegistration registration)
	{
		SaveGameManager.Current.importPartnerships.Where((ImportPartnership x) => x.headquartersAddress == registration.Address).ToList().ForEach(delegate(ImportPartnership x)
		{
			PurchasingAgentHelper.DeletePlan(x.id);
		});
		SaveGameManager.Current.logisticsManagerPlans.Where((LogisticsManagerPlan x) => x.headquartersAddress == registration.Address).ToList().ForEach(delegate(LogisticsManagerPlan x)
		{
			LogisticsManagerHelper.DeletePlan(x.id);
		});
		SaveGameManager.Current.headhunterPlans.Where((HeadhunterPlan x) => x.headquartersAddress == registration.Address).ToList().ForEach(delegate(HeadhunterPlan x)
		{
			HeadhunterHelper.DeletePlan(x.id);
		});
		SaveGameManager.Current.hrManagerPlans.Where((HrManagerPlan x) => x.headquartersAddress == registration.Address).ToList().ForEach(delegate(HrManagerPlan x)
		{
			HrManagerHelper.DeletePlan(x.id);
		});
		List<PricingManagerPlan> pricingManagerPlans = SaveGameManager.Current.pricingManagerPlans;
		for (int num = pricingManagerPlans.Count - 1; num >= 0; num--)
		{
			if (pricingManagerPlans[num].headquartersAddress == registration.Address)
			{
				PricingManagerHelper.DeletePlan(pricingManagerPlans[num].id);
			}
		}
	}

	public static bool IsItemInUse(Address targetBusinessAddress, string productName)
	{
		if (targetBusinessAddress.IsUndefined() || targetBusinessAddress == null)
		{
			return false;
		}
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(targetBusinessAddress).Values)
		{
			if ((value.ItemCached.type & ItemType.ShowcaseShelf) != 0 && value.GetStockInstance().itemName == productName)
			{
				return true;
			}
		}
		return false;
	}

	public static void SetBuildingForRent(BuildingRegistration registration, bool updateMarket = true, bool updateRivals = true)
	{
		ShutdownBusiness(registration, updateMarket, updateRivals);
		registration.AvailableForRent = true;
	}

	public static void RestockCurrentBusinessIfNeeded(BuildingRegistration registration)
	{
		if (registration != null && !InstanceBehavior<UIs>.Instance.timeMachine.isRunning && registration.RentedByPlayer)
		{
			BusinessType data = BusinessTypeHelper.GetData(registration);
			if (data.spawnCustomers && data.simulator is RetailBusinessSimulator retailBusinessSimulator)
			{
				retailBusinessSimulator.SetUp(registration, TimeHelper.GetPreviousHour());
				retailBusinessSimulator.RestockShelvesIfItsTime();
			}
		}
	}

	public static bool CheckIfItemWasSold(BuildingRegistration registration, string itemName)
	{
		for (int i = 0; i < registration.orderHistory.Count; i++)
		{
			OrderHistoryEntry orderHistoryEntry = registration.orderHistory[i];
			for (int j = 0; j < orderHistoryEntry.itemSales.Count; j++)
			{
				if (orderHistoryEntry.itemSales[j].itemName == itemName)
				{
					return true;
				}
			}
		}
		return false;
	}
}
