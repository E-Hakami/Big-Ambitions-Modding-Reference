using System.Collections.Generic;

namespace UI.Smartphone.Apps.MyEmployees;

public static class EmployeeStatusFilter
{
	public const string Assigned = "common_assigned";

	public const string Unassigned = "common_unassigned";

	public const string InTraining = "common_in_training";

	public static readonly IReadOnlyList<string> All = new string[3] { "common_assigned", "common_unassigned", "common_in_training" };
}
