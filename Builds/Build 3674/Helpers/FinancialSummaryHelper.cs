using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using UnityEngine;

namespace Helpers;

public static class FinancialSummaryHelper
{
	public static FinancialSummary CreateFinancialSummary(int dayNumber)
	{
		List<FinancialSummary.BusinessIncomeStatement> list = new List<FinancialSummary.BusinessIncomeStatement>();
		List<FinancialSummary.ResidentialStatement> list2 = new List<FinancialSummary.ResidentialStatement>();
		List<FinancialSummary.RealEstateStatement> list3 = new List<FinancialSummary.RealEstateStatement>();
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			if (BuildingHelper.GetBuilding(buildingRegistration.Address).BuildingType == "ba:buildingtype_residential")
			{
				if (!buildingRegistration.BuildingOwnedByPlayer)
				{
					FinancialSummary.ResidentialStatement residentialStatement = CreateResidentialStatement(buildingRegistration);
					list2.Add(residentialStatement);
					num2 += residentialStatement.Amount;
				}
			}
			else
			{
				FinancialSummary.BusinessIncomeStatement businessIncomeStatement = CreateBusinessIncomeStatement(buildingRegistration, dayNumber);
				list.Add(businessIncomeStatement);
				num += businessIncomeStatement.TotalProfit;
			}
		}
		foreach (RealEstate item in SaveGameManager.Current.realEstate)
		{
			if (!item.Building.IsHamptonsHouse())
			{
				FinancialSummary.RealEstateStatement realEstateStatement = CreateRealEstateStatement(item);
				list3.Add(realEstateStatement);
				num3 += realEstateStatement.Amount;
			}
		}
		if (TutorialHelper.IsTutorialEnabled() && !SaveGameManager.Current.CompletedQuestEntries.Contains("tutorial_quest_get_some_sleep_objective_4"))
		{
			list2.Clear();
			num2 = 0f;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		float num4 = 0f;
		foreach (Transaction transaction in SaveGameManager.Current.Transactions)
		{
			if (transaction.timestamp.Day != dayNumber)
			{
				continue;
			}
			float amount = transaction.amount;
			if (transaction.transactionType == "ba:transaction_unassignedwage")
			{
				num4 += amount;
			}
			List<string> transactionCategories = transaction.transactionCategories;
			if (transactionCategories == null)
			{
				continue;
			}
			for (int i = 0; i < transactionCategories.Count; i++)
			{
				string key = transactionCategories[i];
				if (!dictionary.TryAdd(key, amount))
				{
					dictionary[key] += amount;
				}
			}
		}
		FinancialSummary financialSummary = new FinancialSummary
		{
			businessIncomeStatements = list,
			residentialStatements = list2,
			realEstateStatements = list3,
			totalBusinessProfit = num,
			totalResidentialExpenses = num2,
			totalRealEstate = num3
		};
		dictionary.TryGetValue("ba:transactioncategory_loanexpenses", out financialSummary.totalLoanExpenses);
		dictionary.TryGetValue("ba:transactioncategory_healthinsuranceexpenses", out financialSummary.totalHealthInsuranceExpenses);
		dictionary.TryGetValue("ba:transactioncategory_headhunterreplacementfees", out financialSummary.totalHeadhunterReplacementFees);
		dictionary.TryGetValue("ba:transactioncategory_parkingfees", out financialSummary.parkingFees);
		dictionary.TryGetValue("ba:transactioncategory_salaryincome", out financialSummary.salaryIncome);
		financialSummary.totalUnassignedStaffWages = num4;
		UpdateIncomeStatementTotalProfit(financialSummary);
		financialSummary.dayNumber = dayNumber;
		SaveGameManager.Current.financialSummaries.RemoveAll((FinancialSummary x) => x.dayNumber == dayNumber);
		SaveGameManager.Current.financialSummaries.Add(financialSummary);
		return financialSummary;
	}

	public static void UpdateIncomeStatementTotalProfit(FinancialSummary summary)
	{
		summary.totalProfit = CalculateIncomeStatementTotalProfit(summary);
	}

	private static float CalculateIncomeStatementTotalProfit(FinancialSummary summary)
	{
		return summary.totalBusinessProfit + summary.totalRealEstate - summary.totalResidentialExpenses + summary.totalLoanExpenses + summary.parkingFees + summary.totalUnassignedStaffWages + summary.salaryIncome + summary.totalHealthInsuranceExpenses + summary.totalHeadhunterReplacementFees;
	}

	private static FinancialSummary.ResidentialStatement CreateResidentialStatement(BuildingRegistration buildingRegistration)
	{
		return new FinancialSummary.ResidentialStatement
		{
			Address = buildingRegistration.Address,
			Amount = buildingRegistration.RentPerDay
		};
	}

	private static FinancialSummary.RealEstateStatement CreateRealEstateStatement(RealEstate realEstate)
	{
		return new FinancialSummary.RealEstateStatement
		{
			Address = realEstate.address,
			Amount = realEstate.DailyIncome
		};
	}

	private static FinancialSummary.BusinessIncomeStatement CreateBusinessIncomeStatement(BuildingRegistration registration, int dayNumber)
	{
		OrderHistoryEntry orderHistoryEntry = null;
		for (int i = 0; i < registration.orderHistory.Count; i++)
		{
			OrderHistoryEntry orderHistoryEntry2 = registration.orderHistory[i];
			if (orderHistoryEntry2.dayNumber == dayNumber)
			{
				orderHistoryEntry = orderHistoryEntry2;
				break;
			}
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		Address address = registration.Address;
		foreach (Transaction transaction in SaveGameManager.Current.Transactions)
		{
			if (transaction.timestamp.Day == dayNumber && !(transaction.address != address))
			{
				List<string> transactionCategories = transaction.transactionCategories;
				if (transactionCategories != null && transactionCategories.Contains("ba:transactioncategory_salaryexpenses"))
				{
					num += transaction.amount;
				}
				List<string> transactionCategories2 = transaction.transactionCategories;
				if (transactionCategories2 != null && transactionCategories2.Contains("ba:transactioncategory_rent"))
				{
					num2 += transaction.amount;
				}
				List<string> transactionCategories3 = transaction.transactionCategories;
				if (transactionCategories3 != null && transactionCategories3.Contains("ba:transactioncategory_marketing"))
				{
					num3 += transaction.amount;
				}
				List<string> transactionCategories4 = transaction.transactionCategories;
				if (transactionCategories4 != null && transactionCategories4.Contains("ba:transactioncategory_licensingfees"))
				{
					num4 += transaction.amount;
				}
			}
		}
		FinancialSummary.BusinessIncomeStatement businessIncomeStatement = new FinancialSummary.BusinessIncomeStatement
		{
			Address = address,
			Resources = new List<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry>(),
			Sales = new List<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry>(),
			SalaryExpenses = Mathf.Abs(num),
			RentExpenses = Mathf.Abs(num2),
			MarketingExpenses = Mathf.Abs(num3),
			LicensingFees = Mathf.Abs(num4),
			Theft = registration.stolenItemsCost
		};
		float num5 = 0f;
		float num6 = 0f;
		if (orderHistoryEntry != null)
		{
			foreach (FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry businessResourcePurchase in GetBusinessResourcePurchases(orderHistoryEntry))
			{
				businessIncomeStatement.Resources.Add(businessResourcePurchase);
				num5 += businessResourcePurchase.Amount;
			}
			foreach (FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry businessSale in GetBusinessSales(orderHistoryEntry))
			{
				businessIncomeStatement.Sales.Add(businessSale);
				num6 += businessSale.Amount;
			}
		}
		registration.stolenItemsCost = 0f;
		businessIncomeStatement.TotalOngoing = businessIncomeStatement.SalaryExpenses + businessIncomeStatement.RentExpenses + businessIncomeStatement.MarketingExpenses + businessIncomeStatement.LicensingFees + businessIncomeStatement.Theft;
		businessIncomeStatement.TotalResources = num5;
		businessIncomeStatement.TotalSales = num6;
		businessIncomeStatement.TotalProfit = businessIncomeStatement.TotalSales - businessIncomeStatement.TotalResources - businessIncomeStatement.TotalOngoing;
		return businessIncomeStatement;
	}

	private static IEnumerable<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry> GetBusinessSales(OrderHistoryEntry historyEntry)
	{
		foreach (OrderHistoryEntry.ItemReport item in historyEntry.itemSales.OrderByDescending((OrderHistoryEntry.ItemReport x) => x.amountSold))
		{
			yield return new FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry
			{
				ItemName = item.itemName,
				Amount = item.totalPrice
			};
		}
	}

	private static IEnumerable<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry> GetBusinessResourcePurchases(OrderHistoryEntry historyEntry)
	{
		foreach (OrderHistoryEntry.ItemReport item in historyEntry.itemSales.OrderByDescending((OrderHistoryEntry.ItemReport x) => x.amountSold))
		{
			if ((ItemsGetter.GetByName(item.itemName).type & ItemType.ServiceProduct) == 0)
			{
				yield return new FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry
				{
					ItemName = item.itemName,
					Amount = item.totalWholesalePrice
				};
			}
		}
	}

	public static void CleanupOldSummaries()
	{
		if (SaveGameManager.Current.Day >= SaveGameManager.Current.gameVariables.daysPerYear)
		{
			int oldestDay = SaveGameManager.Current.Day - SaveGameManager.Current.gameVariables.daysPerYear - 1;
			SaveGameManager.Current.financialSummaries.RemoveAll((FinancialSummary x) => x.dayNumber < oldestDay);
		}
	}

	public static List<FinancialSummary> GetLastFinancialSummaries(int numberOfDays)
	{
		if (numberOfDays > SaveGameManager.Current.gameVariables.daysPerYear)
		{
			throw new Exception($"We don't save financial statements for more than {SaveGameManager.Current.gameVariables.daysPerYear} days.");
		}
		List<FinancialSummary> list = new List<FinancialSummary>();
		int i;
		for (i = 1; i <= numberOfDays; i++)
		{
			FinancialSummary financialSummary = SaveGameManager.Current.financialSummaries.Find((FinancialSummary x) => x.dayNumber == SaveGameManager.Current.Day - i);
			if (financialSummary == null)
			{
				financialSummary = new FinancialSummary();
			}
			list.Add(financialSummary);
		}
		return list;
	}

	public static int ProductsSoldLastWeek(string itemName)
	{
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				num += ProductsSoldLastWeekInRegistration(itemName, buildingRegistration);
			}
		}
		return num;
	}

	public static int ProductsSoldLastWeekInRegistration(string itemName, BuildingRegistration registration)
	{
		int num = 0;
		foreach (OrderHistoryEntry item in registration.orderHistory)
		{
			if (item.dayNumber < SaveGameManager.Current.Day - 7)
			{
				continue;
			}
			foreach (OrderHistoryEntry.ItemReport itemSale in item.itemSales)
			{
				if (!(itemSale.itemName != itemName))
				{
					num += itemSale.amountSold;
					break;
				}
			}
		}
		return num;
	}
}
