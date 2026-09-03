using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using Entities.Employee.JobDemands;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class EmployeeFilterController : BaseFilterController<EmployeeModel>
{
	protected override void CreateToggles()
	{
		foreach (string skill in SkillHelper.AllSkillNames)
		{
			if (!string.IsNullOrEmpty(skill) && !(SkillHelper.GetData(skill) == null))
			{
				CreateToggle(delegate(EmployeeFilterToggle t)
				{
					t.ConfigureSkill(skill);
				}, FilterToggleGroup.Skill);
			}
		}
		foreach (string demand in JobDemandHelper.TypeDemands)
		{
			CreateToggle(delegate(EmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand);
			}, FilterToggleGroup.Type);
		}
		foreach (string demand2 in JobDemandHelper.EquipmentDemands)
		{
			CreateToggle(delegate(EmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand2);
			}, FilterToggleGroup.Equipment);
		}
		foreach (string demand3 in JobDemandHelper.ScheduleDemands)
		{
			CreateToggle(delegate(EmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand3);
			}, FilterToggleGroup.Schedule);
		}
		foreach (string demand4 in JobDemandHelper.HealthInsuranceDemands)
		{
			CreateToggle(delegate(EmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand4);
			}, FilterToggleGroup.HealthInsurance);
		}
		foreach (string status in EmployeeStatusFilter.All)
		{
			CreateToggle(delegate(EmployeeFilterToggle t)
			{
				t.ConfigureStatus(status);
			}, FilterToggleGroup.Status);
		}
	}

	protected override IEnumerable<string> GetSearchableText(EmployeeModel item)
	{
		yield return item.employeeName;
		yield return item.currentBusinessName;
	}
}
