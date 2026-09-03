using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class RecalculateLoanPayments : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		LoanHelper.RecalculateLoanPayments(gameInstance);
	}
}
