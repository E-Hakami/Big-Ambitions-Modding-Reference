using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class SkillLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "BigAmbitions.Characters.Skills.SkillName, BigAmbitions.Characters" };

	public override List<string> Keys => new List<string>
	{
		"Skill.name", "HasEmployeeWithSkill.skillName", "SkillData.skillName", "SkillForce.skillName", "Item.suitableSkills", "BusinessType.employeePrimarySkills", "BuildingTypeData.requiredBuildingSkills", "AiBusinessEmployeeData.primarySkillName", "TrainingInstance.skill", "JobDemand.suitableSkillNames",
		"BusinessEmployeeController.employeeSkill", "EmployeeInstancesQueryInfo.withSkills", "EmployeeInstancesQueryInfo.withoutSkills", "CasinoGameController.employeeSkill", "RecruitmentAgencySettings.availableEmployeeSkills", "WorkStationInfo.skillsRequired", "DataHolder.skillName", "EmploymentGoal.skill", "SkillRequirement.skillName", "RecruitmentSettings.selectedSkill",
		"RecruitmentSettings.businessSkills", "Headhunter.skillRecruiting", "EmployeePreset.skill", "HasEmployeeAssigned.skillName", "HasEmployeeAssignedWithSkill.skillNames", "RecruitmentAgencyTargets.skillNameTarget", "HeadhunterPlan.skillRecruiting", "BuildingRegistration.uniformsBySkill.key"
	};

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:skill_customerservice" },
		{ 1, "ba:skill_cleaning" },
		{ 2, "ba:skill_lawyer" },
		{ 3, "ba:skill_purchasingagent" },
		{ 4, "ba:skill_logisticsmanager" },
		{ 5, "ba:skill_deliverydriver" },
		{ 6, "ba:skill_programmer" },
		{ 7, "ba:skill_hrmanager" },
		{ 8, "ba:skill_graphicdesigner" },
		{ 9, "ba:skill_negotiation" },
		{ 10, "ba:skill_dj" },
		{ 11, "ba:skill_hairstylist" },
		{ 12, "ba:skill_securityguard" },
		{ 13, "ba:skill_headhunter" },
		{ 14, "ba:skill_factoryworker" },
		{ 15, "ba:skill_gymtrainer" },
		{ 17, "ba:skill_actor" },
		{ 18, "ba:skill_stagecrew" },
		{ 19, "ba:skill_projectionist" },
		{ 20, "ba:skill_travelagent" },
		{ 21, "ba:skill_eventplanner" }
	};
}
