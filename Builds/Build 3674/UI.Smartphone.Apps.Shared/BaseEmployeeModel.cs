using System.Collections.Generic;
using Localizor;

namespace UI.Smartphone.Apps.Shared;

public abstract class BaseEmployeeModel
{
	public List<string> demands;

	public readonly List<string> statuses = new List<string>();

	public string employeeId;

	public string employeeName;

	public float hourlyWage;

	public string primarySkill;

	public string primarySkillName;

	public int primarySkillPercentage;

	public float satisfaction;

	protected BaseEmployeeModel(string employeeId, string employeeName, float hourlyWage, string primarySkillName, int primarySkillPercentage, float satisfaction, List<string> demands)
	{
		this.employeeId = employeeId;
		this.employeeName = employeeName;
		this.hourlyWage = hourlyWage;
		this.primarySkillName = primarySkillName;
		this.primarySkillPercentage = primarySkillPercentage;
		this.satisfaction = satisfaction;
		this.demands = demands;
		primarySkill = primarySkillName.GetLocalization() + $"{primarySkillPercentage:000}";
	}
}
