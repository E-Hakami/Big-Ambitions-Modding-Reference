using System.Collections.Generic;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UI;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

public class EconoViewTaxes : MonoBehaviour
{
	private const string NoTaxesDueKey = "irs_no_taxes_due";

	private const string TaxesPaidKey = "econoview_taxes_paid";

	private const string UnderTaxMinimumKey = "econoview_taxes_under_minimum";

	[SerializeField]
	private TMP_Text taxRateAmountLabel;

	[BoxGroup("BackTaxes")]
	[SerializeField]
	private GameObject backTaxesArea;

	[BoxGroup("BackTaxes")]
	[SerializeField]
	private TMP_Text backTaxesAmountLabel;

	[BoxGroup("DueTaxes")]
	[SerializeField]
	private TMP_Text taxesOwedAmountLabel;

	[BoxGroup("DueTaxes")]
	[SerializeField]
	private TextLocalizationComponent taxesOwedDaysLeftLabel;

	[BoxGroup("DueTaxes")]
	[SerializeField]
	private ProgressBar taxesOwedProgressBar;

	[BoxGroup("Projection")]
	[SerializeField]
	private TMP_Text upcomingTaxProjectionAmountLabel;

	[BoxGroup("Projection")]
	[SerializeField]
	private TextLocalizationComponent upcomingTaxProjectionDaysLeftLabel;

	[BoxGroup("Projection")]
	[SerializeField]
	private ProgressBar upcomingTaxProjectionProgressBar;

	[BoxGroup("TaxDeduction")]
	[SerializeField]
	private TMP_Text taxDeductionAmountLabel;

	[BoxGroup("Contact")]
	[SerializeField]
	private GameObject contactArea;

	[BoxGroup("Contact")]
	[SerializeField]
	private Button irsContactButton;

	private void Start()
	{
		if ((bool)irsContactButton)
		{
			irsContactButton.onClick.AddListener(OpenIrsContact);
		}
	}

	public void Init()
	{
		taxRateAmountLabel.SetText($"{SaveGameManager.Current.gameVariables.taxPercentage}%");
		SetupBackTaxesRow();
		SetupCurrentTaxRow();
		SetupProjectedTaxRow();
		SetupTaxDeductionsRow();
		contactArea.SetActive(TaxHelper.IsIrsContactAdded());
	}

	private void SetupBackTaxesRow()
	{
		bool flag = TaxHelper.HasBackTaxesToPay();
		backTaxesArea.SetActive(flag);
		if (flag)
		{
			backTaxesAmountLabel.text = TaxHelper.GetBackTaxesToPay().ToShortCurrencyFormat();
			backTaxesAmountLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
	}

	private void SetupCurrentTaxRow()
	{
		if (SaveGameManager.Current.currentUnpaidTaxes != null)
		{
			int currentTaxesDueDay = TaxHelper.GetCurrentTaxesDueDay();
			SetTaxesOwed(TaxHelper.GetCurrentTaxesToPay(), currentTaxesDueDay, TaxHelper.GetCurrentTaxesProgress(), InstanceBehavior<GlobalReferences>.Instance.colors.red);
			return;
		}
		int paymentStartDay = GetPaymentStartDay();
		int num = paymentStartDay + 20;
		bool num2 = paymentStartDay > 0 && SaveGameManager.Current.Day <= num;
		Transaction transaction = FindLastTaxPayment(paymentStartDay);
		if (num2 && PlayerReachedTaxMinimum(paymentStartDay) && transaction != null)
		{
			taxesOwedAmountLabel.text = "econoview_taxes_paid".GetLocalization();
			taxesOwedAmountLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen;
			taxesOwedDaysLeftLabel.SetData(LanguageChangeEventDataHolder.Create("econoview_taxes_paid_day", new
			{
				day = transaction.timestamp.Day
			}));
			taxesOwedProgressBar.SetValue(100f);
		}
		else
		{
			taxesOwedAmountLabel.text = "irs_no_taxes_due".GetLocalization();
			taxesOwedAmountLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen;
			taxesOwedDaysLeftLabel.Key = string.Empty;
			taxesOwedDaysLeftLabel.TextContainer.text = "-";
			taxesOwedProgressBar.SetValue(0f);
		}
	}

	private void SetupProjectedTaxRow()
	{
		int day = SaveGameManager.Current.Day;
		int daysPerYear = SaveGameManager.Current.gameVariables.daysPerYear;
		int projectedTaxPeriodStartDay = GetProjectedTaxPeriodStartDay(day, daysPerYear);
		int day2 = projectedTaxPeriodStartDay + daysPerYear - 1;
		int num = day - 1;
		int num2 = Mathf.Max(0, num - projectedTaxPeriodStartDay + 1);
		bool flag = true;
		float amount = 0f;
		if (num2 > 0)
		{
			flag = GetBusinessSales(projectedTaxPeriodStartDay, num) / (float)num2 * (float)daysPerYear < 150000f;
			if (!flag)
			{
				amount = GetProjectedTaxTotal(projectedTaxPeriodStartDay, num, num2, daysPerYear);
			}
		}
		float progress = (float)num2 / (float)daysPerYear * 100f;
		SetUpcomingTaxProjection(amount, day2, progress, flag);
	}

	private void SetupTaxDeductionsRow()
	{
		if (SaveGameManager.Current.currentTaxPeriodDeductibleExpenses == null)
		{
			SetTaxDeductionLabel(0f);
			return;
		}
		float num = 0f;
		foreach (TaxDeductibleExpense currentTaxPeriodDeductibleExpense in SaveGameManager.Current.currentTaxPeriodDeductibleExpenses)
		{
			num += currentTaxPeriodDeductibleExpense.amount;
		}
		SetTaxDeductionLabel(num);
	}

	private void SetTaxesOwed(float amount, int day, float progress, Color amountColor)
	{
		taxesOwedAmountLabel.text = amount.ToShortCurrencyFormat();
		taxesOwedAmountLabel.color = amountColor;
		SetDaysLeft(taxesOwedDaysLeftLabel, day);
		taxesOwedProgressBar.SetValue(progress);
	}

	private void SetUpcomingTaxProjection(float amount, int day, float progress, bool isUnderTaxMinimum)
	{
		upcomingTaxProjectionAmountLabel.text = (isUnderTaxMinimum ? "econoview_taxes_under_minimum".GetLocalization() : amount.ToShortCurrencyFormat());
		upcomingTaxProjectionAmountLabel.color = (isUnderTaxMinimum ? InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		SetProjectedDaysLeft(upcomingTaxProjectionDaysLeftLabel, day);
		upcomingTaxProjectionProgressBar.SetValue(progress);
	}

	private void SetTaxDeductionLabel(float amount)
	{
		taxDeductionAmountLabel.color = ((amount > 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen : InstanceBehavior<GlobalReferences>.Instance.colors.black);
		taxDeductionAmountLabel.SetText(amount.ToShortCurrencyFormat());
	}

	private static void SetDaysLeft(TextLocalizationComponent label, int day)
	{
		label.SetData(LanguageChangeEventDataHolder.Create("econoview_taxes_due_day", new
		{
			day = day,
			daysLeft = GetDaysLeft(day)
		}));
	}

	private static void SetProjectedDaysLeft(TextLocalizationComponent label, int day)
	{
		label.SetData(LanguageChangeEventDataHolder.Create("econoview_taxes_due_day", new
		{
			day = day,
			daysLeft = GetProjectedDaysLeft(day)
		}));
	}

	private static float GetProjectedTaxTotal(int periodStartDay, int latestRealDay, int elapsedDays, int daysPerYear)
	{
		float num = GetBusinessSales(periodStartDay, latestRealDay) / (float)elapsedDays * (float)daysPerYear;
		num -= GetCurrentDeductibleExpensesTotal();
		num += GetCurrentTaxableGamblingWinnings();
		if (num < 0f)
		{
			num = 0f;
		}
		return num * ((float)SaveGameManager.Current.gameVariables.taxPercentage / 100f) + GetProjectedRealEstateTaxes(periodStartDay, daysPerYear);
	}

	private static float GetBusinessSales(int periodStartDay, int latestRealDay)
	{
		float num = 0f;
		for (int i = 0; i < SaveGameManager.Current.financialSummaries.Count; i++)
		{
			FinancialSummary financialSummary = SaveGameManager.Current.financialSummaries[i];
			if (financialSummary.dayNumber >= periodStartDay && financialSummary.dayNumber <= latestRealDay)
			{
				for (int j = 0; j < financialSummary.businessIncomeStatements.Count; j++)
				{
					num += financialSummary.businessIncomeStatements[j].TotalSales;
				}
			}
		}
		return num;
	}

	private static bool PlayerReachedTaxMinimum(int paymentStartDay)
	{
		int daysPerYear = SaveGameManager.Current.gameVariables.daysPerYear;
		return GetBusinessSales(paymentStartDay - daysPerYear + 1, paymentStartDay) >= 150000f;
	}

	private static float GetCurrentDeductibleExpensesTotal()
	{
		float num = 0f;
		List<TaxDeductibleExpense> currentTaxPeriodDeductibleExpenses = SaveGameManager.Current.currentTaxPeriodDeductibleExpenses;
		if (currentTaxPeriodDeductibleExpenses == null)
		{
			return num;
		}
		for (int i = 0; i < currentTaxPeriodDeductibleExpenses.Count; i++)
		{
			float amount = currentTaxPeriodDeductibleExpenses[i].amount;
			num += Mathf.Abs(amount);
		}
		return num;
	}

	private static float GetCurrentTaxableGamblingWinnings()
	{
		float num = SaveGameManager.Current.CurrentTaxPeriodGamblingWinnings - SaveGameManager.Current.CurrentTaxPeriodGamblingLosses;
		if (!(num > 0f))
		{
			return 0f;
		}
		return num;
	}

	private static float GetProjectedRealEstateTaxes(int periodStartDay, int daysPerYear)
	{
		float num = 0f;
		int num2 = periodStartDay + daysPerYear - 1;
		for (int i = 0; i < SaveGameManager.Current.realEstate.Count; i++)
		{
			RealEstate realEstate = SaveGameManager.Current.realEstate[i];
			if (realEstate.purchaseDay <= num2)
			{
				int num3 = num2 - realEstate.purchaseDay;
				num += ((num3 < daysPerYear) ? (realEstate.TaxesAmount * (float)num3 / (float)daysPerYear) : realEstate.TaxesAmount);
			}
		}
		return num;
	}

	private static Transaction FindLastTaxPayment(int firstDay)
	{
		Transaction transaction = null;
		foreach (Transaction transaction2 in SaveGameManager.Current.Transactions)
		{
			if (!(transaction2.transactionType != "ba:transaction_taxpayment") && transaction2.timestamp != null && transaction2.timestamp.Day >= firstDay && (transaction == null || transaction2.timestamp.Day > transaction.timestamp.Day))
			{
				transaction = transaction2;
			}
		}
		return transaction;
	}

	private static int GetPaymentStartDay()
	{
		return SaveGameManager.Current.Day / SaveGameManager.Current.gameVariables.daysPerYear * SaveGameManager.Current.gameVariables.daysPerYear;
	}

	private static int GetProjectedTaxPeriodStartDay(int currentDay, int daysPerYear)
	{
		return currentDay / daysPerYear * daysPerYear + 1;
	}

	private static int GetDaysLeft(int day)
	{
		return Mathf.Max(0, day - SaveGameManager.Current.Day);
	}

	private static int GetProjectedDaysLeft(int day)
	{
		return Mathf.Max(0, day - SaveGameManager.Current.Day - 1);
	}

	private static void OpenIrsContact()
	{
		Contact iRSContact = TaxHelper.GetIRSContact();
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
		InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(iRSContact);
	}
}
