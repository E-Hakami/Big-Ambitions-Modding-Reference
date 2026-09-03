using Entities;
using Helpers;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class EmployeeModel : BaseEmployeeModel
{
	public string currentBusinessName;

	public string hrManagerPlanId;

	public string task;

	public EmployeeInstance employeeInstance;

	public int ageInDays;

	public int hoursPerWeek;

	public bool isAbsent;

	public EmployeeModel(EmployeeInstance instance)
		: base(instance.id, instance.characterData.name, instance.hourlyWage, instance.GetPrimarySkill(), Mathf.FloorToInt(instance.GetSkillValue(instance.GetPrimarySkill())), instance.satisfaction, instance.demands)
	{
		employeeInstance = instance;
		ageInDays = instance.characterData.ageInDays;
		task = instance.GetCurrentTask();
		currentBusinessName = BuildingHelper.GetBuildingRegistration(instance.assignedAddress)?.BusinessName;
		hoursPerWeek = instance.assignedWeeklyHours;
		hrManagerPlanId = instance.assignedHrManagerPlanId;
		isAbsent = instance.isAbsent;
		statuses.Add(instance.IsAssignedToAnyBusiness() ? "common_assigned" : "common_unassigned");
		if (instance.IsTraining)
		{
			statuses.Add("common_in_training");
		}
	}
}
