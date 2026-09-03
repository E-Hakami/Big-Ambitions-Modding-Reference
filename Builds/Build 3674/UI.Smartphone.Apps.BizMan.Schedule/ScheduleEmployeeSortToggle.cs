using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Items;
using Entities.Employee.JobDemands;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public sealed class ScheduleEmployeeSortToggle : BaseSortToggle<ScheduleEmployeeModel>
{
	[SerializeField]
	private bool isDefault;

	[SerializeField]
	private ScheduleEmployeeSortingMethod sortingMethod;

	public override void SetUp(int index, Action<int> onStateChanged)
	{
		SetUp(delegate
		{
			onStateChanged(index);
		});
		if (isDefault)
		{
			SetState(2, updateState: false);
		}
	}

	public override void Sort(ref List<ScheduleEmployeeModel> employees, string workstationId)
	{
		if (sortingMethod == ScheduleEmployeeSortingMethod.EquipmentDemand && string.IsNullOrEmpty(workstationId))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.gameObject.SetActive(value: true);
		if (state != 0)
		{
			bool ascending = state == 1;
			ScheduleEmployeeSortingMethod scheduleEmployeeSortingMethod = sortingMethod;
			ItemInstance workstation = ((scheduleEmployeeSortingMethod != ScheduleEmployeeSortingMethod.EquipmentDemand && scheduleEmployeeSortingMethod != ScheduleEmployeeSortingMethod.Skill) ? null : (string.IsNullOrEmpty(workstationId) ? null : ScheduleHelper.WorkstationsById[workstationId]));
			HashSet<string> equipmentDemands = ((sortingMethod == ScheduleEmployeeSortingMethod.EquipmentDemand) ? PrepareEquipmentDemands(workstation) : null);
			employees.Sort((ScheduleEmployeeModel a, ScheduleEmployeeModel b) => CompareEmployees(a, b, workstation, equipmentDemands, ascending));
		}
	}

	private static HashSet<string> PrepareEquipmentDemands(ItemInstance workstation)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string attachedItem in ScheduleHelper.GetAttachedItems(workstation))
		{
			if (!JobDemandHelper.IsEnvironmentDemand(attachedItem, out var demandNames))
			{
				continue;
			}
			foreach (string item in demandNames)
			{
				hashSet.Add(item);
			}
		}
		return hashSet;
	}

	private int CompareEmployees(ScheduleEmployeeModel a, ScheduleEmployeeModel b, ItemInstance workstation, HashSet<string> equipmentDemands, bool ascending)
	{
		if (a.perfectMatch && !b.perfectMatch)
		{
			return -1;
		}
		if (!a.perfectMatch && b.perfectMatch)
		{
			return 1;
		}
		if (a.isAvailable && !b.isAvailable)
		{
			return -1;
		}
		if (!a.isAvailable && b.isAvailable)
		{
			return 1;
		}
		int num = sortingMethod switch
		{
			ScheduleEmployeeSortingMethod.Skill => CompareBySkillAscending(a, b, workstation), 
			ScheduleEmployeeSortingMethod.EquipmentDemand => CompareByEquipmentDemandAscending(a, b, equipmentDemands), 
			_ => CompareByFallbackAscending(a, b), 
		};
		if (!ascending)
		{
			return -num;
		}
		return num;
	}

	private static int CompareBySkillAscending(ScheduleEmployeeModel a, ScheduleEmployeeModel b, ItemInstance workstation)
	{
		if (workstation != null)
		{
			string skillName = workstation.ItemCached.suitableSkills[0];
			int skillValue = GetSkillValue(a.skills, skillName);
			int skillValue2 = GetSkillValue(b.skills, skillName);
			if (skillValue < skillValue2)
			{
				return -1;
			}
			if (skillValue > skillValue2)
			{
				return 1;
			}
			return a.skills.Count.CompareTo(b.skills.Count);
		}
		Skill bestSkill = GetBestSkill(a.skills);
		Skill bestSkill2 = GetBestSkill(b.skills);
		int num = string.Compare(bestSkill.name, bestSkill2.name, StringComparison.OrdinalIgnoreCase);
		if (num != 0)
		{
			return num;
		}
		return bestSkill.value.CompareTo(bestSkill2.value);
	}

	private static int CompareByEquipmentDemandAscending(ScheduleEmployeeModel a, ScheduleEmployeeModel b, HashSet<string> equipmentDemands)
	{
		int num = a.demands.Count(equipmentDemands.Contains);
		int num2 = b.demands.Count(equipmentDemands.Contains);
		if (num < num2)
		{
			return -1;
		}
		if (num > num2)
		{
			return 1;
		}
		return a.demands.Count.CompareTo(b.demands.Count);
	}

	private int CompareByFallbackAscending(ScheduleEmployeeModel a, ScheduleEmployeeModel b)
	{
		return sortingMethod switch
		{
			ScheduleEmployeeSortingMethod.Salary => a.hourlyWage.CompareTo(b.hourlyWage), 
			ScheduleEmployeeSortingMethod.Satisfaction => a.satisfaction.CompareTo(b.satisfaction), 
			ScheduleEmployeeSortingMethod.ScheduledHours => a.hoursAssigned.CompareTo(b.hoursAssigned), 
			_ => 0, 
		};
	}

	private static int GetSkillValue(List<Skill> skills, string skillName)
	{
		for (int i = 0; i < skills.Count; i++)
		{
			if (skills[i].name == skillName)
			{
				return skills[i].GetRoundedValue();
			}
		}
		return 0;
	}

	private static Skill GetBestSkill(List<Skill> skills)
	{
		Skill result = null;
		float num = -1f;
		for (int i = 0; i < skills.Count; i++)
		{
			float value = skills[i].value;
			if (value > num)
			{
				result = skills[i];
				num = value;
			}
		}
		return result;
	}
}
