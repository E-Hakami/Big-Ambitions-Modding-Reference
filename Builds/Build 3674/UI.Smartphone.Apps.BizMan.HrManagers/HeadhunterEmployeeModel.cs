using System;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public class HeadhunterEmployeeModel
{
	public string Id;

	public string EmployeeId;

	public string EmployeeName;

	public string BusinessName;

	public string PrimarySkill;

	public string ReplacementReason;

	private readonly Action<string> _onReplace;

	public HeadhunterEmployeeModel(string employeeId, string employeeName, string businessName, string primarySkill, string replacementReason)
	{
		EmployeeId = employeeId;
		EmployeeName = employeeName;
		BusinessName = businessName;
		PrimarySkill = primarySkill;
		ReplacementReason = replacementReason;
	}
}
