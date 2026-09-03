using System.Collections.Generic;
using Entities.Employee.JobDemands;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public sealed class ScheduleEmployeeFilterController : BaseFilterController<ScheduleEmployeeModel>
{
	protected override void CreateToggles()
	{
		foreach (string demand in JobDemandHelper.TypeDemands)
		{
			CreateToggle(delegate(ScheduleEmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand);
			}, FilterToggleGroup.Type);
		}
		foreach (string demand2 in JobDemandHelper.EquipmentDemands)
		{
			CreateToggle(delegate(ScheduleEmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand2);
			}, FilterToggleGroup.Equipment);
		}
		foreach (string demand3 in JobDemandHelper.ScheduleDemands)
		{
			CreateToggle(delegate(ScheduleEmployeeFilterToggle t)
			{
				t.ConfigureDemand(demand3);
			}, FilterToggleGroup.Schedule);
		}
	}

	protected override IEnumerable<string> GetSearchableText(ScheduleEmployeeModel item)
	{
		yield return item.employeeName;
	}
}
