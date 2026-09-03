using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Smartphone.Apps.EconoView;
using UnityEngine;

public class EconoViewBusinessDetails : MonoBehaviour
{
	public EconoView econoView;

	public EconoViewIncomeStatement incomeStatement;

	private List<FinancialSummary.BusinessIncomeStatement> _selectedBusinessIncomeStatements;

	[SerializeField]
	private TextLocalizationComponent[] daysText;

	public void RefreshData()
	{
		_selectedBusinessIncomeStatements = GetLastBusinessIncomeStatements(4);
		incomeStatement.Reset();
		SetDaysBanners();
		LoadSales();
		LoadOngoing();
		LoadResources();
		List<float> values = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => statement.TotalProfit).ToList();
		incomeStatement.SetTotal("econoview_row_profit", values);
	}

	public void SetDestination()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(econoView.selectedBusiness.Address);
	}

	private void SetDaysBanners()
	{
		if (daysText.Length == 0)
		{
			return;
		}
		daysText[0].SetData("econoview_yesterday_date".Localize(new
		{
			dayNumber = SaveGameManager.Current.Day - 1
		}));
		for (int i = 1; i < daysText.Length; i++)
		{
			int num = SaveGameManager.Current.Day - (i + 1);
			if (num > 0)
			{
				daysText[i].SetData("common_day_number".Localize(new
				{
					number = num
				}));
				continue;
			}
			break;
		}
	}

	private List<FinancialSummary.BusinessIncomeStatement> GetLastBusinessIncomeStatements(int numberOfDays)
	{
		List<FinancialSummary.BusinessIncomeStatement> list = new List<FinancialSummary.BusinessIncomeStatement>();
		int i;
		for (i = 1; i <= numberOfDays; i++)
		{
			FinancialSummary.BusinessIncomeStatement item = SaveGameManager.Current.financialSummaries.Find((FinancialSummary x) => x.dayNumber == SaveGameManager.Current.Day - i)?.businessIncomeStatements.Find((FinancialSummary.BusinessIncomeStatement x) => x.Address == econoView.selectedBusiness.Address) ?? new FinancialSummary.BusinessIncomeStatement();
			list.Add(item);
		}
		return list;
	}

	private void LoadOngoing()
	{
		Transform transform = incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Default);
		List<float> list = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.SalaryExpenses).ToList();
		if (list.Sum() != 0f)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, "econoview_row_salaries", list, autoSetValue1Color: true, transform);
		}
		List<float> list2 = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.RentExpenses).ToList();
		if (list2.Sum() != 0f)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, "econoview_row_rent", list2, autoSetValue1Color: true, transform);
		}
		List<float> list3 = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.MarketingExpenses).ToList();
		if (list3.Sum() != 0f)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, "econoview_row_marketing", list3, autoSetValue1Color: true, transform);
		}
		List<float> list4 = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.Theft).ToList();
		if (list4.Sum() != 0f)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, "econoview_row_theft", list4, autoSetValue1Color: true, transform);
		}
		List<float> list5 = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.LicensingFees).ToList();
		if (list5.Sum() != 0f)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, "econoview_row_licensing_fees", list5, autoSetValue1Color: true, transform);
		}
		List<float> values = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.TotalOngoing).ToList();
		incomeStatement.SetRowData(transform, "econoview_row_ongoing_expenses", values);
	}

	private void LoadSales()
	{
		BusinessType data = BusinessTypeHelper.GetData(econoView.selectedBusiness);
		bool hasWeekendOnlyEntranceFee = data.hasWeekendOnlyEntranceFee;
		Transform transform = incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Default);
		int count = _selectedBusinessIncomeStatements.Count;
		Dictionary<string, List<float>> dictionary = new Dictionary<string, List<float>>();
		for (int i = 0; i < count; i++)
		{
			List<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry> sales = _selectedBusinessIncomeStatements[i].Sales;
			if (sales == null)
			{
				continue;
			}
			foreach (FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry item in sales)
			{
				if (!ItemsGetter.GetByName(item.ItemName).HasTag(TagRef.Itemtag.isbag))
				{
					if (!dictionary.TryGetValue(item.ItemName, out var value))
					{
						value = Enumerable.Repeat(0f, count).ToList();
						dictionary[item.ItemName] = value;
					}
					value[i] = item.Amount;
				}
			}
		}
		foreach (var (text2, list2) in dictionary)
		{
			if (list2.All((float a) => a == 0f))
			{
				continue;
			}
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Success, text2, list2, autoSetValue1Color: true, transform);
			if (hasWeekendOnlyEntranceFee && (!(text2 != data.defaultEntranceFee) || !(text2 != data.weekendOnlyEntranceFee)))
			{
				FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry transactionGroupEntry = FindFirstSalesGrouping(text2);
				if (transactionGroupEntry != null)
				{
					HandleMultipleEntranceFees(transactionGroupEntry, data, transform);
				}
			}
		}
		List<float> values = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => statement.TotalSales).ToList();
		incomeStatement.SetRowData(transform, "econoview_row_sales", values);
	}

	private FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry FindFirstSalesGrouping(string itemName)
	{
		foreach (FinancialSummary.BusinessIncomeStatement selectedBusinessIncomeStatement in _selectedBusinessIncomeStatements)
		{
			FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry transactionGroupEntry = selectedBusinessIncomeStatement.Sales?.Find((FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry x) => x.ItemName == itemName);
			if (transactionGroupEntry != null)
			{
				return transactionGroupEntry;
			}
		}
		return null;
	}

	private void HandleMultipleEntranceFees(FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry grouping, BusinessType businessType, Transform category)
	{
		string anotherEntranceFeeName = ((grouping.ItemName == businessType.defaultEntranceFee) ? businessType.weekendOnlyEntranceFee : businessType.defaultEntranceFee);
		List<float> list = new List<float> { 0f };
		bool flag = false;
		for (int i = 2; i <= 4; i++)
		{
			FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry transactionGroupEntry = _selectedBusinessIncomeStatements[i - 1].Sales?.Find((FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry x) => x.ItemName == anotherEntranceFeeName);
			if (transactionGroupEntry != null)
			{
				flag = true;
			}
			list.Add(transactionGroupEntry?.Amount ?? 0f);
		}
		if (flag)
		{
			incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Success, anotherEntranceFeeName, list, autoSetValue1Color: true, category);
		}
	}

	private void LoadResources()
	{
		Transform transform = incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Default);
		int count = _selectedBusinessIncomeStatements.Count;
		Dictionary<string, List<float>> dictionary = new Dictionary<string, List<float>>();
		for (int i = 0; i < count; i++)
		{
			List<FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry> resources = _selectedBusinessIncomeStatements[i].Resources;
			if (resources == null)
			{
				continue;
			}
			foreach (FinancialSummary.BusinessIncomeStatement.TransactionGroupEntry item in resources)
			{
				if (!dictionary.TryGetValue(item.ItemName, out var value))
				{
					value = Enumerable.Repeat(0f, count).ToList();
					dictionary[item.ItemName] = value;
				}
				value[i] = 0f - item.Amount;
			}
		}
		foreach (var (rowName, list2) in dictionary)
		{
			if (!list2.All((float a) => a == 0f))
			{
				incomeStatement.CreateRow(EconoViewIncomeStatement.RowType.Danger, rowName, list2, autoSetValue1Color: true, transform);
			}
		}
		List<float> values = _selectedBusinessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement statement) => 0f - statement.TotalResources).ToList();
		incomeStatement.SetRowData(transform, "econoview_row_resources", values);
	}
}
