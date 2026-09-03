using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class UpdateInvestmentFunds : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.investmentFunds.RemoveAll((InvestmentFund investment) => investment.playerValue == 0f && !HasChangedValues(investment));
		foreach (InvestmentFund investmentFund in gameInstance.investmentFunds)
		{
			if (HasChangedValues(investmentFund))
			{
				continue;
			}
			bool flag = false;
			float num = 0f;
			foreach (InvestmentProgressEntry item in investmentFund.purchaseHistory)
			{
				if (!flag)
				{
					investmentFund.initialDeposit = item.change;
					flag = true;
				}
				else if (item.change > 0f)
				{
					investmentFund.additionalInvestment += item.change;
				}
				else
				{
					investmentFund.withdrawal -= item.change;
				}
				num += item.change;
			}
			investmentFund.interestPayment = investmentFund.playerValue - num;
			investmentFund.purchaseHistory.Clear();
			investmentFund.playerValue = 0f;
		}
	}

	private static bool HasChangedValues(InvestmentFund investment)
	{
		if (investment.initialDeposit == 0f && investment.additionalInvestment == 0f && investment.withdrawal == 0f)
		{
			return investment.interestPayment != 0f;
		}
		return true;
	}
}
