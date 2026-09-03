using System.Collections.Generic;
using BaTable;
using BigAmbitions.Characters.Skills;
using Entities.Employee.JobDemands;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleEmployeeCellView : BaTableCellView<ScheduleEmployeeModel>
{
	[Header("Schedule employee")]
	[SerializeField]
	private ScheduleEmployeeInfoBox infoBoxPrefab;

	[Header("Parents")]
	[SerializeField]
	private Transform demandsParent;

	[SerializeField]
	private Transform skillsParent;

	[Header("UI Elements")]
	[SerializeField]
	private Image background;

	[SerializeField]
	private Button selectButton;

	[SerializeField]
	private ButtonEffects selectButtonEffects;

	[Header("Top")]
	[SerializeField]
	private TMP_Text employeeNameLabel;

	[SerializeField]
	private TMP_Text satisfactionLabel;

	[SerializeField]
	private TextLocalizationComponent scheduledLabel;

	[SerializeField]
	private BasicTooltip scheduledHoursTooltip;

	[SerializeField]
	private TextLocalizationComponent hourlyWageLabel;

	[Header("Bottom")]
	[SerializeField]
	private GameObject warningObj;

	[SerializeField]
	private Image warningIcon;

	[SerializeField]
	private BasicTooltip warningTooltip;

	[Header("Overlays")]
	[SerializeField]
	private GameObject notAvailableOverlay;

	[SerializeField]
	private Image hoverOverlay;

	private readonly List<ScheduleEmployeeInfoBox> _infoBoxes = new List<ScheduleEmployeeInfoBox>();

	private ScheduleEmployeeModel _data;

	private Color? _defaultWarningColor;

	public override void SetData(ScheduleEmployeeModel data)
	{
		_data = data;
		ResetInfoBoxes();
		SetBasicInfo(data);
		SetDemands(data);
		SetSkills(data);
		hoverOverlay.gameObject.SetActive(value: false);
		selectButton.onClick.RemoveAllListeners();
		selectButtonEffects.enabled = !data.isEmployeeList;
		selectButton.enabled = !data.isEmployeeList;
		if (data.isEmployeeList)
		{
			return;
		}
		selectButton.onClick.AddListener(delegate
		{
			if (!data.isAvailable)
			{
				Notifications.ShowError("bizman_schedule_employee_not_available", null, trackOnSaveGame: false);
			}
			else
			{
				data.onEmployeeSelected(data.employeeId);
			}
		});
	}

	private void ResetInfoBoxes()
	{
		_infoBoxes.ForEach(delegate(ScheduleEmployeeInfoBox x)
		{
			Object.Destroy(x.gameObject);
		});
		_infoBoxes.Clear();
	}

	private void SetBasicInfo(ScheduleEmployeeModel data)
	{
		employeeNameLabel.text = data.employeeName;
		satisfactionLabel.text = $"{Mathf.RoundToInt(data.satisfaction)}%";
		scheduledLabel.Arguments = new
		{
			hours = data.hoursAssigned,
			days = data.daysAssigned
		};
		hourlyWageLabel.Arguments = new
		{
			wage = data.hourlyWage.ToShortCurrencyFormat()
		};
		scheduledHoursTooltip.localizationArguments = new
		{
			hours = data.hoursAssigned,
			days = data.daysAssigned,
			hoursWorked = data.hoursWorked,
			daysWorked = data.daysWorked
		};
		notAvailableOverlay.SetActive(!data.isAvailable);
		background.color = data.backgroundColor;
		hoverOverlay.color = GetHoverColor(data.backgroundColor);
		UpdateWarning(data);
	}

	private void UpdateWarning(ScheduleEmployeeModel data)
	{
		bool flag = !data.violatedDemandName.IsNullOrEmpty();
		bool flag2 = data.overworkedDays.Count > 0;
		bool flag3 = flag | flag2;
		warningObj.SetActive(flag3);
		if (flag3)
		{
			if (!_defaultWarningColor.HasValue)
			{
				_defaultWarningColor = warningIcon.color;
			}
			warningIcon.color = (flag ? ((Color)InstanceBehavior<GlobalReferences>.Instance.colors.red) : _defaultWarningColor.Value);
			if (flag)
			{
				warningTooltip.descriptionKey = "bizman_schedule_employee_demand_not_met";
				warningTooltip.localizationArguments = new
				{
					violatedDemandName = data.violatedDemandName.GetLocalization()
				};
			}
			else
			{
				warningTooltip.descriptionKey = "bizman_schedule_employee_overworked_days";
				warningTooltip.localizationArguments = ScheduleHelper.GetOverworkedTooltipArguments(data.overworkedDays);
			}
		}
	}

	private static Color GetHoverColor(Color backgroundColor)
	{
		Color.RGBToHSV(backgroundColor, out var H, out var S, out var V);
		float num = V + 0.5f * (0.5f - V);
		float num2 = Mathf.Abs(num - 0.5f) / 0.5f;
		float num3 = 1f - num2 * num2;
		float value = num + 0.3f * num3 * (1f - num);
		value = Mathf.Clamp01(value);
		return Color.HSVToRGB(H, S, value);
	}

	private void SetDemands(ScheduleEmployeeModel data)
	{
		foreach (string demand in data.demands)
		{
			ScheduleEmployeeInfoBox scheduleEmployeeInfoBox = Object.Instantiate(infoBoxPrefab, demandsParent);
			JobDemand byName = JobDemandHelper.GetByName(demand);
			scheduleEmployeeInfoBox.SetUpDemand(byName, data.employeeId, data);
			_infoBoxes.Add(scheduleEmployeeInfoBox);
		}
	}

	private void SetSkills(ScheduleEmployeeModel data)
	{
		foreach (Skill skill in data.skills)
		{
			ScheduleEmployeeInfoBox scheduleEmployeeInfoBox = Object.Instantiate(infoBoxPrefab, skillsParent);
			scheduleEmployeeInfoBox.SetUpSkill(skill);
			_infoBoxes.Add(scheduleEmployeeInfoBox);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		if (_data.isAvailable && !_data.isEmployeeList)
		{
			hoverOverlay.gameObject.SetActive(value: true);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
		if (_data.isAvailable && !_data.isEmployeeList)
		{
			hoverOverlay.gameObject.SetActive(value: false);
		}
	}
}
