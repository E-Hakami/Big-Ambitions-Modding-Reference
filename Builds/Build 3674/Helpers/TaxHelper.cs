using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Enums;
using Extensions;
using IngameDebugConsole;
using Localizor;
using Streets;
using UI;
using UI.Smartphone.Apps.Contacts;
using UI.Tasks;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Helpers;

public class TaxHelper : MonoBehaviour
{
	public const float RealEstateTaxesMultiplier = 3.5f;

	public const int DaysToPayOnFirstWarning = 20;

	public const float MinimumIncomeForTaxes = 150000f;

	public const string TaxDeductibleNameKey = "taxDeductibleName";

	public const string TaxDeductibleVehicleKey = "tax_vehicle";

	public const string TaxDeductibleMarketingKey = "tax_marketing";

	public const string TaxDeductibleRentKey = "tax_rent";

	private const int DaysToPayOnSecondWarning = 10;

	private const float LateFeeMultiplier = 1.1f;

	private const float MinimumPayableTaxAmount = 1f;

	private const string IrsContactId = "internal_revenue_service";

	private const string IrsContactDescription = "government";

	private static Address IrsAddress;

	private static Contact IrsContact;

	public static void RunDaily()
	{
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		if (currentUnpaidTaxes != null)
		{
			int num = SaveGameManager.Current.Day - currentUnpaidTaxes.day;
			if (num > 20)
			{
				ApplyLateFee(currentUnpaidTaxes);
			}
			EnsureCurrentTaxesDueDay(currentUnpaidTaxes);
			TodoTask todoTask = GetPayTaxesTask() ?? InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.PayTaxes, GetIRSAddress(), null, null, null, Priority.Low, 0, GetDaysLeft(currentUnpaidTaxes.dueDay));
			if (SaveGameManager.Current.Day > currentUnpaidTaxes.dueDay)
			{
				Tuple<List<string>, List<string>> repossessedItems = ForcePayCurrentTaxes(GetCurrentTaxesToPay(currentUnpaidTaxes), out var remainingBackTaxes);
				AddBackTaxes(remainingBackTaxes);
				SendForcedPayMessage(repossessedItems, SaveGameManager.Current.currentBackTaxes);
				InstanceBehavior<UIs>.Instance.tasksUI.CompleteTodoTask(todoTask);
				SaveGameManager.Current.currentUnpaidTaxes = null;
			}
			else if (num == 21)
			{
				SendUnpaidWarning();
				todoTask.remainingDays = GetDaysLeft(currentUnpaidTaxes.dueDay);
				todoTask.priority = Priority.High;
				InstanceBehavior<UIs>.Instance.tasksUI.UpdateTodoTask(todoTask, todoTask.priority);
			}
			else
			{
				todoTask.remainingDays = GetDaysLeft(currentUnpaidTaxes.dueDay);
				Priority newPriority = todoTask.priority;
				if (todoTask.priority != Priority.High)
				{
					if (num <= 15)
					{
						if (num > 10)
						{
							newPriority = Priority.Medium;
						}
					}
					else
					{
						newPriority = Priority.High;
					}
				}
				InstanceBehavior<UIs>.Instance.tasksUI.UpdateTodoTask(todoTask, newPriority);
			}
		}
		else if (PlayerShouldDoTaxes())
		{
			ExecutePlayerTaxesEvent();
		}
		CollectBackTaxes();
	}

	private static void ExecutePlayerTaxesEvent()
	{
		Taxes taxes = GenerateTaxes();
		GameInstance current = SaveGameManager.Current;
		if (current.currentTaxPeriodDeductibleExpenses == null)
		{
			current.currentTaxPeriodDeductibleExpenses = new List<TaxDeductibleExpense>();
		}
		SaveGameManager.Current.currentTaxPeriodDeductibleExpenses.Clear();
		SaveGameManager.Current.CurrentTaxPeriodGamblingWinnings = 0f;
		SaveGameManager.Current.CurrentTaxPeriodGamblingLosses = 0f;
		if (taxes.totalToPay <= 0f)
		{
			taxes.totalToPay = 0f;
			if ((bool)InstanceBehavior<UIs>.Instance)
			{
				SendTaxNotice(taxes);
			}
			return;
		}
		SaveGameManager.Current.currentUnpaidTaxes = taxes;
		Address iRSAddress = GetIRSAddress();
		if ((bool)InstanceBehavior<UIs>.Instance)
		{
			SendTaxNotice(taxes);
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.PayTaxes, iRSAddress, null, null, null, Priority.Low, 0, 20);
			return;
		}
		Contact iRSContact = GetIRSContact();
		TextMessage textMessage = new TextMessage
		{
			messageKey = "ba:messagetype_taxes",
			additionalData = new AdditionalMessageData
			{
				taxes = taxes
			},
			timestamp = TimeHelper.Now(),
			read = false,
			isNewInteraction = true
		};
		iRSContact.SendMessage(textMessage);
		TasksUI.AddNewTodoTask(TodoTaskType.PayTaxes, iRSAddress, null, null, null, Priority.Low, 0, 20);
	}

	private static bool PlayerShouldDoTaxes()
	{
		int daysPerYear = SaveGameManager.Current.gameVariables.daysPerYear;
		if (SaveGameManager.Current.Day % daysPerYear != 0)
		{
			return false;
		}
		float num = 0f;
		int num2 = SaveGameManager.Current.financialSummaries.Count - daysPerYear;
		if (num2 < 0)
		{
			num2 = 0;
		}
		for (int i = num2; i < SaveGameManager.Current.financialSummaries.Count; i++)
		{
			foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in SaveGameManager.Current.financialSummaries[i].businessIncomeStatements)
			{
				num += businessIncomeStatement.TotalSales;
			}
		}
		return num >= 150000f;
	}

	private static void SendTaxNotice(Taxes taxes)
	{
		GameManager.SendTextMessage(Contact.GetContact("internal_revenue_service", ContactCategoryName.Finance, "government", GetIRSAddress()), "ba:messagetype_taxes", null, null, new AdditionalMessageData
		{
			taxes = taxes
		});
	}

	private static void SendUnpaidWarning()
	{
		Contact contact = Contact.GetContact("internal_revenue_service", ContactCategoryName.Finance, "government", GetIRSAddress());
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"day",
				GetCurrentTaxesDueDay().ToString()
			},
			{
				"address",
				GetIRSAddress().ToFormattedString()
			},
			{
				"amount",
				GetCurrentTaxesToPay(currentUnpaidTaxes).ToCurrencyFormat()
			}
		};
		GameManager.SendTextMessage(contact, "ba:messagetype_contacts_taxes_message_warning", messageData);
	}

	private static Taxes GenerateTaxes()
	{
		float num = SaveGameManager.Current.CurrentTaxPeriodGamblingWinnings - SaveGameManager.Current.CurrentTaxPeriodGamblingLosses;
		if (num < 0f)
		{
			num = 0f;
		}
		List<(string, float)> deductibleExpenses = GetDeductibleExpenses(out var subtotal);
		Taxes taxes = new Taxes
		{
			day = SaveGameManager.Current.Day,
			taxPercentage = SaveGameManager.Current.gameVariables.taxPercentage,
			businessesIncome = new List<(string, float)>(),
			estateTaxes = new List<(string, float)>(),
			deductibleExpenses = deductibleExpenses,
			subtotalDeductibleExpenses = subtotal,
			subtotalGamblingWinnings = num,
			dueDay = SaveGameManager.Current.Day + 20
		};
		int num2 = SaveGameManager.Current.Day - SaveGameManager.Current.gameVariables.daysPerYear + 1;
		int day = SaveGameManager.Current.Day;
		Dictionary<Address, float> dictionary = new Dictionary<Address, float>();
		List<Address> list = new List<Address>();
		foreach (FinancialSummary financialSummary in SaveGameManager.Current.financialSummaries)
		{
			if (financialSummary.dayNumber < num2 || financialSummary.dayNumber > day)
			{
				continue;
			}
			foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in financialSummary.businessIncomeStatements)
			{
				if (dictionary.TryAdd(businessIncomeStatement.Address, 0f))
				{
					list.Add(businessIncomeStatement.Address);
				}
				dictionary[businessIncomeStatement.Address] += businessIncomeStatement.TotalSales;
			}
		}
		foreach (Address item2 in list)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(item2);
			float item = dictionary[item2];
			if (buildingRegistration.RentedByPlayer && !string.IsNullOrEmpty(buildingRegistration.BusinessName))
			{
				taxes.businessesIncome.Add((buildingRegistration.BusinessName, item));
			}
			else
			{
				taxes.businessesIncome.Add((buildingRegistration.Address.ToFormattedString(), item));
			}
		}
		foreach (RealEstate item3 in SaveGameManager.Current.realEstate)
		{
			float num3 = item3.TaxesAmount;
			int num4 = SaveGameManager.Current.Day - item3.purchaseDay;
			if (num4 < SaveGameManager.Current.gameVariables.daysPerYear)
			{
				num3 *= (float)num4 / (float)SaveGameManager.Current.gameVariables.daysPerYear;
			}
			taxes.estateTaxes.Add((item3.address.ToFormattedString(), num3));
		}
		taxes.subtotalRegisteredBusinesses = GetSubtotal(taxes.businessesIncome);
		taxes.subtotalRealEstateTaxes = GetSubtotal(taxes.estateTaxes);
		float num5 = taxes.subtotalRegisteredBusinesses + taxes.subtotalGamblingWinnings - taxes.subtotalDeductibleExpenses;
		if (num5 < 0f)
		{
			num5 = 0f;
		}
		taxes.totalToPay = num5 * ((float)SaveGameManager.Current.gameVariables.taxPercentage / 100f) + taxes.subtotalRealEstateTaxes;
		return taxes;
	}

	public static void TrackTransaction(Transaction transaction)
	{
		if (!transaction.isTaxDeductible)
		{
			return;
		}
		float num = 0f - transaction.amount;
		if (!(num <= 0f))
		{
			string text = transaction.transactionData?.GetValueOrDefault("taxDeductibleName");
			if (string.IsNullOrEmpty(text))
			{
				Debug.LogWarning("Transaction " + transaction.transactionType + " is marked as tax deductible but has no tax deductible name.");
				return;
			}
			Dictionary<string, string> values = TaxDeductibleExpense.CopyValuesForArguments(text, transaction.transactionData, "taxDeductibleName");
			AddDeductibleExpense(text, values, num);
		}
	}

	private static List<(string, float)> GetDeductibleExpenses(out float subtotal)
	{
		List<(string, float)> list = new List<(string, float)>();
		subtotal = 0f;
		List<TaxDeductibleExpense> currentTaxPeriodDeductibleExpenses = SaveGameManager.Current.currentTaxPeriodDeductibleExpenses;
		if (currentTaxPeriodDeductibleExpenses == null)
		{
			return list;
		}
		foreach (TaxDeductibleExpense item in currentTaxPeriodDeductibleExpenses)
		{
			if (!string.IsNullOrEmpty(item.key))
			{
				float num = item.amount;
				if (num < 0f)
				{
					num = 0f - num;
				}
				string deductibleExpenseLabel = GetDeductibleExpenseLabel(item);
				list.Add((deductibleExpenseLabel, num));
				subtotal += num;
			}
		}
		return list;
	}

	private static string GetDeductibleExpenseLabel(TaxDeductibleExpense expense)
	{
		if (!LocalizorManager.IsLocalizedKey(expense.key))
		{
			return expense.key;
		}
		return LocalizorManager.GetLocalization(expense.key, expense.values);
	}

	private static void AddDeductibleExpense(string key, Dictionary<string, string> values, float amount)
	{
		if (string.IsNullOrEmpty(key) || amount <= 0f)
		{
			return;
		}
		GameInstance current = SaveGameManager.Current;
		if (current.currentTaxPeriodDeductibleExpenses == null)
		{
			current.currentTaxPeriodDeductibleExpenses = new List<TaxDeductibleExpense>();
		}
		List<TaxDeductibleExpense> currentTaxPeriodDeductibleExpenses = SaveGameManager.Current.currentTaxPeriodDeductibleExpenses;
		for (int i = 0; i < currentTaxPeriodDeductibleExpenses.Count; i++)
		{
			TaxDeductibleExpense taxDeductibleExpense = currentTaxPeriodDeductibleExpenses[i];
			if (TaxDeductibleExpense.HasMatchingKeyAndValues(taxDeductibleExpense, key, values))
			{
				double num = (double)taxDeductibleExpense.amount + (double)amount;
				if (!(num > 3.4028234663852886E+38))
				{
					taxDeductibleExpense.amount = (float)num;
					return;
				}
			}
		}
		currentTaxPeriodDeductibleExpenses.Add(new TaxDeductibleExpense
		{
			key = key,
			amount = amount,
			values = values
		});
	}

	public static bool PayCurrentTaxes()
	{
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		if (currentUnpaidTaxes == null)
		{
			return true;
		}
		return PayCurrentTaxes(GetCurrentTaxesToPay(currentUnpaidTaxes));
	}

	public static bool PayCurrentTaxes(float amount)
	{
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		if (currentUnpaidTaxes == null)
		{
			return true;
		}
		float currentTaxesToPay = GetCurrentTaxesToPay(currentUnpaidTaxes);
		if (currentTaxesToPay < 1f)
		{
			TodoTask payTaxesTask = GetPayTaxesTask();
			if (payTaxesTask != null)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.CompleteTodoTask(payTaxesTask);
			}
			SaveGameManager.Current.currentUnpaidTaxes = null;
			GameEvent.Invoke(string.Empty);
			return true;
		}
		float num = Mathf.Min(amount, currentTaxesToPay);
		if (num <= 0f || !PayTaxes(num))
		{
			return false;
		}
		SaveGameManager.Current.achievementsData.taxesPaid += num;
		float remainingTaxAmount = GetRemainingTaxAmount(currentTaxesToPay, num);
		if (remainingTaxAmount <= 0f)
		{
			TodoTask payTaxesTask2 = GetPayTaxesTask();
			if (payTaxesTask2 != null)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.CompleteTodoTask(payTaxesTask2);
			}
			SaveGameManager.Current.currentUnpaidTaxes = null;
		}
		else
		{
			currentUnpaidTaxes.totalToPay = (currentUnpaidTaxes.lateFeeApplied ? (remainingTaxAmount / 1.1f) : remainingTaxAmount);
		}
		GameEvent.Invoke(string.Empty);
		return true;
	}

	public static bool PayBackTaxes()
	{
		if (!HasBackTaxesToPay())
		{
			return true;
		}
		return PayBackTaxes(SaveGameManager.Current.currentBackTaxes);
	}

	public static bool PayBackTaxes(float amount)
	{
		if (!HasBackTaxesToPay())
		{
			return true;
		}
		float currentBackTaxes = SaveGameManager.Current.currentBackTaxes;
		if (currentBackTaxes < 1f)
		{
			SaveGameManager.Current.currentBackTaxes = 0f;
			GameEvent.Invoke(string.Empty);
			return true;
		}
		float num = Mathf.Min(amount, currentBackTaxes);
		if (num <= 0f || !PayTaxes(num))
		{
			return false;
		}
		SaveGameManager.Current.achievementsData.taxesPaid += num;
		SaveGameManager.Current.currentBackTaxes = GetRemainingTaxAmount(currentBackTaxes, num);
		GameEvent.Invoke(string.Empty);
		return true;
	}

	public static bool HasCurrentTaxesToPay()
	{
		return SaveGameManager.Current.currentUnpaidTaxes != null;
	}

	public static bool HasBackTaxesToPay()
	{
		return SaveGameManager.Current.currentBackTaxes > 0f;
	}

	public static bool HasAnyTaxesToPay()
	{
		if (!HasCurrentTaxesToPay())
		{
			return HasBackTaxesToPay();
		}
		return true;
	}

	public static int GetCurrentTaxesDueDay()
	{
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		if (currentUnpaidTaxes == null)
		{
			return 0;
		}
		EnsureCurrentTaxesDueDay(currentUnpaidTaxes);
		return currentUnpaidTaxes.dueDay;
	}

	public static float GetCurrentTaxesToPay()
	{
		return GetCurrentTaxesToPay(SaveGameManager.Current.currentUnpaidTaxes);
	}

	public static float GetBackTaxesToPay()
	{
		return SaveGameManager.Current.currentBackTaxes;
	}

	public static float GetCurrentTaxesProgress()
	{
		Taxes currentUnpaidTaxes = SaveGameManager.Current.currentUnpaidTaxes;
		if (currentUnpaidTaxes == null)
		{
			return 0f;
		}
		EnsureCurrentTaxesDueDay(currentUnpaidTaxes);
		int num = (currentUnpaidTaxes.lateFeeApplied ? (currentUnpaidTaxes.day + 20) : currentUnpaidTaxes.day);
		int num2 = (currentUnpaidTaxes.lateFeeApplied ? 10 : 20);
		return Mathf.Clamp01((float)(SaveGameManager.Current.Day - num) / (float)num2) * 100f;
	}

	private static float GetCurrentTaxesToPay(Taxes taxes)
	{
		if (taxes == null)
		{
			return 0f;
		}
		if (!taxes.lateFeeApplied)
		{
			return taxes.totalToPay;
		}
		return taxes.totalToPay * 1.1f;
	}

	private static bool PayTaxes(float amount)
	{
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_taxpayment");
		return GameManager.ChangeMoneySafe(0f - amount, transactionInfo);
	}

	[ConsoleMethod("IRSForcePayment", "Triggers the IRS forced payment of a specific amount", new string[] { })]
	public static void Command_IRSForcePayment(float amountToPay)
	{
		Debug.Log("Forcing IRS to repossess stuff with total value of " + amountToPay.ToShortCurrencyFormat() + "...");
		Tuple<List<string>, List<string>> tuple = ForcePayCurrentTaxes(amountToPay, out var remainingBackTaxes);
		Debug.Log($"Stuff repossessed: {tuple.Item1.Count} items " + $"and {tuple.Item2.Count} vehicles");
		Debug.Log("Remaining back taxes: " + remainingBackTaxes.ToShortCurrencyFormat());
		if (tuple.Item1.Count > 0)
		{
			Debug.Log("Items repossessed:");
			foreach (string item in tuple.Item1)
			{
				Item byName = ItemsGetter.GetByName(item);
				float val = ((byName.GetWholesalePrice() != 0f) ? byName.GetWholesalePrice() : byName.DefaultMarketPrice);
				Debug.Log(item + " (value of " + val.ToShortCurrencyFormat() + ")");
			}
		}
		if (tuple.Item2.Count <= 0)
		{
			return;
		}
		Debug.Log("Vehicles repossessed:");
		foreach (string item2 in tuple.Item2)
		{
			float price = VehicleTypeHelper.GetVehicleType(item2).price;
			Debug.Log(item2 + " (value of " + price.ToShortCurrencyFormat() + ")");
		}
	}

	[ConsoleMethod("IRSPrintTaxesOwedSoFar", "Prints how much tax the player owes so far", new string[] { })]
	public static void Command_IRSPrintTaxesOwedSoFar()
	{
		Taxes taxes = GenerateTaxes();
		Debug.Log("Taxes owed so far: " + taxes.totalToPay.ToShortCurrencyFormat());
	}

	private static Tuple<List<string>, List<string>> ForcePayCurrentTaxes(float amountToPay, out float remainingBackTaxes)
	{
		remainingBackTaxes = amountToPay;
		Tuple<List<string>, List<string>> tuple = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
		VehicleController[] vehicles = UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
		List<(VehicleInstance, float, int)> list = new List<(VehicleInstance, float, int)>();
		for (int i = 0; i < SaveGameManager.Current.VehicleInstances.Count; i++)
		{
			VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances[i];
			if (!(vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId) && !vehicleInstance.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
			{
				list.Add((vehicleInstance, vehicleInstance.GetSellingPrice(), i));
			}
		}
		list.Sort(CompareVehiclePrices);
		foreach (var item in list)
		{
			if (amountToPay <= 0f)
			{
				remainingBackTaxes = 0f;
				return tuple;
			}
			VehicleController vehicleController = GetVehicleController(vehicles, item.Item1);
			tuple.Item2.Add(item.Item1.vehicleTypeName);
			item.Item1.Delete(vehicleController);
			if (item.Item2 > amountToPay)
			{
				remainingBackTaxes = 0f;
				return tuple;
			}
			amountToPay -= item.Item2;
		}
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.GetBuildingType() != "ba:buildingtype_warehouse")
			{
				continue;
			}
			foreach (ItemInstance taxableItem in GetTaxableItems(buildingRegistration.Address))
			{
				if (CollectItem(taxableItem, ref amountToPay, out var repossessedItems))
				{
					tuple.Item1.AddRange(repossessedItems);
					buildingRegistration.RemoveItemInstanceFromBuilding(taxableItem);
					if (amountToPay <= 0f)
					{
						remainingBackTaxes = 0f;
						return tuple;
					}
				}
			}
		}
		foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration2.RentedByPlayer || buildingRegistration2.businessTypeName == "ba:businesstype_empty")
			{
				continue;
			}
			List<ItemInstance> taxableItems = GetTaxableItems(buildingRegistration2.Address);
			if (taxableItems.Count == 0)
			{
				continue;
			}
			foreach (ItemInstance item2 in taxableItems)
			{
				if (CollectItem(item2, ref amountToPay, out var repossessedItems2))
				{
					tuple.Item1.AddRange(repossessedItems2);
					buildingRegistration2.RemoveItemInstanceFromBuilding(item2);
					buildingRegistration2.UpdateEmployeesAssignedWorkStationItems();
					if (amountToPay <= 0f)
					{
						remainingBackTaxes = 0f;
						return tuple;
					}
				}
			}
			buildingRegistration2.UpdateEmployeesAssignedWorkStationItems();
		}
		remainingBackTaxes = amountToPay;
		return tuple;
	}

	private static List<ItemInstance> GetTaxableItems(Address address)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(address).Values)
		{
			if (value.ItemCached.isSpecialGift)
			{
				list.Add(value);
			}
		}
		return list;
	}

	private static bool CollectItem(ItemInstance itemInstance, ref float toPay, out List<string> repossessedItems)
	{
		repossessedItems = new List<string>();
		for (int num = itemInstance.cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = itemInstance.cargoInstances[num];
			if (!cargoInstance.ItemCached.isSpecialGift)
			{
				float worth = cargoInstance.GetWorth();
				itemInstance.RemoveFromCargo(cargoInstance);
				repossessedItems.Add(cargoInstance.itemName);
				if (worth >= toPay)
				{
					toPay = 0f;
					return true;
				}
				toPay -= worth;
			}
		}
		repossessedItems.Add(itemInstance.itemName);
		float worth2 = itemInstance.GetWorth();
		if (worth2 >= toPay)
		{
			toPay = 0f;
			return true;
		}
		toPay -= worth2;
		return toPay <= 0f;
	}

	private static void SendForcedPayMessage(Tuple<List<string>, List<string>> repossessedItems, float backTaxesOwed)
	{
		if (HasRepossessedItems(repossessedItems) || !(backTaxesOwed <= 0f))
		{
			List<string> list = new List<string>(repossessedItems.Item1);
			list.AddRange(repossessedItems.Item2);
			GameManager.SendTextMessage(Contact.GetContact("internal_revenue_service", ContactCategoryName.Finance, "government"), "ba:messagetype_contacts_taxes_repossession_message", null, null, new AdditionalMessageData
			{
				listOfLabels = list,
				backTaxesOwed = backTaxesOwed
			});
		}
	}

	public static Address GetIRSAddress()
	{
		if (IrsAddress == null)
		{
			IrsAddress = BuildingHelper.allBuildings.Find((Building x) => x.SpecialService != null && x.SpecialService.name == "IRS").Address;
		}
		return IrsAddress;
	}

	public static bool IsIrsContactAdded()
	{
		return SaveGameManager.Current.Contacts.Exists((Contact contact) => contact.id == "internal_revenue_service");
	}

	public static bool IsIrsContact(Contact contact)
	{
		if (contact != null)
		{
			return contact.id == "internal_revenue_service";
		}
		return false;
	}

	public static Contact GetIRSContact()
	{
		if (IrsContact == null)
		{
			IrsContact = Contact.GetContact("internal_revenue_service", ContactCategoryName.Finance, "government", GetIRSAddress());
		}
		return IrsContact;
	}

	private static TodoTask GetPayTaxesTask()
	{
		return TodoTask.GetTaskOfType(TodoTaskType.PayTaxes);
	}

	private static void ApplyLateFee(Taxes taxes)
	{
		if (!taxes.lateFeeApplied)
		{
			taxes.lateFeeApplied = true;
			taxes.dueDay = GetLateTaxesDueDay(taxes.day);
		}
	}

	private static void EnsureCurrentTaxesDueDay(Taxes taxes)
	{
		if (taxes.lateFeeApplied)
		{
			taxes.dueDay = GetLateTaxesDueDay(taxes.day);
		}
		else if (taxes.dueDay <= 0)
		{
			taxes.dueDay = taxes.day + 20;
		}
	}

	private static int GetLateTaxesDueDay(int noticeDay)
	{
		return noticeDay + 20 + 10;
	}

	private static void AddBackTaxes(float amount)
	{
		if (!(amount <= 0f))
		{
			SaveGameManager.Current.currentBackTaxes += amount;
		}
	}

	private static void CollectBackTaxes()
	{
		if (HasBackTaxesToPay())
		{
			Tuple<List<string>, List<string>> repossessedItems = ForcePayCurrentTaxes(SaveGameManager.Current.currentBackTaxes, out var remainingBackTaxes);
			SaveGameManager.Current.currentBackTaxes = remainingBackTaxes;
			if (HasRepossessedItems(repossessedItems))
			{
				SendForcedPayMessage(repossessedItems, remainingBackTaxes);
			}
		}
	}

	private static bool HasRepossessedItems(Tuple<List<string>, List<string>> repossessedItems)
	{
		if (repossessedItems.Item1.Count <= 0)
		{
			return repossessedItems.Item2.Count > 0;
		}
		return true;
	}

	private static float GetRemainingTaxAmount(float amount, float amountPaid)
	{
		float num = amount - amountPaid;
		if (!(Math.Round(num, 2) < 1.0))
		{
			return num;
		}
		return 0f;
	}

	private static int GetDaysLeft(int day)
	{
		return Math.Max(0, day - SaveGameManager.Current.Day);
	}

	private static float GetSubtotal(List<(string, float)> values)
	{
		float num = 0f;
		foreach (var value in values)
		{
			num += value.Item2;
		}
		return num;
	}

	private static VehicleController GetVehicleController(VehicleController[] vehicles, VehicleInstance vehicleInstance)
	{
		foreach (VehicleController vehicleController in vehicles)
		{
			if (vehicleController.vehicleInstance == vehicleInstance)
			{
				return vehicleController;
			}
		}
		return null;
	}

	private static int CompareVehiclePrices((VehicleInstance VehicleInstance, float Price, int Index) first, (VehicleInstance VehicleInstance, float Price, int Index) second)
	{
		int num = first.Price.CompareTo(second.Price);
		if (num == 0)
		{
			return first.Index.CompareTo(second.Index);
		}
		return num;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IrsAddress = null;
		IrsContact = null;
	}
}
