using System.Collections.Generic;
using Entities;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewOverview : MonoBehaviour
{
	public EconoView econoView;

	public List<FinancialSummary> latestFinancialSummaries;

	public EconoViewFullTransactions fullTransactionsPanel;

	[SerializeField]
	private EconoViewIncomeStatementScrollerController incomeStatementScrollerController;

	[SerializeField]
	private EconoViewLastTransactionsScrollerController lastTransactionsScrollerController;

	[SerializeField]
	private TextLocalizationComponent[] daysText;

	private void OnEnable()
	{
		latestFinancialSummaries = FinancialSummaryHelper.GetLastFinancialSummaries(4);
		Reload();
	}

	private void LoadTransactions()
	{
		lastTransactionsScrollerController.LoadLatest();
	}

	public static LanguageChangeEventDataHolder GenerateTransactionData(Transaction transaction)
	{
		LanguageChangeEventDataHolder result = LanguageChangeEventDataHolder.Create(transaction.transactionType);
		result.Arguments = transaction.transactionData;
		return result;
	}

	private void LoadIncomeStatement()
	{
		incomeStatementScrollerController.Load(latestFinancialSummaries, econoView);
	}

	private void SetDaysBanners()
	{
		if (daysText.Length == 0)
		{
			return;
		}
		daysText[0].SetData(LanguageChangeEventDataHolder.Create("econoview_yesterday_date", new
		{
			dayNumber = SaveGameManager.Current.Day - 1
		}));
		for (int i = 1; i < daysText.Length; i++)
		{
			int num = SaveGameManager.Current.Day - (i + 1);
			if (num > 0)
			{
				daysText[i].SetData(LanguageChangeEventDataHolder.Create("common_day_number", new
				{
					number = num
				}));
				continue;
			}
			break;
		}
	}

	private void Reload()
	{
		fullTransactionsPanel.Close();
		RefreshTransactions();
		SetDaysBanners();
		LoadIncomeStatement();
	}

	public void RefreshTransactions()
	{
		LoadTransactions();
		if (fullTransactionsPanel.isActiveAndEnabled)
		{
			fullTransactionsPanel.RefreshTransactionsList();
		}
	}
}
