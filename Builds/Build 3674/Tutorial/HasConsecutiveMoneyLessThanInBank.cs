using System.Collections.Generic;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasConsecutiveMoneyLessThanInBank")]
public class HasConsecutiveMoneyLessThanInBank : QuestRequirement
{
	[SerializeField]
	private int requiredConsecutiveNegativeMidnights = 2;

	[SerializeField]
	private float maximumAmount;

	public override List<string> ChangesToCheckOn => new List<string> { "ba:gameevent_newday" };

	public override bool CheckIfCompleted(string changeType)
	{
		if (changeType != "ba:gameevent_newday")
		{
			return false;
		}
		return CheckIfCompleted();
	}

	public override bool CheckIfCompleted()
	{
		List<float> midnightBankBalances = SaveGameManager.Current.midnightBankBalances;
		if (midnightBankBalances == null || midnightBankBalances.Count < requiredConsecutiveNegativeMidnights)
		{
			return false;
		}
		int num = midnightBankBalances.Count - requiredConsecutiveNegativeMidnights;
		for (int num2 = midnightBankBalances.Count - 1; num2 >= num; num2--)
		{
			if (midnightBankBalances[num2] >= maximumAmount)
			{
				return false;
			}
		}
		return true;
	}
}
