using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using Entities.Employee.JobDemands;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class CandidateFilterController : BaseFilterController<CandidateModel>
{
	protected override void CreateToggles()
	{
		foreach (string skill in SkillHelper.AllSkillNames)
		{
			if (!string.IsNullOrEmpty(skill) && !(SkillHelper.GetData(skill) == null))
			{
				CreateToggle(delegate(CandidateFilterToggle t)
				{
					t.ConfigureSkill(skill);
				}, FilterToggleGroup.Skill);
			}
		}
		foreach (string demand in JobDemandHelper.TypeDemands)
		{
			CreateToggle(delegate(CandidateFilterToggle t)
			{
				t.ConfigureDemand(demand);
			}, FilterToggleGroup.Type);
		}
		foreach (string demand2 in JobDemandHelper.EquipmentDemands)
		{
			CreateToggle(delegate(CandidateFilterToggle t)
			{
				t.ConfigureDemand(demand2);
			}, FilterToggleGroup.Equipment);
		}
		foreach (string demand3 in JobDemandHelper.ScheduleDemands)
		{
			CreateToggle(delegate(CandidateFilterToggle t)
			{
				t.ConfigureDemand(demand3);
			}, FilterToggleGroup.Schedule);
		}
		foreach (string demand4 in JobDemandHelper.HealthInsuranceDemands)
		{
			CreateToggle(delegate(CandidateFilterToggle t)
			{
				t.ConfigureDemand(demand4);
			}, FilterToggleGroup.HealthInsurance);
		}
	}

	protected override IEnumerable<string> GetSearchableText(CandidateModel item)
	{
		yield return item.employeeName;
	}
}
