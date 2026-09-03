using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasTransactionType")]
public class HasTransactionType : QuestRequirement
{
	public string transactionType;

	[SerializeField]
	private bool latestTransactionOnly;

	public override bool CheckIfCompleted()
	{
		if (!latestTransactionOnly)
		{
			foreach (Transaction transaction in SaveGameManager.Current.Transactions)
			{
				if (transaction.transactionType == transactionType)
				{
					return true;
				}
			}
			return false;
		}
		string empty = string.Empty;
		foreach (Transaction transaction2 in SaveGameManager.Current.Transactions)
		{
			empty = transaction2.transactionType;
		}
		return empty == transactionType;
	}
}
