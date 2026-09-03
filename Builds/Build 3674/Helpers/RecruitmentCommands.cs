using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using IngameDebugConsole;
using UnityEngine;

namespace Helpers;

public static class RecruitmentCommands
{
	[ConsoleMethod("employees.generatecandidate", "Generate a random candidate with the required skill", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidate(string skillName, int skillValue)
	{
		Command_GenerateCandidate(skillName, skillValue, partTime: true, fullTime: true);
	}

	[ConsoleMethod("employees.generatecandidate", "Generate a random candidate with the required skill", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidate(string skillName, int skillValue, int satisfaction)
	{
		Command_GenerateCandidate(skillName, skillValue, partTime: true, fullTime: true, satisfaction);
	}

	[ConsoleMethod("employees.generatecandidate", "Generate a random candidate with the required skill and schedule demands", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidate(string skillName, int skillValue, bool partTime, bool fullTime, int satisfaction = 50)
	{
		if (!SkillHelper.GetData(skillName).HasTag(TagRef.Skilltag.hashoursperweekdemand))
		{
			partTime = false;
			fullTime = false;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (partTime)
		{
			list2.Add("ba:jobdemand_parttime");
		}
		if (fullTime)
		{
			list2.Add("ba:jobdemand_fulltime");
		}
		if (list2.Count > 0)
		{
			list.Add(list2.GetRandom());
		}
		RecruitmentHelper.GenerateCandidate(skillName, skillValue, null, list).satisfaction = satisfaction;
	}

	[ConsoleMethod("employees.generatecandidatewithdemand", "Generate a candidate with the required skill carrying a specific job demand (e.g. a health insurance demand)", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills", "demandName=JobDemands" })]
	public static void Command_GenerateCandidateWithDemand(string skillName, int skillValue, string demandName)
	{
		RecruitmentHelper.GenerateCandidate(skillName, skillValue, null, new List<string> { demandName });
	}

	[ConsoleMethod("employees.generatecandidatefromrecruitmentagency", "Generate a candidate with Recruitment Agency as recruitment source", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromRecruitmentAgency(string skillName, int skillValue)
	{
		Command_GenerateCandidateFromRecruitmentAgency(skillName, skillValue, "");
	}

	[ConsoleMethod("employees.generatecandidatefromrecruitmentagency", "Generate a candidate with Recruitment Agency as recruitment source by agency business name", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromRecruitmentAgency(string skillName, int skillValue, string recruitmentAgencyName)
	{
		RecruitmentHelper.GenerateCandidate(skillName, skillValue, null).candidateInfo.sourceAddress = GetDebugRecruitmentAgencyAddress(recruitmentAgencyName);
	}

	[ConsoleMethod("employees.generatecandidatefromjobboard", "Generate a candidate with Job Board as recruitment source", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromJobBoard(string skillName, int skillValue)
	{
		Command_GenerateCandidateFromJobBoard(skillName, skillValue, "");
	}

	[ConsoleMethod("employees.generatecandidatefromjobboard", "Generate a candidate with Job Board as recruitment source by source business name", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromJobBoard(string skillName, int skillValue, string jobBoardBusinessName)
	{
		EmployeeInstance employeeInstance = RecruitmentHelper.GenerateCandidate(skillName, skillValue, null);
		employeeInstance.candidateInfo.fromJobBoard = true;
		employeeInstance.candidateInfo.sourceAddress = GetDebugJobBoardBusinessAddress(jobBoardBusinessName);
	}

	[ConsoleMethod("employees.generatecandidatefromheadhunter", "Generate a candidate with Headhunter as recruitment source", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromHeadhunter(string skillName, int skillValue)
	{
		Command_GenerateCandidateFromHeadhunter(skillName, skillValue, "");
	}

	[ConsoleMethod("employees.generatecandidatefromheadhunter", "Generate a candidate with Headhunter as recruitment source by headhunter name or id", new string[] { }, AutoCompleteMap = new string[] { "skillName=Skills" })]
	public static void Command_GenerateCandidateFromHeadhunter(string skillName, int skillValue, string headhunterNameOrId)
	{
		EmployeeInstance employeeInstance = RecruitmentHelper.GenerateCandidate(skillName, skillValue, null);
		EmployeeInstance debugHeadhunter = GetDebugHeadhunter(headhunterNameOrId);
		if (debugHeadhunter == null)
		{
			Debug.LogWarning("No hired headhunter found. Candidate generated without a headhunter source.");
		}
		else
		{
			employeeInstance.candidateInfo.sourceHeadhunterId = debugHeadhunter.id;
		}
	}

	private static Address GetDebugRecruitmentAgencyAddress(string recruitmentAgencyName)
	{
		if (!string.IsNullOrEmpty(recruitmentAgencyName))
		{
			BuildingRegistration buildingRegistration = SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_recruitmentagency" && string.Equals(x.BusinessName, recruitmentAgencyName, StringComparison.OrdinalIgnoreCase));
			if (buildingRegistration != null)
			{
				return buildingRegistration.Address;
			}
			Debug.LogWarning("No recruitment agency found with business name '" + recruitmentAgencyName + "'.");
		}
		return SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_recruitmentagency")?.Address;
	}

	private static Address GetDebugJobBoardBusinessAddress(string jobBoardBusinessName)
	{
		if (!string.IsNullOrEmpty(jobBoardBusinessName))
		{
			BuildingRegistration buildingRegistration = SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => string.Equals(x.BusinessName, jobBoardBusinessName, StringComparison.OrdinalIgnoreCase));
			if (buildingRegistration != null)
			{
				return buildingRegistration.Address;
			}
			Debug.LogWarning("No building registration found with business name '" + jobBoardBusinessName + "'.");
		}
		object obj = InstanceBehavior<BuildingManager>.Instance?.buildingRegistration?.Address;
		if (obj == null)
		{
			BuildingRegistration buildingRegistration2 = SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.RentedByPlayer);
			if (buildingRegistration2 == null)
			{
				return null;
			}
			obj = buildingRegistration2.Address;
		}
		return (Address)obj;
	}

	private static EmployeeInstance GetDebugHeadhunter(string headhunterNameOrId)
	{
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances();
		if (employeeInstances == null)
		{
			return null;
		}
		if (!string.IsNullOrEmpty(headhunterNameOrId))
		{
			EmployeeInstance employeeInstance = employeeInstances.FirstOrDefault((EmployeeInstance x) => x.id == headhunterNameOrId || string.Equals(x.characterData?.name, headhunterNameOrId, StringComparison.OrdinalIgnoreCase));
			if (employeeInstance != null)
			{
				return employeeInstance;
			}
			Debug.LogWarning("No employee found with name or id '" + headhunterNameOrId + "'.");
		}
		return employeeInstances.FirstOrDefault((EmployeeInstance x) => x.HasSkill("ba:skill_headhunter"));
	}
}
