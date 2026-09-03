using System.Linq;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Player/HasInvestments")]
public class HasInvestments : QuestRequirement
{
	[SerializeField]
	private float minimumAmount;

	public override bool CheckIfCompleted()
	{
		if (minimumAmount <= 0f)
		{
			return InvestmentFundHelper.HasAnyInvestments();
		}
		return SaveGameManager.Current.investmentFunds.Sum((InvestmentFund x) => x.CurrentValue) >= minimumAmount;
	}
}
