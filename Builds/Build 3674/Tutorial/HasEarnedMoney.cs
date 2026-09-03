using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasEarnedMoney")]
public class HasEarnedMoney : QuestRequirement
{
	[SerializeField]
	private string[] transactionTypes;

	[SerializeField]
	private int minimumAmount;

	public override bool CheckIfCompleted()
	{
		if (transactionTypes.Length == 0)
		{
			return SaveGameManager.Current.Transactions.Sum((Transaction x) => x.amount) >= (float)minimumAmount;
		}
		return SaveGameManager.Current.Transactions.Where((Transaction x) => transactionTypes.Contains(x.transactionType)).Sum((Transaction x) => x.amount) >= (float)minimumAmount;
	}
}
