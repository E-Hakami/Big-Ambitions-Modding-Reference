using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Player/HasInvestmentValueLessThan")]
public class HasInvestmentValueLessThan : QuestRequirement
{
	[SerializeField]
	private float maximumAmount;

	public override List<string> ChangesToCheckOn => new List<string> { "ba:gameevent_newday" };

	public override bool CheckIfCompleted()
	{
		float num = 0f;
		foreach (InvestmentFund investmentFund in SaveGameManager.Current.investmentFunds)
		{
			num += investmentFund.CurrentValue;
			if (num >= maximumAmount)
			{
				return false;
			}
		}
		return true;
	}
}
