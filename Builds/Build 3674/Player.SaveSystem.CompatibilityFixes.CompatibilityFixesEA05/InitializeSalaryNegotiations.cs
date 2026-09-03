using System.Collections.Generic;
using AI.Employees.SalaryNegotiation;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class InitializeSalaryNegotiations : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.candidateSalaryNegotiations == null)
		{
			gameInstance.candidateSalaryNegotiations = new List<CandidateSalaryNegotiation>();
		}
	}
}
