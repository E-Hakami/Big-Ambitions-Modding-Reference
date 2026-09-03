using System;
using System.Collections.Generic;
using JimmysUnityUtilities;
using Localizor;

namespace Entities;

[Serializable]
public class InvestmentFund
{
	public string name;

	[Obsolete("Obsolete since: 1.0")]
	public float playerValue;

	public float initialDeposit;

	public float additionalInvestment;

	public float withdrawal;

	public float interestPayment;

	public bool isAutoInvesting;

	public float autoInvestment;

	[Obsolete("Obsolete since: 1.0")]
	public List<InvestmentProgressEntry> purchaseHistory = new List<InvestmentProgressEntry>();

	public List<InvestmentProgressEntry> developmentHistory = new List<InvestmentProgressEntry>();

	public float CurrentValue => initialDeposit + additionalInvestment - withdrawal + (float)interestPayment.RoundToInt();

	public InvestmentFund()
	{
	}

	public InvestmentFund(string name)
	{
		this.name = name;
	}

	public bool Invest(float amount, bool showMoneyNotification = true)
	{
		if (amount <= 0f)
		{
			return false;
		}
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"investmentFundName",
			name.GetLocalization()
		} };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_investment", data);
		float amount2 = 0f - amount;
		bool showNotification = showMoneyNotification;
		if (!GameManager.ChangeMoneySafe(amount2, transactionInfo, null, null, force: false, showNotification))
		{
			return false;
		}
		if (initialDeposit == 0f)
		{
			initialDeposit = amount;
		}
		else
		{
			additionalInvestment += amount;
		}
		UpdateDevelopmentHistory();
		GameEvent.Invoke("ba:gameevent_investmentdone");
		return true;
	}

	public void UpdateDevelopmentHistory(float change = 0f)
	{
		int num = developmentHistory.Count - 1;
		if (num >= 0 && developmentHistory[num].day == SaveGameManager.Current.Day)
		{
			developmentHistory[num].change += change;
			developmentHistory[num].newBalance = CurrentValue;
		}
		else
		{
			developmentHistory.Add(new InvestmentProgressEntry(SaveGameManager.Current.Day, change, CurrentValue));
		}
	}
}
