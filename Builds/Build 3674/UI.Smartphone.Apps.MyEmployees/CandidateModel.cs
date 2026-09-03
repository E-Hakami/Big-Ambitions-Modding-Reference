using Entities;
using Entities.Employee.JobDemands.Requirements;
using Localizor;
using Streets;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class CandidateModel : BaseEmployeeModel
{
	public string recruitmentSource;

	public string schedule;

	public EmployeeInstance employeeInstance;

	public int hoursUntilExpiring;

	public string assignBusiness
	{
		get
		{
			if (!employeeInstance.assignedAddress.IsUndefined())
			{
				return employeeInstance.assignedAddress.ToFormattedString();
			}
			return "common_unassigned".GetLocalization();
		}
	}

	public int expiresIn => hoursUntilExpiring;

	public CandidateModel(EmployeeInstance instance)
		: base(instance.id, instance.characterData.name, instance.hourlyWage, instance.GetPrimarySkill(), Mathf.FloorToInt(instance.GetSkillValue(instance.GetPrimarySkill())), instance.satisfaction, instance.demands)
	{
		employeeInstance = instance;
		demands = instance.demands;
		schedule = instance.GetDemandOfTypeLocalized<HoursWorkingPerWeek>();
		recruitmentSource = instance.candidateInfo.GetSource();
		hoursUntilExpiring = instance.candidateInfo.hoursUntilExpiring;
	}
}
