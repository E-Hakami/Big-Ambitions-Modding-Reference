using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class InitializeHeadhunters : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance item in gameInstance.EmployeeInstances.Where((EmployeeInstance x) => x is Headhunter))
		{
			item.Initialize();
		}
		foreach (EmployeeInstance item2 in gameInstance.CandidateEmployeeInstances.Where((EmployeeInstance x) => x is Headhunter))
		{
			item2.Initialize();
		}
	}
}
