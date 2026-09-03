using BigAmbitions.Characters.Skills;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleEmployeeInfoBox : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private GameObject notFulfilledBackground;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private BasicTooltip tooltip;

	private void OnDisable()
	{
		tooltip.Hide();
	}

	public void SetUpDemand(JobDemand demand, string employeeId, ScheduleEmployeeModel data)
	{
		label.Key = demand.demandName;
		iconImage.gameObject.SetActive(value: false);
		bool flag = demand.Fulfilled(employeeId, ScheduleHelper.ScheduleDays);
		notFulfilledBackground.SetActive(!flag);
		string titleKey = demand.demandName + "_description";
		tooltip.titleKey = titleKey;
		if (demand is HoursWorkingPerWeek)
		{
			tooltip.descriptionKey = "bizman_schedule_employee_hours";
			tooltip.localizationArguments = new { data.hoursAssigned, data.hoursWorked };
		}
		else if (demand is DaysWorkingPerWeek)
		{
			tooltip.descriptionKey = "bizman_schedule_employee_days";
			tooltip.localizationArguments = new { data.daysAssigned, data.daysWorked };
		}
	}

	public void SetUpSkill(Skill skill)
	{
		tooltip.enabled = false;
		label.Key = "employees_skill_with_percentage";
		label.Arguments = new
		{
			skillName = skill.name.GetLocalization(),
			percentage = skill.GetRoundedValue()
		};
		iconImage.gameObject.SetActive(value: true);
		iconImage.sprite = SkillHelper.GetData(skill).icon28;
		notFulfilledBackground.SetActive(value: false);
	}
}
