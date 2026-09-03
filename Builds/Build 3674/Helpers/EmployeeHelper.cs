using System.Collections.Generic;
using System.Linq;
using AI.Employees;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using Buildings;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Localizor;
using UI;
using UI.Notification;
using UnityEngine;

namespace Helpers;

public static class EmployeeHelper
{
	public const int HoursUntilCandidateExpires = 168;

	public const int HourToFinishTrainings = 17;

	public const int RetirementAge = 67;

	public const int LowSatisfactionThreshold = 20;

	public const int EmployeeTrainingSkillIncrease = 10;

	public const float TrainingWageDiscount = 0.7f;

	public const int MaxDailyWorkHoursBeforeSickRisk = 14;

	private const int DailyWorkHoursUntilCertainSickness = 20;

	private const int MaxNewDemandsPerDay = 3;

	private const float RetirementNoticeTimeInYears = 1f;

	private const float MinSatisfactionForSkillIncrease = 80f;

	private const string AwaitingReplacementTextKey = "awaiting_replacement";

	public static readonly Dictionary<string, EmployeeInstance> EmployeeInstancesDictionary = new Dictionary<string, EmployeeInstance>();

	private static readonly List<EmployeeInstance> SickEmployees = new List<EmployeeInstance>();

	private static readonly List<EmployeeInstance> RetiredEmployees = new List<EmployeeInstance>();

	public static readonly Dictionary<EmployeeInstance, string> FinishedTrainingEmployees = new Dictionary<EmployeeInstance, string>();

	public static readonly List<(string, BuildingRegistration)> ResignedEmployeesAndLastRegistrationAssigned = new List<(string, BuildingRegistration)>();

	private static int DemandsGeneratedToday;

	public static bool IsInitialized;

	public static void EnsureInit(GameInstance gameInstance)
	{
		if (!IsInitialized)
		{
			Init(gameInstance);
		}
	}

	public static void Init(GameInstance gameInstance = null)
	{
		if (gameInstance == null)
		{
			gameInstance = SaveGameManager.Current;
		}
		EmployeeInstancesDictionary.Clear();
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			EmployeeInstancesDictionary.Add(employeeInstance.id, employeeInstance);
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			EmployeeInstancesDictionary.Add(candidateEmployeeInstance.id, candidateEmployeeInstance);
		}
		IsInitialized = true;
	}

	public static void PayDailyWages()
	{
		foreach (EmployeeInstance employeeInstance in GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}))
		{
			employeeInstance.PayWage();
		}
	}

	public static void WorkDaily()
	{
		foreach (EmployeeInstance employeeInstance in GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}))
		{
			employeeInstance.WorkDaily();
		}
	}

	public static void RunDaily()
	{
		DemandsGeneratedToday = 0;
		DayOfWeekOrdered today = TimeHelper.GetDayOfWeek(SaveGameManager.Current.Day);
		int num = 67 * SaveGameManager.Current.gameVariables.daysPerYear;
		List<EmployeeInstance> employeeInstances = GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		});
		for (int i = 0; i < employeeInstances.Count; i++)
		{
			EmployeeInstance employeeInstance = employeeInstances[i];
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress);
			if (TimeHelper.GetDayOfWeek() == DayOfWeekOrdered.Monday)
			{
				employeeInstance.workedDays = 0;
				employeeInstance.workedHoursThisWeek = 0;
			}
			employeeInstance.isAbsent = false;
			employeeInstance.isReplaced = false;
			employeeInstance.isTrainingDay = false;
			employeeInstance.characterData.ageInDays++;
			if (employeeInstance.characterData.ageInDays >= 67 * SaveGameManager.Current.gameVariables.daysPerYear)
			{
				if (!employeeInstance.ReplaceAutomaticallyOnRetire())
				{
					RetiredEmployees.Add(employeeInstance);
				}
				continue;
			}
			if (employeeInstance.HasExpiredPoachDeadline)
			{
				employeeInstance.PoachDeadlineExpired();
				continue;
			}
			if (employeeInstance.characterData.ageInDays == Mathf.FloorToInt((float)num - (float)SaveGameManager.Current.gameVariables.daysPerYear * 1f) && !employeeInstance.sendRetirementNotice)
			{
				int num2 = num - employeeInstance.characterData.ageInDays;
				int num3 = SaveGameManager.Current.Day + num2;
				Dictionary<string, string> data = new Dictionary<string, string> { 
				{
					"days",
					num3.ToString()
				} };
				employeeInstance.SendMessage("ba:messagetype_employee_contact_message_retirement_notice", data);
				employeeInstance.sendRetirementNotice = true;
			}
			if (employeeInstance.workedHoursToday > 14 && RngHelper.Chance(GetSickNextDayChance(employeeInstance.workedHoursToday)))
			{
				employeeInstance.nextSickDay = SaveGameManager.Current.Day;
			}
			ScheduleDay scheduleDay = buildingRegistration?.scheduleDays.FirstOrDefault((ScheduleDay x) => x.day == today);
			if (employeeInstance.nextSickDay <= SaveGameManager.Current.Day && scheduleDay != null && !buildingRegistration.temporarilyClosed && employeeInstance.GetWorkingHoursOnDay(scheduleDay) > 0)
			{
				employeeInstance.isAbsent = true;
				employeeInstance.nextSickDay = GetNextSickDay(employeeInstance);
				HrManagerPlan planFromId = HrManagerHelper.GetPlanFromId(employeeInstance.assignedHrManagerPlanId);
				if (planFromId != null && planFromId.replaceAbsentEmployees && !string.IsNullOrEmpty(planFromId.assignedEmployeeId))
				{
					employeeInstance.isReplaced = true;
				}
				else
				{
					SickEmployees.Add(employeeInstance);
				}
			}
			if (scheduleDay != null && !employeeInstance.isAbsent && employeeInstance.satisfaction >= 80f)
			{
				int num4 = employeeInstance.workedHoursToday;
				foreach (Skill skill in employeeInstance.characterData.skills)
				{
					if (employeeInstance.characterData.skills.Count > 1)
					{
						int num5 = ((!(skill.name == "ba:skill_cleaning")) ? employeeInstance.WorkingHoursOnDay(scheduleDay, WorkShiftType.Default) : employeeInstance.WorkingHoursOnDay(scheduleDay, WorkShiftType.Cleaning));
						num4 = num5;
					}
					skill.value = Mathf.Min(100f, skill.value + (float)num4 * Random.Range(0.01f, 0.02f));
				}
			}
			employeeInstance.workedHoursToday = 0;
			if (DemandsGeneratedToday < 3)
			{
				DemandsGeneratedToday += employeeInstance.UpdateDemands();
			}
			employeeInstance.complaintData.UpdateHoursUntilNextComplaintDueToSatisfaction(employeeInstance.satisfaction);
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (SickEmployees.Count > 3)
		{
			dictionary.Add("amount", SickEmployees.Count.ToString());
			Notifications.Show(NotificationType.Warning, "employeehelper_notification_employee_amount_called_in_sick", dictionary, 10f);
		}
		else
		{
			foreach (EmployeeInstance employeeInstance2 in SickEmployees)
			{
				dictionary.Clear();
				dictionary.Add("name", employeeInstance2.characterData.name);
				dictionary.Add("businessName", BuildingHelper.GetBuildingRegistration(employeeInstance2.assignedAddress).BusinessName);
				Notifications.Show(NotificationType.Warning, "employeehelper_notification_employee_called_in_sick", dictionary, 10f, null, delegate
				{
					OnClickShowEmployee(employeeInstance2);
				});
			}
		}
		foreach (EmployeeInstance retiredEmployee in RetiredEmployees)
		{
			BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(retiredEmployee.assignedAddress);
			string value = "common_unassigned";
			if (buildingRegistration2 != null)
			{
				value = buildingRegistration2.BusinessName;
			}
			retiredEmployee.SendMessage("ba:messagetype_employee_contact_message_retire");
			dictionary.Clear();
			dictionary.Add("name", retiredEmployee.characterData.name);
			dictionary.Add("businessName", value);
			Notifications.Show(NotificationType.Warning, "employeehelper_notification_employee_retired", dictionary, 10f);
			retiredEmployee.RemoveEmployee();
		}
		SickEmployees.Clear();
		RetiredEmployees.Clear();
		ComplaintHelper.UpdateDaysWhenEmployeesComplained();
	}

	public static void RunHourly()
	{
		EmployeeInstance.UpdateSatisfactionWork.ForceCompleteAllWork();
		List<EmployeeInstance> candidateEmployeeInstances = SaveGameManager.Current.CandidateEmployeeInstances;
		for (int num = candidateEmployeeInstances.Count - 1; num >= 0; num--)
		{
			EmployeeInstance employeeInstance = candidateEmployeeInstances[num];
			if (employeeInstance.IsCandidate)
			{
				employeeInstance.candidateInfo.hoursUntilExpiring--;
				if (employeeInstance.candidateInfo.hoursUntilExpiring <= 0)
				{
					EmployeeInstancesDictionary.Remove(employeeInstance.id);
					candidateEmployeeInstances.RemoveAt(num);
				}
			}
		}
		InstanceBehavior<UIs>.Instance.smartphoneUI?.UpdateBadgeCount(AppName.MyEmployees, playSound: false);
		List<EmployeeInstance> employeeInstances = GetEmployeeInstances();
		for (int num2 = employeeInstances.Count - 1; num2 >= 0; num2--)
		{
			EmployeeInstance employeeInstance2 = employeeInstances[num2];
			if (!employeeInstance2.isBeingReplaced)
			{
				employeeInstance2.RunHourly();
				if (!(employeeInstance2.satisfaction > 0f) && !employeeInstance2.isBeingReplaced)
				{
					EmployeeInstancesDictionary.Remove(employeeInstance2.id);
					employeeInstances.RemoveAt(num2);
				}
			}
		}
		if (SaveGameManager.Current.Hour == HealthInsuranceHelper.HourToSendOffer)
		{
			foreach (HealthInsurancePlanOffer item in SaveGameManager.Current.healthInsurancePlanOffers.ToList())
			{
				if (!item.negotiationFinished && item.dayToSendOffer == SaveGameManager.Current.Day)
				{
					item.SendOffer();
				}
			}
		}
		ShowFinishedTrainingEmployeeNotifications();
		FinishedTrainingEmployees.Clear();
		ShowResignedEmployeeNotifications();
		HeadhunterPlan.ShowReplacementNotifications();
	}

	private static void OnClickShowEmployee(EmployeeInstance employeeInstance)
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.canvas.isActiveAndEnabled)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(employeeInstance);
		}
	}

	private static void ShowFinishedTrainingEmployeeNotifications()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (FinishedTrainingEmployees.Count > 3)
		{
			dictionary.Add("amount", FinishedTrainingEmployees.Count.ToString());
			Notifications.Show(NotificationType.Info, "employeehelper_notification_employee_amount_finished_training", dictionary);
			return;
		}
		foreach (KeyValuePair<EmployeeInstance, string> keyValuePair in FinishedTrainingEmployees)
		{
			dictionary.Clear();
			string headerKey = ((keyValuePair.Key.characterData.gender == Gender.Female) ? "employeehelper_notification_employee_finished_training_female" : "employeehelper_notification_employee_finished_training_male");
			dictionary.Add("name", keyValuePair.Key.characterData.name);
			dictionary.Add("skill", keyValuePair.Value);
			Notifications.Show(NotificationType.Success, headerKey, dictionary, 4f, null, delegate
			{
				OnClickShowEmployee(keyValuePair.Key);
			});
		}
	}

	private static void ShowResignedEmployeeNotifications()
	{
		if (ResignedEmployeesAndLastRegistrationAssigned.Count > 3)
		{
			ShowMultipleResignNotification(ResignedEmployeesAndLastRegistrationAssigned.Count);
		}
		else
		{
			foreach (var item in ResignedEmployeesAndLastRegistrationAssigned)
			{
				ShowResignNotification(item.Item1, item.Item2);
			}
		}
		ResignedEmployeesAndLastRegistrationAssigned.Clear();
	}

	private static void ShowResignNotification(string employeeName, BuildingRegistration business)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{ "employeeName", employeeName },
			{
				"businessName",
				(business != null) ? business.BusinessName : "common_unassigned"
			}
		};
		Notifications.Show(NotificationType.Error, "employeehelper_notification_emloyee_resigned", notificationData);
	}

	private static void ShowMultipleResignNotification(int numberOfEmployees)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"amount",
			numberOfEmployees.ToString()
		} };
		Notifications.Show(NotificationType.Error, "employeehelper_notification_emloyee_resigned_multiple", notificationData);
	}

	public static float CalculateHourlyWageForSkill(Skill skill)
	{
		float baseHourlyWage = SkillHelper.GetData(skill).baseHourlyWage;
		float skillWageBoost = GetSkillWageBoost(skill.value);
		float employeeHourlySalaryMultiplier = SaveGameManager.Current.gameVariables.employeeHourlySalaryMultiplier;
		return baseHourlyWage * skillWageBoost * employeeHourlySalaryMultiplier;
	}

	public static float CalculateHourlyWageForSkill(string skillName, float skillValue)
	{
		float baseHourlyWage = SkillHelper.GetData(skillName).baseHourlyWage;
		float skillWageBoost = GetSkillWageBoost(skillValue);
		float employeeHourlySalaryMultiplier = SaveGameManager.Current.gameVariables.employeeHourlySalaryMultiplier;
		return baseHourlyWage * skillWageBoost * employeeHourlySalaryMultiplier;
	}

	public static float GetSkillWageBoost(float skillValue)
	{
		return Mathf.Pow(skillValue, 1.05f) / 100f + 1f;
	}

	public static void UnassignEmployeeFromAllWorkshifts(EmployeeInstance employeeInstance)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress);
		if (employeeInstance.HasSkill("ba:skill_deliverydriver"))
		{
			if (buildingRegistration is Warehouse warehouse)
			{
				VehicleSlot vehicleSlot = warehouse.vehicleSlots.FirstOrDefault((VehicleSlot x) => x.employeeDriverId == employeeInstance.id);
				if (vehicleSlot != null)
				{
					vehicleSlot.employeeDriverId = null;
					employeeInstance.UpdateWeeklyHoursAndDays();
				}
			}
		}
		else if (buildingRegistration?.scheduleDays != null)
		{
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				if (scheduleDay.workShifts != null)
				{
					scheduleDay.RemoveAllWorkShiftsThatMatchPredicate((WorkShift x) => x.employeeId == employeeInstance.id);
				}
			}
		}
		employeeInstance.UnAssignWork();
		Address assignedAddress = employeeInstance.assignedAddress;
		employeeInstance.assignedAddress = null;
		CustomerDemandHelper.ReloadCachedFulfilled(assignedAddress);
		employeeInstance.AddTodoTask(TodoTaskType.EmployeeIdle);
	}

	public static EmployeeInstance GetEmployeeAtStationAndHour(BuildingRegistration registration, string stationId, int hour = -1)
	{
		if (PlayerHelper.IsPlayerWorkingInEmployeeStation(stationId))
		{
			return PlayerHelper.GetPlayerEmployeeInstance();
		}
		ScheduleDay todaySchedule = BuildingHelper.GetTodaySchedule(registration);
		if (todaySchedule?.workShifts == null)
		{
			return null;
		}
		if (hour == -1)
		{
			hour = SaveGameManager.Current.Hour;
		}
		foreach (WorkShift workShift in todaySchedule.workShifts)
		{
			if (!(workShift.itemInstanceId != stationId) && hour.InRange(workShift.startingHour, workShift.endingHour - 1))
			{
				return (workShift.employeeId == null) ? null : GetEmployeeById(workShift.employeeId);
			}
		}
		return null;
	}

	public static bool IsEmployeeStationEmployedAtHour(BuildingRegistration registration, string stationId, int hour)
	{
		if (PlayerHelper.IsPlayerWorkingInEmployeeStation(stationId))
		{
			return true;
		}
		ScheduleDay todaySchedule = BuildingHelper.GetTodaySchedule(registration);
		if (todaySchedule?.workShifts == null)
		{
			return false;
		}
		foreach (WorkShift workShift in todaySchedule.workShifts)
		{
			if (!(workShift.itemInstanceId != stationId) && hour.InRange(workShift.startingHour, workShift.endingHour - 1))
			{
				return workShift.employeeId != null;
			}
		}
		return false;
	}

	public static float GetTrainingCost(EmployeeInstance employeeInstance, string skillName, int skillIncrease)
	{
		SkillData data = SkillHelper.GetData(skillName);
		return 10f * data.trainingCostMultiplier * ((100f + employeeInstance.GetSkillValue(skillName)) / 50f) * (float)skillIncrease;
	}

	private static float GetSickNextDayChance(int workedHoursToday)
	{
		return Mathf.InverseLerp(14f, 20f, workedHoursToday) * 100f;
	}

	public static int GetNextSickDay(EmployeeInstance employeeInstance)
	{
		int num = Mathf.RoundToInt(employeeInstance.satisfaction * 0.8f);
		return SaveGameManager.Current.Day + Random.Range(10 + num, 20 + num);
	}

	public static void ForceEmployeeSickNextDay(EmployeeInstance employeeInstance)
	{
		employeeInstance.nextSickDay = SaveGameManager.Current.Day + 1;
	}

	public static EmployeeInstance GetEmployeeById(string employeeId, bool showError = true)
	{
		if (string.IsNullOrEmpty(employeeId))
		{
			return null;
		}
		if (!EmployeeInstancesDictionary.TryGetValue(employeeId, out var value))
		{
			if (showError)
			{
				Debug.LogError("Employee with ID " + employeeId + " not found");
			}
			return null;
		}
		return value;
	}

	public static List<AppearanceElementData> GetUniformElements(List<string> employeeSkills, Gender gender)
	{
		EmployeePreset employeePreset = null;
		SpecialService specialService = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BuildingCached.SpecialService;
		if (specialService != null)
		{
			employeePreset = specialService.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && employeeSkills.Exists((string skill) => skill == x.skill)) ?? specialService.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent);
		}
		if (employeePreset == null)
		{
			BusinessType data = BusinessTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
			employeePreset = data.uniforms.FirstOrDefault((EmployeePreset x) => x.skillDependent && employeeSkills.Exists((string skill) => skill == x.skill)) ?? data.uniforms.FirstOrDefault((EmployeePreset x) => !x.skillDependent);
		}
		if (gender != Gender.Female)
		{
			return employeePreset?.maleElements;
		}
		return employeePreset?.femaleElements;
	}

	public static EmployeeInstance CreateAIEmployeeInstance(string primarySkillName)
	{
		return new EmployeeInstance
		{
			characterData = new CharacterData
			{
				skills = new List<Skill>
				{
					new Skill
					{
						name = primarySkillName,
						value = 50f
					}
				},
				gender = RngHelper.GetRandomEnum<Gender>()
			},
			satisfaction = 100f
		};
	}

	public static void HireCandidate(EmployeeInstance candidate)
	{
		if (GetEmployeeInstances().Contains(candidate))
		{
			Debug.LogWarning("Ignored duplicate hire of '" + candidate.characterData.name + "'.");
			return;
		}
		SaveGameManager.Current.CandidateEmployeeInstances.Remove(candidate);
		EmployeeInstancesDictionary.TryAdd(candidate.id, candidate);
		FinishPendingNegotiation(candidate, accepted: true);
		GetEmployeeInstances().Add(candidate);
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.MyEmployees, playSound: false);
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.UpdateBadge();
		candidate.candidateInfo = null;
		candidate.dayHired = SaveGameManager.Current.Day;
		candidate.nextSickDay = GetNextSickDay(candidate);
		candidate.AddTodoTask((!candidate.IsAssignedToAnyBusiness()) ? TodoTaskType.EmployeeUnassigned : TodoTaskType.EmployeeIdle, invokeQuestAffectingChange: false);
		candidate.complaintData.ResetHoursUntilNextComplaint();
		GameEvent.Invoke("ba:gameevent_employeehired");
		HappinessHelper.AddModifier("ba:happinessmodifier_first_employee");
	}

	public static void DiscardCandidate(EmployeeInstance candidate)
	{
		SaveGameManager.Current.CandidateEmployeeInstances.Remove(candidate);
		EmployeeInstancesDictionary.Remove(candidate.id);
		FinishPendingNegotiation(candidate, accepted: false);
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.MyEmployees, playSound: false);
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.UpdateBadge();
	}

	private static void FinishPendingNegotiation(EmployeeInstance candidate, bool accepted)
	{
		CandidateSalaryNegotiation candidateSalaryNegotiation = SaveGameManager.Current.candidateSalaryNegotiations.FirstOrDefault((CandidateSalaryNegotiation x) => x.employeeInstance == candidate);
		if (candidateSalaryNegotiation != null)
		{
			candidateSalaryNegotiation.accepted = accepted;
			candidateSalaryNegotiation.completed = true;
		}
	}

	public static string GetAwaitingReplacementText()
	{
		return "awaiting_replacement".Localize().ToString();
	}

	public static List<EmployeeInstance> GetEmployeeInstances()
	{
		return SaveGameManager.Current?.EmployeeInstances;
	}

	public static List<EmployeeInstance> GetEmployeeInstances(EmployeeInstancesQueryInfo queryInfo, List<EmployeeInstance> listToFill = null)
	{
		List<EmployeeInstance> list = listToFill ?? new List<EmployeeInstance>();
		list.Clear();
		List<EmployeeInstance> employeeInstances = GetEmployeeInstances();
		if (employeeInstances == null)
		{
			return list;
		}
		Address withAssignedAddress = queryInfo.withAssignedAddress;
		bool flag = withAssignedAddress != null;
		for (int i = 0; i < employeeInstances.Count; i++)
		{
			EmployeeInstance employeeInstance = employeeInstances[i];
			if ((flag && employeeInstance.assignedAddress != withAssignedAddress) || (queryInfo.excludeAbsentsNotReplaced && employeeInstance.isAbsent && !employeeInstance.isReplaced) || (queryInfo.excludeBeingReplaced && employeeInstance.isBeingReplaced) || (queryInfo.isAssignedToAnyWorkShift && !employeeInstance.IsAssignedToAnyWorkShift()) || (queryInfo.isAssignedToAnyBusiness && !employeeInstance.IsAssignedToAnyBusiness()))
			{
				continue;
			}
			string[] withSkills = queryInfo.withSkills;
			if (withSkills != null && withSkills.Length > 0)
			{
				bool flag2 = false;
				withSkills = queryInfo.withSkills;
				foreach (string skill in withSkills)
				{
					if (employeeInstance.HasSkill(skill))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					continue;
				}
			}
			withSkills = queryInfo.withoutSkills;
			if (withSkills != null && withSkills.Length > 0)
			{
				bool flag3 = false;
				withSkills = queryInfo.withoutSkills;
				foreach (string skill2 in withSkills)
				{
					if (employeeInstance.HasSkill(skill2))
					{
						flag3 = true;
						break;
					}
				}
				if (flag3)
				{
					continue;
				}
			}
			list.Add(employeeInstance);
		}
		return list;
	}

	public static float GetSkillOfEmployee(string assignedEmployeeId, string skillName)
	{
		return GetEmployeeById(assignedEmployeeId).GetSkillValue(skillName);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsInitialized = false;
		EmployeeInstancesDictionary.Clear();
		SickEmployees.Clear();
		RetiredEmployees.Clear();
		FinishedTrainingEmployees.Clear();
		ResignedEmployeesAndLastRegistrationAssigned.Clear();
		DemandsGeneratedToday = 0;
	}
}
