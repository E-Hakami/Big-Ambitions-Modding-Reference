using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using Entities;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public sealed class ScheduleEmployeeModel : BaseEmployeeModel
{
	public List<Skill> skills;

	public Action<string> onEmployeeSelected;

	public Color backgroundColor;

	public int daysAssigned;

	public int daysWorked;

	public int hoursAssigned;

	public int hoursWorked;

	public bool isAvailable;

	public bool isEmployeeList;

	public bool perfectMatch;

	public string violatedDemandName;

	public List<DayOfWeekOrdered> overworkedDays;

	public ScheduleEmployeeModel(EmployeeInstance instance, bool isAvailable, bool perfectMatch, string violatedDemandName, List<DayOfWeekOrdered> overworkedDays, Color backgroundColor, bool isEmployeeList, Action<string> onEmployeeSelected)
		: base(instance.id, instance.characterData.name, instance.hourlyWage, instance.GetPrimarySkill(), Mathf.FloorToInt(instance.GetSkillValue(instance.GetPrimarySkill())), instance.satisfaction, instance.demands)
	{
		demands = instance.demands;
		skills = instance.characterData.skills;
		daysAssigned = instance.assignedWeeklyDays.Count;
		daysWorked = instance.workedDays;
		hoursAssigned = instance.assignedWeeklyHours;
		hoursWorked = instance.workedHoursThisWeek;
		this.isAvailable = isAvailable;
		this.perfectMatch = perfectMatch;
		this.violatedDemandName = violatedDemandName;
		this.overworkedDays = overworkedDays;
		this.backgroundColor = backgroundColor;
		this.isEmployeeList = isEmployeeList;
		this.onEmployeeSelected = onEmployeeSelected;
	}
}
