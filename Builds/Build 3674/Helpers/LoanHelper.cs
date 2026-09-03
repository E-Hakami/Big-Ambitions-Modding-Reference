using System.Collections.Generic;
using SpecialServices.Bank;
using UI.Notification;
using UnityEngine;

namespace Helpers;

public static class LoanHelper
{
	private const int MinimumDailyPayment = 5;

	public static void RunDaily()
	{
		bool flag = false;
		for (int num = SaveGameManager.Current.Loans.Count - 1; num >= 0; num--)
		{
			Loan loan = SaveGameManager.Current.Loans[num];
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(loan.bankAddress);
			float num2 = (float)loan.dailyInterest + (((float)loan.dailyPayment > loan.remainingAmount) ? loan.remainingAmount : ((float)loan.dailyPayment));
			Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_loanpayment", "ba:transactioncategory_loanexpenses", data);
			if (GameManager.ChangeMoneySafe(0f - num2, transactionInfo, SaveGameManager.Current.Day - 1))
			{
				loan.remainingAmount -= loan.dailyPayment;
				if (loan.remainingAmount <= 0f)
				{
					SaveGameManager.Current.Loans.RemoveAt(num);
				}
			}
			else
			{
				flag = true;
				GameManager.ChangeMoneySafe(-loan.dailyInterest, transactionInfo, SaveGameManager.Current.Day - 1, null, force: true);
			}
		}
		if (flag)
		{
			Notifications.Show(NotificationType.Error, "loan_installment_insufficient_money");
		}
	}

	public static BankSettings GetBankSettings(Address bankAddress)
	{
		return BuildingHelper.GetBuilding(bankAddress)?.SpecialService?.settings as BankSettings;
	}

	public static int CalculatePayBackDays(BankSettings bankSettings)
	{
		return CalculatePayBackDays(bankSettings, SaveGameManager.Current.gameVariables.daysPerYear);
	}

	public static int CalculatePayBackDays(BankSettings bankSettings, int daysPerYear)
	{
		return daysPerYear * bankSettings.yearsToPayLoan;
	}

	public static float CalculateEffectiveAnnualInterestRate(BankSettings bankSettings)
	{
		return (float)bankSettings.annualInterestRate * SaveGameManager.Current.gameVariables.bankInterestMultiplier;
	}

	public static int CalculateDailyInterestPayment(float requestedAmount, BankSettings bankSettings)
	{
		return CalculateDailyInterestPayment(requestedAmount, bankSettings, SaveGameManager.Current.gameVariables.daysPerYear, SaveGameManager.Current.gameVariables.bankInterestMultiplier);
	}

	public static int CalculateDailyInterestPayment(float requestedAmount, BankSettings bankSettings, int daysPerYear, float bankInterestMultiplier)
	{
		return Mathf.FloorToInt(requestedAmount * (float)bankSettings.annualInterestRate * bankInterestMultiplier / 100f * (float)bankSettings.yearsToPayLoan / (float)CalculatePayBackDays(bankSettings, daysPerYear));
	}

	public static int CalculateDailyPayment(float requestedAmount, BankSettings bankSettings)
	{
		return CalculateDailyPayment(requestedAmount, bankSettings, SaveGameManager.Current.gameVariables.daysPerYear);
	}

	public static int CalculateDailyPayment(float requestedAmount, BankSettings bankSettings, int daysPerYear)
	{
		int b = Mathf.FloorToInt(requestedAmount / (float)CalculatePayBackDays(bankSettings, daysPerYear));
		if (!(requestedAmount > 0f))
		{
			return 0;
		}
		return Mathf.Max(5, b);
	}

	public static int CalculateMinimumDailyPayment(Loan loan)
	{
		BankSettings bankSettings = GetBankSettings(loan.bankAddress);
		if (!(bankSettings == null))
		{
			return CalculateMinimumDailyPayment(loan.totalAmount, bankSettings);
		}
		return 0;
	}

	public static int CalculateMinimumDailyPayment(float requestedAmount, BankSettings bankSettings)
	{
		return CalculateMinimumDailyPayment(requestedAmount, bankSettings, SaveGameManager.Current.gameVariables.daysPerYear);
	}

	public static int CalculateMinimumDailyPayment(float requestedAmount, BankSettings bankSettings, int daysPerYear)
	{
		int b = Mathf.FloorToInt((float)CalculateDailyPayment(requestedAmount, bankSettings, daysPerYear) * 0.5f);
		if (!(requestedAmount > 0f))
		{
			return 0;
		}
		return Mathf.Max(5, b);
	}

	public static bool HasLoans()
	{
		return SaveGameManager.Current.Loans.Count > 0;
	}

	public static void RecalculateLoanPayments(GameInstance gameInstance)
	{
		foreach (Loan loan in gameInstance.Loans)
		{
			BankSettings bankSettings = GetBankSettings(loan.bankAddress);
			if (!(bankSettings == null))
			{
				loan.dailyInterest = CalculateDailyInterestPayment(loan.totalAmount, bankSettings, gameInstance.gameVariables.daysPerYear, gameInstance.gameVariables.bankInterestMultiplier);
				loan.dailyPayment = CalculateDailyPayment(loan.totalAmount, bankSettings, gameInstance.gameVariables.daysPerYear);
			}
		}
	}
}
