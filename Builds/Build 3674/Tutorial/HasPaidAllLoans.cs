using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasPaidAllLoans")]
public class HasPaidAllLoans : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return !LoanHelper.HasLoans();
	}
}
