using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings;
using Buildings.BuildingTypes.Shared;
using Entities;
using UnityEngine;

namespace Helpers;

public static class BusinessEmployeeGenerator
{
	public class SkillForce
	{
		public readonly string skillName;

		public int hours;

		public SkillForce(string skillName)
		{
			this.skillName = skillName;
		}
	}

	public static void GenerateEmployees(this BuildingRegistration buildingRegistration)
	{
		foreach (AiBusinessEmployeeData aiEmployee in buildingRegistration.aiEmployees)
		{
			GenerateEmployeeFromAI(aiEmployee);
		}
		buildingRegistration.aiEmployees.Clear();
	}

	private static void GenerateEmployeeFromAI(AiBusinessEmployeeData aiEmployee)
	{
		EmployeeInstance employeeInstance = aiEmployee.GetEmployeeInstance();
		employeeInstance.satisfaction = Random.Range(30, 70);
		employeeInstance.dayHired = SaveGameManager.Current.Day;
		employeeInstance.nextSickDay = EmployeeHelper.GetNextSickDay(employeeInstance);
		employeeInstance.complaintData.ResetHoursUntilNextComplaint();
		EmployeeHelper.GetEmployeeInstances().Add(employeeInstance);
		EmployeeHelper.EmployeeInstancesDictionary.Add(employeeInstance.id, employeeInstance);
	}

	public static List<SkillForce> CalculateNeededSkillForce(this BuildingRegistration buildingRegistration, List<string> workStations)
	{
		string[] employeePrimarySkills = BusinessTypeHelper.GetData(buildingRegistration).employeePrimarySkills;
		List<SkillForce> list = new List<SkillForce>();
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				continue;
			}
			int getHoursOpen = scheduleDay.GetHoursOpen;
			foreach (string workStation in workStations)
			{
				Item byName = ItemsGetter.GetByName(workStation);
				bool flag = false;
				string workStationSkill = string.Empty;
				string[] suitableSkills = byName.suitableSkills;
				foreach (string text in suitableSkills)
				{
					if (employeePrimarySkills.Contains(text))
					{
						workStationSkill = text;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					string[] requiredBuildingSkills = BuildingTypeHelper.GetData(buildingRegistration).requiredBuildingSkills;
					suitableSkills = byName.suitableSkills;
					foreach (string text2 in suitableSkills)
					{
						if (requiredBuildingSkills.Contains(text2))
						{
							workStationSkill = text2;
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					SkillForce skillForce = list.FirstOrDefault((SkillForce x) => x.skillName == workStationSkill);
					if (skillForce == null)
					{
						skillForce = new SkillForce(workStationSkill);
						list.Add(skillForce);
					}
					skillForce.hours += getHoursOpen;
				}
			}
		}
		return list;
	}
}
