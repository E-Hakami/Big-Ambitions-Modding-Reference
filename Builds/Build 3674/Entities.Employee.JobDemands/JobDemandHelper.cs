using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Entities.Employee.JobDemands.Requirements;
using Extensions;
using HGAttributes;
using JimmysUnityUtilities;
using UnityEngine;

namespace Entities.Employee.JobDemands;

public static class JobDemandHelper
{
	public const string AddressableLabel = "JobDemands";

	private static readonly Dictionary<string, JobDemand> AllJobDemandsByName = new Dictionary<string, JobDemand>();

	private static readonly Dictionary<string, List<string>> ItemsWithEnvironmentDemands = new Dictionary<string, List<string>>();

	private static readonly List<string> PossibleDemands = new List<string>();

	private static readonly Dictionary<string, List<string>> JobDemandsBySpecificSkill = new Dictionary<string, List<string>>();

	private static readonly Dictionary<string, string> SingleSelectionGroupByDemandName = new Dictionary<string, string>();

	private static readonly HashSet<string> ActiveSingleSelectionGroups = new HashSet<string>();

	private static List<JobDemand> AllJobDemands;

	public static readonly HashSet<string> ScheduleDemands = new HashSet<string>();

	public static readonly HashSet<string> EquipmentDemands = new HashSet<string>();

	public static readonly HashSet<string> EnvironmentDemands = new HashSet<string>();

	public static readonly HashSet<string> TypeDemands = new HashSet<string>();

	public static readonly HashSet<string> HealthInsuranceDemands = new HashSet<string>();

	public static readonly HashSet<string> NonAdditiveDemands = new HashSet<string>();

	[AutocompleteProvider("JobDemands")]
	private static IEnumerable<string> JobDemandNames => AllJobDemandsByName.Keys;

	public static void OnJobDemandsLoaded(IList<JobDemand> jobDemands)
	{
		if (AllJobDemands == null)
		{
			AllJobDemands = new List<JobDemand>(jobDemands.Count);
		}
		AllJobDemands.Clear();
		AllJobDemands.EnsureCapacity(jobDemands.Count);
		AllJobDemands.AddRange(jobDemands);
		ItemsWithEnvironmentDemands.Clear();
		JobDemandsBySpecificSkill.Clear();
		AllJobDemandsByName.Clear();
		SingleSelectionGroupByDemandName.Clear();
		ScheduleDemands.Clear();
		EquipmentDemands.Clear();
		EnvironmentDemands.Clear();
		TypeDemands.Clear();
		HealthInsuranceDemands.Clear();
		NonAdditiveDemands.Clear();
		foreach (JobDemand allJobDemand in AllJobDemands)
		{
			if (allJobDemand == null)
			{
				continue;
			}
			AllJobDemandsByName.Add(allJobDemand.demandName, allJobDemand);
			if (allJobDemand.hasSingleSelectionGroup && !string.IsNullOrEmpty(allJobDemand.singleSelectionGroup))
			{
				SingleSelectionGroupByDemandName[allJobDemand.demandName] = allJobDemand.singleSelectionGroup;
			}
			if (allJobDemand is IScheduleDemand)
			{
				ScheduleDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand is IEquipmentDemand)
			{
				EquipmentDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand is IEnvironmentDemand)
			{
				EnvironmentDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand is HoursWorkingPerWeek)
			{
				TypeDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand is HasHealthInsurance)
			{
				HealthInsuranceDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand.isNonAdditive)
			{
				NonAdditiveDemands.Add(allJobDemand.demandName);
			}
			if (allJobDemand is WorksOnItem { itemNames: var itemNames })
			{
				foreach (string key in itemNames)
				{
					if (!ItemsWithEnvironmentDemands.ContainsKey(key))
					{
						ItemsWithEnvironmentDemands.Add(key, new List<string>());
					}
					ItemsWithEnvironmentDemands[key].Add(allJobDemand.demandName);
				}
			}
			if (allJobDemand.isSkillSpecific)
			{
				if (JobDemandsBySpecificSkill.TryGetValue(allJobDemand.specificSkillName, out var value))
				{
					value.Add(allJobDemand.demandName);
					continue;
				}
				JobDemandsBySpecificSkill.Add(allJobDemand.specificSkillName, new List<string> { allJobDemand.demandName });
			}
		}
	}

	public static int GetIdealNumberOfDemands(string primarySkill, float totalEmployeeSkill)
	{
		if (!SkillHelper.GetData(primarySkill).HasTag(TagRef.Skilltag.canhavejobdemands))
		{
			return 0;
		}
		int num = 1;
		if (totalEmployeeSkill >= 50f)
		{
			num++;
		}
		if (totalEmployeeSkill >= 100f)
		{
			num++;
		}
		return num;
	}

	public static string GetRandomHoursPerWeekDemandForSkill(string skillName)
	{
		SkillData data = SkillHelper.GetData(skillName);
		if (!data.HasTag(TagRef.Skilltag.hashoursperweekdemand))
		{
			if (!data.HasTag(TagRef.Skilltag.forcefulltime))
			{
				return string.Empty;
			}
			return "ba:jobdemand_fulltime";
		}
		return TypeDemands.GetRandom();
	}

	public static string GetRandomJobSpecificDemandForSkill(string skillName)
	{
		if (!JobDemandsBySpecificSkill.TryGetValue(skillName, out var value))
		{
			return string.Empty;
		}
		return value.GetRandom();
	}

	public static string GetRandomDemandForSkill(string primarySkill, List<string> currentDemands, List<string> demandsToIgnore)
	{
		ActiveSingleSelectionGroups.Clear();
		foreach (string currentDemand in currentDemands)
		{
			if (SingleSelectionGroupByDemandName.TryGetValue(currentDemand, out var value))
			{
				ActiveSingleSelectionGroups.Add(value);
			}
		}
		PossibleDemands.Clear();
		foreach (JobDemand allJobDemand in AllJobDemands)
		{
			if ((demandsToIgnore != null && demandsToIgnore.Contains(allJobDemand.demandName)) || allJobDemand.isSkillSpecific || currentDemands.Contains(allJobDemand.demandName) || (SingleSelectionGroupByDemandName.TryGetValue(allJobDemand.demandName, out var value2) && ActiveSingleSelectionGroups.Contains(value2)))
			{
				continue;
			}
			if (!allJobDemand.suitableToAllSkills && allJobDemand.suitableSkillNames != null)
			{
				string[] suitableSkillNames = allJobDemand.suitableSkillNames;
				for (int i = 0; i < suitableSkillNames.Length; i++)
				{
					if (!(suitableSkillNames[i] != primarySkill))
					{
						PossibleDemands.Add(allJobDemand.demandName);
						break;
					}
				}
			}
			else
			{
				PossibleDemands.Add(allJobDemand.demandName);
			}
		}
		if (PossibleDemands.Count <= 0)
		{
			return string.Empty;
		}
		return PossibleDemands.GetRandom();
	}

	public static JobDemand GetByName(string demandName)
	{
		return AllJobDemandsByName[demandName];
	}

	public static bool IsEnvironmentDemand(string itemName, out List<string> demandNames)
	{
		return ItemsWithEnvironmentDemands.TryGetValue(itemName, out demandNames);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AllJobDemands = null;
		AllJobDemandsByName.Clear();
		ItemsWithEnvironmentDemands.Clear();
		JobDemandsBySpecificSkill.Clear();
		SingleSelectionGroupByDemandName.Clear();
		ActiveSingleSelectionGroups.Clear();
		ScheduleDemands.Clear();
		EquipmentDemands.Clear();
		EnvironmentDemands.Clear();
		TypeDemands.Clear();
		HealthInsuranceDemands.Clear();
		NonAdditiveDemands.Clear();
	}
}
