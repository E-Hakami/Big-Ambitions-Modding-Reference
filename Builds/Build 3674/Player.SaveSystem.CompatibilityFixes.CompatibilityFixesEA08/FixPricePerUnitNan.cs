using System.Collections.Generic;
using BigAmbitions.Items;
using Entities;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixPricePerUnitNan : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			foreach (Order unprocessedCompletedOrder in buildingRegistration.unprocessedCompletedOrders)
			{
				foreach (OrderEntry entry in unprocessedCompletedOrder.entries)
				{
					if (float.IsNaN(entry.wholesalePrice))
					{
						Item byName = ItemsGetter.GetByName(entry.itemName);
						entry.wholesalePrice = byName.GetWholesalePrice();
						if (entry.wholesalePrice == 0f)
						{
							entry.wholesalePrice = byName.DefaultMarketPrice;
						}
					}
				}
			}
			foreach (KeyValuePair<string, ItemInstance> itemInstance in buildingRegistration.itemInstances)
			{
				foreach (CargoInstance cargoInstance in itemInstance.Value.cargoInstances)
				{
					if (!float.IsNaN(cargoInstance.pricePerUnit))
					{
						continue;
					}
					if (string.IsNullOrEmpty(cargoInstance.itemName))
					{
						cargoInstance.pricePerUnit = 0f;
						continue;
					}
					cargoInstance.pricePerUnit = cargoInstance.ItemCached.GetWholesalePrice();
					if (cargoInstance.pricePerUnit == 0f)
					{
						cargoInstance.pricePerUnit = cargoInstance.ItemCached.DefaultMarketPrice;
					}
				}
			}
			foreach (OrderHistoryEntry item in buildingRegistration.orderHistory)
			{
				foreach (OrderHistoryEntry.ItemReport itemSale in item.itemSales)
				{
					if (!float.IsNaN(itemSale.totalWholesalePrice))
					{
						continue;
					}
					Item byName2 = ItemsGetter.GetByName(itemSale.itemName);
					float num = byName2.GetWholesalePrice();
					if (num == 0f)
					{
						num = byName2.DefaultMarketPrice;
					}
					itemSale.totalWholesalePrice = num * (float)itemSale.amountSold;
					foreach (FinancialSummary financialSummary in gameInstance.financialSummaries)
					{
						bool flag = false;
						foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in financialSummary.businessIncomeStatements)
						{
							if (businessIncomeStatement.Address != buildingRegistration.Address)
							{
								continue;
							}
							bool flag2 = false;
							foreach (FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry resource in businessIncomeStatement.Resources)
							{
								if (resource.ItemName == itemSale.itemName)
								{
									resource.Amount = itemSale.totalWholesalePrice;
									flag2 = true;
									flag = true;
								}
							}
							if (flag2)
							{
								businessIncomeStatement.TotalResources = businessIncomeStatement.Resources.SumValues((FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry x) => x.Amount);
								if (float.IsNaN(businessIncomeStatement.Theft))
								{
									buildingRegistration.stolenItemsCost = 0f;
									businessIncomeStatement.Theft = 0f;
									businessIncomeStatement.TotalOngoing = businessIncomeStatement.SalaryExpenses + businessIncomeStatement.RentExpenses + businessIncomeStatement.MarketingExpenses;
								}
								businessIncomeStatement.TotalProfit = businessIncomeStatement.TotalSales - businessIncomeStatement.TotalResources - businessIncomeStatement.TotalOngoing;
							}
						}
						if (flag)
						{
							financialSummary.totalBusinessProfit = financialSummary.businessIncomeStatements.SumValues((FinancialSummary.BusinessIncomeStatement x) => x.TotalProfit);
							FinancialSummaryHelper.UpdateIncomeStatementTotalProfit(financialSummary);
						}
					}
				}
			}
		}
	}
}
