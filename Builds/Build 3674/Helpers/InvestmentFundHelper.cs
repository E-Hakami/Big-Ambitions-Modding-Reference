using System;
using System.Collections.Generic;
using Entities;
using Enums;
using Extensions;
using Localizor;
using UI.Notification;
using UnityEngine;

namespace Helpers;

public static class InvestmentFundHelper
{
	public const string AddressableLabel = "InvestmentFunds";

	private const int MaxIndividualAutoInvestmentNotifications = 2;

	private static List<InvestmentFundData> AllFunds;

	public static void OnInvestmentFundsLoaded(IList<InvestmentFundData> funds)
	{
		if (AllFunds == null)
		{
			AllFunds = new List<InvestmentFundData>();
		}
		AllFunds.Clear();
		AllFunds.AddRange(funds);
	}

	public static void RunDaily()
	{
		int yearsByDays = TimeHelper.GetYearsByDays(SaveGameManager.Current.Day);
		List<InvestmentFund> list = new List<InvestmentFund>();
		float num = 0f;
		foreach (InvestmentFund investmentFund in SaveGameManager.Current.investmentFunds)
		{
			InvestmentFundData data = GetData(investmentFund.name);
			if (data == null)
			{
				continue;
			}
			int num2 = Mathf.FloorToInt((float)yearsByDays / (float)data.yearlyMarketChanges.Count) * data.yearlyMarketChanges.Count;
			int num3 = data.yearlyMarketChanges[yearsByDays - num2];
			float num4 = investmentFund.CurrentValue * ((float)num3 / 100f) / (float)SaveGameManager.Current.gameVariables.daysPerYear * UnityEngine.Random.Range(0.9f, 1.1f);
			investmentFund.interestPayment += num4;
			investmentFund.UpdateDevelopmentHistory(num4);
			if (investmentFund.developmentHistory.Count > 14)
			{
				investmentFund.developmentHistory.RemoveRange(0, investmentFund.developmentHistory.Count - 14);
			}
			if (investmentFund.isAutoInvesting && investmentFund.autoInvestment > 0f)
			{
				if (!investmentFund.Invest(investmentFund.autoInvestment, showMoneyNotification: false))
				{
					Notifications.Show(NotificationType.Error, "econoview_investments_auto_invest_failed", new Dictionary<string, string> { 
					{
						"investmentFund",
						investmentFund.name.GetLocalization()
					} });
				}
				else
				{
					list.Add(investmentFund);
					num += investmentFund.autoInvestment;
				}
			}
		}
		ShowAutoInvestmentNotifications(list, num);
	}

	private static void ShowAutoInvestmentNotifications(List<InvestmentFund> investments, float total)
	{
		if (investments.Count == 0)
		{
			return;
		}
		if (investments.Count > 2)
		{
			Notifications.Show(NotificationType.Success, "econoview_investments_auto_invest_success_multiple", new Dictionary<string, string>
			{
				{
					"fundCount",
					investments.Count.ToString()
				},
				{
					"amount",
					total.ToShortCurrencyFormat()
				}
			});
			return;
		}
		for (int i = 0; i < investments.Count; i++)
		{
			InvestmentFund investmentFund = investments[i];
			Notifications.Show(NotificationType.Success, "econoview_investments_auto_invest_success", new Dictionary<string, string>
			{
				{
					"investmentFund",
					investmentFund.name.GetLocalization()
				},
				{
					"amount",
					investmentFund.autoInvestment.ToShortCurrencyFormat()
				}
			});
		}
	}

	public static List<InvestmentFundData> GetInvestmentFundsByAddress(Address address)
	{
		List<InvestmentFundData> list = new List<InvestmentFundData>();
		foreach (InvestmentFundData allFund in AllFunds)
		{
			if (allFund.bankAddress == address)
			{
				list.Add(allFund);
			}
		}
		return list;
	}

	public static InvestmentFundData GetData(string fundName)
	{
		foreach (InvestmentFundData allFund in AllFunds)
		{
			if (allFund.fundName == fundName)
			{
				return allFund;
			}
		}
		return null;
	}

	public static InvestmentFund GetInvestmentByName(string fundName, bool createIfNotExists = false)
	{
		foreach (InvestmentFund investmentFund2 in SaveGameManager.Current.investmentFunds)
		{
			if (investmentFund2.name == fundName)
			{
				return investmentFund2;
			}
		}
		if (!createIfNotExists)
		{
			return null;
		}
		InvestmentFund investmentFund = new InvestmentFund(fundName);
		SaveGameManager.Current.investmentFunds.Add(investmentFund);
		return investmentFund;
	}

	public static (Priority risk, int low, int high) DetermineRisk(InvestmentFundData fundData)
	{
		int num = int.MaxValue;
		int num2 = int.MinValue;
		foreach (int yearlyMarketChange in fundData.yearlyMarketChanges)
		{
			num = Math.Min(num, yearlyMarketChange);
			num2 = Math.Max(num2, yearlyMarketChange);
		}
		int num3 = num2 - num;
		Priority item = ((num3 > 10) ? Priority.High : ((num3 > 6) ? Priority.Medium : Priority.Low));
		return (risk: item, low: num, high: num2);
	}

	public static bool HasAnyInvestments()
	{
		List<InvestmentFund> investmentFunds = SaveGameManager.Current.investmentFunds;
		if (investmentFunds != null)
		{
			return investmentFunds.Count > 0;
		}
		return false;
	}
}
