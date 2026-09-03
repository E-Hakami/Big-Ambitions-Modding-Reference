using System;
using System.Linq;
using AI.Employees.SalaryNegotiation;
using BA_Packages.Rivals.Scripts;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Skills;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Helpers;

namespace Buildings.BuildingTypes.Shared;

[Serializable]
public class AiBusinessEmployeeData
{
	private const int MinPrimarySkillRandomValue = 30;

	private const int MaxPrimarySkillRandomValue = 95;

	private const int MaxPrimarySkillForReplacement = 50;

	public string id;

	public string primarySkillName;

	public float primarySkillValue;

	public string hoursPerWeekDemandName;

	public Address aiAddress;

	public bool isPoached;

	public bool isNegotiationFinished;

	public int reenableNegotiationAtDay;

	[NonSerialized]
	private string _fullName;

	public bool HasActiveSalaryNegotiation
	{
		get
		{
			if (SaveGameManager.Current.candidateSalaryNegotiations != null)
			{
				return SaveGameManager.Current.candidateSalaryNegotiations.Any((CandidateSalaryNegotiation x) => x.employeeInstance.id == id && !x.completed);
			}
			return false;
		}
	}

	public AiBusinessEmployeeData(string id, string primarySkill, Address aiAddress, bool replacement = false)
	{
		this.id = id;
		primarySkillName = primarySkill;
		this.aiAddress = aiAddress;
		primarySkillValue = GetSeededRandom().Next(30, replacement ? 50 : 95);
		hoursPerWeekDemandName = JobDemandHelper.GetRandomHoursPerWeekDemandForSkill(primarySkillName);
	}

	public AiBusinessEmployeeData(EmployeeInstance employee)
	{
		isPoached = true;
		_fullName = employee.characterData.name;
		id = employee.id;
		primarySkillName = employee.GetPrimarySkill();
		primarySkillValue = employee.GetSkillValue(primarySkillName);
		aiAddress = employee.assignedAddress;
		hoursPerWeekDemandName = JobDemandHelper.GetRandomHoursPerWeekDemandForSkill(primarySkillName);
	}

	private Random GetSeededRandom()
	{
		return new Random(id.GetHashCode());
	}

	public EmployeeInstance GetEmployeeInstance()
	{
		if (isPoached)
		{
			return BuildingHelper.GetBuildingRegistration(aiAddress).poachedEmployees.FirstOrDefault((EmployeeInstance x) => x.id == id);
		}
		Random seededRandom = GetSeededRandom();
		EmployeeInstance employeeInstance = new EmployeeInstance();
		employeeInstance.Initialize();
		employeeInstance.id = id;
		Gender gender = seededRandom.GetGender();
		string fullName = seededRandom.GetFullName(gender);
		employeeInstance.characterData.gender = gender;
		employeeInstance.characterData.name = fullName;
		employeeInstance.characterData.skills.Add(new Skill
		{
			name = primarySkillName,
			value = primarySkillValue
		});
		SkillData data = SkillHelper.GetData(primarySkillName);
		if (!string.IsNullOrEmpty(data.secondarySkill))
		{
			employeeInstance.characterData.skills.Add(new Skill
			{
				name = data.secondarySkill,
				value = seededRandom.Next(data.secondarySkillRange.x, data.secondarySkillRange.y)
			});
		}
		employeeInstance.assignedAddress = aiAddress;
		employeeInstance.GenerateDemands(hoursPerWeekDemandName);
		employeeInstance.characterData.ageInDays = RecruitmentHelper.GetRandomEmployeeAgeInDays(seededRandom);
		employeeInstance.hourlyWage = EmployeeHelper.CalculateHourlyWageForSkill(employeeInstance.characterData.skills.First());
		employeeInstance.initialCombinedSkillAmount = employeeInstance.characterData.skills.Sum((Skill x) => x.value);
		return employeeInstance;
	}

	public int GetExpectedHoursPerWeek()
	{
		JobDemand byName = JobDemandHelper.GetByName(hoursPerWeekDemandName);
		if ((bool)byName && byName is HoursWorkingPerWeek hoursWorkingPerWeek)
		{
			return (hoursWorkingPerWeek.MinHours + hoursWorkingPerWeek.MaxHours) / 2;
		}
		return 40;
	}

	public string GetFullName()
	{
		Random seededRandom = GetSeededRandom();
		return _fullName ?? (_fullName = seededRandom.GetFullName(seededRandom.GetGender()));
	}
}
