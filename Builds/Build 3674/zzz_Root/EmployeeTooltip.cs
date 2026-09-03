using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using Entities;
using Entities.Employee.JobDemands;
using Enums;
using Localizor;
using TMPro;
using Tooltip;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;

public class EmployeeTooltip : TooltipTarget
{
	private const string AwaitingReplacementFromHeadhunterTextKey = "awaiting_replacement_from_headhunter";

	[HideInInspector]
	public EmployeeInstance employeeInstance;

	[HideInInspector]
	public List<DayOfWeekOrdered> overworkedDays;

	[SerializeField]
	private string tooltipLabelText = "tooltip_right_click_to_remove";

	protected override void ShowTooltip()
	{
		if (employeeInstance.isBeingReplaced)
		{
			ShowAwaitingReplacementTooltip();
			return;
		}
		List<Tuple<(string, Color), string>> elements = employeeInstance.characterData.skills.Select((Skill skill) => new Tuple<(string, Color), string>((skill.name, Color.white), $"{skill.GetRoundedValue()}%")).ToList();
		List<(string, (string, Color), bool)> elements2 = employeeInstance.demands.Select(GetDemandLabel).ToList();
		TooltipSystem.AddHeader(employeeInstance.characterData.name.Localize(), "common_years_amount".Localize(new
		{
			years = employeeInstance.Years
		}));
		TooltipSystem.AddSplitter();
		TooltipSystem.AddLabel("common_hours_per_week".Localize(new
		{
			weeklyHours = employeeInstance.assignedWeeklyHours
		}), InstanceBehavior<GlobalReferences>.Instance.colors.white, FontStyles.UpperCase);
		TooltipSystem.AddProgressBar(Mathf.CeilToInt(employeeInstance.satisfaction), InstanceBehavior<GlobalReferences>.Instance.colors.green);
		TooltipSystem.AddSplitter();
		TooltipSystem.AddList(elements);
		TooltipSystem.AddSplitter();
		TooltipSystem.AddDemandList(elements2);
		TooltipSystem.AddSplitter();
		if (overworkedDays.Count > 0)
		{
			object overworkedTooltipArguments = ScheduleHelper.GetOverworkedTooltipArguments(overworkedDays);
			TooltipSystem.AddLabel("bizman_schedule_employee_overworked_days".Localize(overworkedTooltipArguments), InstanceBehavior<GlobalReferences>.Instance.colors.yellow);
			TooltipSystem.AddSplitter();
		}
		TooltipSystem.AddLabel(tooltipLabelText.Localize(), InstanceBehavior<GlobalReferences>.Instance.colors.blue, FontStyles.UpperCase, TextAlignmentOptions.Midline);
	}

	private void ShowAwaitingReplacementTooltip()
	{
		TooltipSystem.AddHeader(employeeInstance.characterData.name.Localize());
		TooltipSystem.AddLabel("awaiting_replacement_from_headhunter".Localize(new
		{
			headhunterName = employeeInstance.GetAssignedHeadhunterPlan().HeadhunterInstance?.characterData.name
		}), InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	private (string, (string, Color), bool) GetDemandLabel(string demandName)
	{
		JobDemand byName = JobDemandHelper.GetByName(demandName);
		Color32 color = byName.priority switch
		{
			Priority.Low => InstanceBehavior<GlobalReferences>.Instance.colors.green, 
			Priority.Medium => InstanceBehavior<GlobalReferences>.Instance.colors.yellow, 
			Priority.High => InstanceBehavior<GlobalReferences>.Instance.colors.red, 
			_ => InstanceBehavior<GlobalReferences>.Instance.colors.black, 
		};
		bool item = byName.Fulfilled(employeeInstance, null);
		return (demandName, (byName.PriorityLocalizeKey, color), item);
	}
}
