using System.Collections.Generic;
using Enums;
using HGAttributes;
using Helpers;
using Localizor;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Entities.Employee.JobDemands;

public abstract class JobDemand : ScriptableObject
{
	[Header("Demand")]
	public string demandName;

	[SearchableEnum]
	public Priority priority;

	[Space]
	[Tooltip("If true, this demand will not be able to be added after hiring")]
	public bool isNonAdditive;

	[Tooltip("If true, only one demand from the same single selection group can be assigned")]
	public bool hasSingleSelectionGroup;

	[ShowIf("hasSingleSelectionGroup")]
	public string singleSelectionGroup;

	public bool isSkillSpecific;

	[ShowIf("isSkillSpecific")]
	[AutocompleteDropdown("Skills")]
	public string specificSkillName;

	[Space]
	public bool suitableToAllSkills;

	[HideIf("suitableToAllSkills")]
	[AutocompleteDropdown("Skills")]
	public string[] suitableSkillNames;

	public float Impact => priority switch
	{
		Priority.Low => 0.2f, 
		Priority.Medium => 0.3f, 
		Priority.High => 0.5f, 
		_ => 0f, 
	};

	public string PriorityLocalizeKey => "job_demand_priority_" + priority.ToStringFast();

	public string GetLocalization()
	{
		return demandName.GetLocalization();
	}

	public bool Fulfilled(string employeeId, List<ScheduleDay> scheduleDays)
	{
		EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(employeeId);
		return Fulfilled(employeeById, scheduleDays);
	}

	public abstract bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays);
}
