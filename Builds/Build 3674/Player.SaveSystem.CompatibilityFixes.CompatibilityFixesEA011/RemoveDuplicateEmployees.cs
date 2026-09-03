using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public sealed class RemoveDuplicateEmployees : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		HashSet<string> seenIds = new HashSet<string>();
		gameInstance.EmployeeInstances.RemoveAll((EmployeeInstance employee) => !seenIds.Add(employee.id));
	}
}
