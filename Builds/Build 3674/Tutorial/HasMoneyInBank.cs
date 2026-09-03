using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasMoneyInBank")]
public class HasMoneyInBank : QuestRequirement
{
	[SerializeField]
	private float minimumAmount;

	[SerializeField]
	private bool checkBankLoans;

	[SerializeField]
	private bool inverted;

	public override bool CheckIfCompleted()
	{
		float num = Mathf.Ceil(SaveGameManager.Current.Money);
		if (checkBankLoans)
		{
			float num2 = 0f;
			foreach (Loan loan in SaveGameManager.Current.Loans)
			{
				num2 += loan.remainingAmount;
			}
			num -= num2;
		}
		bool flag = num >= minimumAmount;
		if (!inverted)
		{
			return flag;
		}
		return !flag;
	}
}
