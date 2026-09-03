using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Entities;
using Entities.Employee.JobDemands;
using Extensions;
using Helpers;
using UI.Notification;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public class HeadhunterPlan
{
	private static readonly List<string> DemandsToIgnore = new List<string>();

	public readonly string id = UuidHelper.GenerateBase64Uuid();

	public string assignedEmployeeId;

	public Address headquartersAddress;

	public string[] assignedHrPlans = new string[2];

	public bool isRecruiting;

	public string skillRecruiting = "ba:skill_customerservice";

	public float skillValueTarget = 10f;

	public List<string> dealBreakerTypes = new List<string>();

	public Timestamp nextRecruit;

	public bool automaticallyReplaceOnRetire;

	public bool automaticallyReplaceOnResign;

	public int remainingCandidatesToRecruit = -1;

	public int amountOfCandidatesToRecruitPreference = -1;

	public List<HeadhunterReplacementData> headhunterReplacementDataList = new List<HeadhunterReplacementData>();

	private List<(string, string)> _replacementsThisHour = new List<(string, string)>();

	public EmployeeInstance HeadhunterInstance => EmployeeHelper.GetEmployeeById(assignedEmployeeId);

	public float HeadhunterSkillValue => HeadhunterInstance?.GetSkillValue("ba:skill_headhunter") ?? 0f;

	public float HoursPerRecruit => 4.25f - HeadhunterSkillValue / 25f;

	public int AvailableDealBreakersPoints => HeadhunterSkillValue.CalculateMaxDealBreakersPoints();

	public int MinSkillTarget => Mathf.FloorToInt(Mathf.Clamp(skillValueTarget - 5f, 0f, 100f));

	public int MaxSkillTarget => Mathf.CeilToInt(Mathf.Clamp(skillValueTarget + 5f, 0f, 100f));

	public int NumberOfAssignedHrPlans => assignedHrPlans.Count((string x) => x != null);

	public int MaxHrPlansThatCanBeAssigned => HeadhunterSkillValue.CalculateMaxHrPlans();

	private List<(string, string)> GetReplacementsThisHour()
	{
		return _replacementsThisHour ?? (_replacementsThisHour = new List<(string, string)>());
	}

	public (float, float) GetWageRangeForSkill(string selectedSkillToRecruit)
	{
		int num = MinSkillTarget;
		int num2 = MaxSkillTarget;
		SkillData data = SkillHelper.GetData(selectedSkillToRecruit);
		if (!string.IsNullOrEmpty(data.secondarySkill))
		{
			num += data.secondarySkillRange.x;
			num2 += data.secondarySkillRange.y;
		}
		float item = EmployeeHelper.CalculateHourlyWageForSkill(selectedSkillToRecruit, num);
		float item2 = EmployeeHelper.CalculateHourlyWageForSkill(selectedSkillToRecruit, num2);
		return (item, item2);
	}

	public void StartRecruiting()
	{
		isRecruiting = true;
		remainingCandidatesToRecruit = amountOfCandidatesToRecruitPreference;
		nextRecruit = TimeHelper.Now();
		if (nextRecruit.Hour < 8)
		{
			nextRecruit.Hour = 8;
		}
		nextRecruit.AddMinutes(Mathf.CeilToInt(HoursPerRecruit * 60f));
		SetNextAvailableRecruitHour();
	}

	public void CheckForRecruiting()
	{
		if (!isRecruiting)
		{
			return;
		}
		while (nextRecruit.IsInThePast())
		{
			int num = Random.Range(MinSkillTarget, MaxSkillTarget + 1);
			float getRandomSecondarySkillInitialValue = RecruitmentHelper.GetRandomSecondarySkillInitialValue;
			List<string> list = ((!string.IsNullOrEmpty(SkillHelper.GetData(skillRecruiting).secondarySkill)) ? GetRandomDemandsForCandidate((float)num + getRandomSecondarySkillInitialValue) : GetRandomDemandsForCandidate(num));
			if (list == null)
			{
				HeadhunterInstance?.SendMessage("ba:messagetype_headhunter_couldnt_find_employee_with_requirements");
				isRecruiting = false;
				return;
			}
			RecruitmentHelper.GenerateCandidate(skillRecruiting, num, null, list, getRandomSecondarySkillInitialValue).candidateInfo.sourceHeadhunterId = id;
			if (remainingCandidatesToRecruit != -1)
			{
				remainingCandidatesToRecruit--;
				if (remainingCandidatesToRecruit <= 0)
				{
					isRecruiting = false;
					return;
				}
			}
			nextRecruit.AddMinutes(Mathf.CeilToInt(HoursPerRecruit * 60f));
		}
		SetNextAvailableRecruitHour();
	}

	private List<string> GetRandomDemandsForCandidate(float totalSkillValue)
	{
		int num = JobDemandHelper.GetIdealNumberOfDemands(skillRecruiting, totalSkillValue);
		List<string> list = new List<string>();
		if (num == 0)
		{
			return list;
		}
		SkillData data = SkillHelper.GetData(skillRecruiting);
		if (data.HasTag(TagRef.Skilltag.forcefulltime))
		{
			list.Add("ba:jobdemand_fulltime");
			num--;
		}
		else if (data.HasTag(TagRef.Skilltag.hashoursperweekdemand))
		{
			bool flag = dealBreakerTypes.Contains("ba:headhuntersdealbreaker_parttime");
			bool flag2 = dealBreakerTypes.Contains("ba:headhuntersdealbreaker_fulltime");
			if (flag & flag2)
			{
				return null;
			}
			if (flag)
			{
				list.Add("ba:jobdemand_fulltime");
			}
			else if (flag2)
			{
				list.Add("ba:jobdemand_parttime");
			}
			else
			{
				string randomHoursPerWeekDemandForSkill = JobDemandHelper.GetRandomHoursPerWeekDemandForSkill(skillRecruiting);
				if (string.IsNullOrEmpty(randomHoursPerWeekDemandForSkill))
				{
					return null;
				}
				list.Add(randomHoursPerWeekDemandForSkill);
			}
			num--;
		}
		string randomJobSpecificDemandForSkill = JobDemandHelper.GetRandomJobSpecificDemandForSkill(skillRecruiting);
		if (!string.IsNullOrEmpty(randomJobSpecificDemandForSkill))
		{
			list.Add(randomJobSpecificDemandForSkill);
			num--;
		}
		DemandsToIgnore.Clear();
		if (skillRecruiting == "ba:skill_hrmanager")
		{
			DemandsToIgnore.AddRange(JobDemandHelper.HealthInsuranceDemands);
		}
		foreach (string dealBreakerType in dealBreakerTypes)
		{
			DemandsToIgnore.AddRange(HeadhunterHelper.GetData(dealBreakerType).applicableJobDemands);
		}
		while (num > 0)
		{
			string randomDemandForSkill = JobDemandHelper.GetRandomDemandForSkill(skillRecruiting, list, DemandsToIgnore);
			if (string.IsNullOrEmpty(randomDemandForSkill))
			{
				break;
			}
			list.Add(randomDemandForSkill);
			num--;
		}
		if (num > 0)
		{
			return null;
		}
		return list;
	}

	public void SetNextAvailableRecruitHour()
	{
		if (nextRecruit.Hour >= 15)
		{
			nextRecruit.AddHours(16);
		}
		switch (TimeHelper.GetDayOfWeek(nextRecruit.Day))
		{
		case DayOfWeekOrdered.Saturday:
			nextRecruit.Day += 2;
			break;
		case DayOfWeekOrdered.Sunday:
			nextRecruit.Day++;
			break;
		}
	}

	public bool SetEmployeeToReplace(EmployeeInstance employeeInstance)
	{
		string value = HeadhunterInstance?.characterData.name ?? "ba:transaction_replacementfee";
		Dictionary<string, string> data = new Dictionary<string, string> { { "employee", value } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_replacementfee", "ba:transactioncategory_headhunterreplacementfees", data);
		transactionInfo.SetTaxDeductibleName("ba:transaction_replacementfee_label");
		Address address = headquartersAddress;
		if (!GameManager.ChangeMoneySafe(-2500f, transactionInfo, null, address))
		{
			employeeInstance.SendMessage("ba:messagetype_headhunter_insufficient_funds", null, isSpecial: true);
			return false;
		}
		employeeInstance.ResignBeforeReplacement();
		ReplacementReason replacementReason = employeeInstance.GetReplacementReason();
		int num = HeadhunterHelper.GetWorkHoursUntilReplacement(replacementReason);
		if (employeeInstance.id == assignedEmployeeId)
		{
			num = 0;
		}
		if (num == 0)
		{
			ReplaceEmployee(employeeInstance);
		}
		else
		{
			SendReplacementMessage(employeeInstance, replacementReason);
			headhunterReplacementDataList.Add(new HeadhunterReplacementData
			{
				employeeInstance = employeeInstance,
				hoursUntilReplacement = num
			});
			employeeInstance.isBeingReplaced = true;
		}
		return true;
	}

	private static void SendReplacementMessage(EmployeeInstance employeeInstance, ReplacementReason replacementReason)
	{
		Vector2Int daysUntilReplacement = HeadhunterHelper.GetDaysUntilReplacement(replacementReason);
		string messageKey = "ba:messagetype_headhunter_expected_completion_1_day";
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (daysUntilReplacement.x != daysUntilReplacement.y)
		{
			dictionary.Add("startingDay", daysUntilReplacement.x.ToString());
			dictionary.Add("days", daysUntilReplacement.y.ToString());
			messageKey = "ba:messagetype_headhunter_expected_completion_days_range";
		}
		employeeInstance.SendMessage(messageKey, dictionary, isSpecial: true);
	}

	private void ReplaceEmployee(EmployeeInstance employeeInstance)
	{
		string name = employeeInstance.characterData.name;
		var (gender, text) = RecruitmentHelper.GetRandomGenderAndName();
		employeeInstance.characterData.gender = gender;
		employeeInstance.characterData.name = text;
		employeeInstance.satisfaction = ((employeeInstance.demands.Count == 0) ? 100 : 50);
		employeeInstance.ResetEmployee();
		if (employeeInstance.demands.Count > 0 && HeadhunterSkillValue < 100f)
		{
			ReRollNonScheduleDemands(employeeInstance);
		}
		headhunterReplacementDataList.RemoveAll((HeadhunterReplacementData x) => x.employeeInstance == employeeInstance);
		GetReplacementsThisHour().Add((name, text));
	}

	public static void ShowReplacementNotifications()
	{
		List<HeadhunterPlan> headhunterPlans = SaveGameManager.Current.headhunterPlans;
		if (headhunterPlans.Count == 0)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		HeadhunterPlan headhunterPlan = headhunterPlans[0];
		foreach (HeadhunterPlan item in headhunterPlans)
		{
			if (item.GetReplacementsThisHour().Count > 0)
			{
				num += item.GetReplacementsThisHour().Count;
				headhunterPlan = item;
				num2++;
			}
		}
		if (num == 0)
		{
			return;
		}
		if (num <= 3)
		{
			foreach (HeadhunterPlan item2 in headhunterPlans)
			{
				foreach (var item3 in item2.GetReplacementsThisHour())
				{
					item2.ShowReplacementNotification(item3.Item1, item3.Item2);
				}
			}
		}
		else if (num2 == 1)
		{
			headhunterPlan.ShowMultipleReplacementNotificationByHeadhunter(num);
		}
		else
		{
			ShowMultipleReplacementNotification(num);
		}
		foreach (HeadhunterPlan item4 in headhunterPlans)
		{
			item4.GetReplacementsThisHour().Clear();
		}
	}

	private void ShowReplacementNotification(string previousEmployeeName, string name)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{
				"employeeName",
				HeadhunterInstance?.characterData.name
			},
			{ "fromname", previousEmployeeName },
			{ "toname", name }
		};
		Notifications.Show(NotificationType.Info, "notifications_headhunter_employee_replaced", notificationData);
	}

	private static void ShowMultipleReplacementNotification(int totalReplacementsThisHour)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"amount",
			totalReplacementsThisHour.ToString()
		} };
		Notifications.Show(NotificationType.Info, "notifications_headhunters_employee_replaced_multiple", notificationData);
	}

	private void ShowMultipleReplacementNotificationByHeadhunter(int totalReplacementsThisHour)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string>
		{
			{
				"employeeName",
				HeadhunterInstance?.characterData.name
			},
			{
				"amount",
				totalReplacementsThisHour.ToString()
			}
		};
		Notifications.Show(NotificationType.Info, "notifications_headhunter_employee_replaced_multiple", notificationData);
	}

	public void UpdateReplacementStatus()
	{
		for (int num = headhunterReplacementDataList.Count - 1; num >= 0; num--)
		{
			HeadhunterReplacementData headhunterReplacementData = headhunterReplacementDataList[num];
			headhunterReplacementData.hoursUntilReplacement--;
			if (headhunterReplacementData.hoursUntilReplacement <= 0)
			{
				ReplaceEmployee(headhunterReplacementData.employeeInstance);
			}
		}
	}

	private void ReRollNonScheduleDemands(EmployeeInstance employeeInstance)
	{
		for (int i = 0; i < employeeInstance.demands.Count; i++)
		{
			if (JobDemandHelper.EnvironmentDemands.Contains(employeeInstance.demands[i]))
			{
				employeeInstance.demands[i] = JobDemandHelper.EnvironmentDemands.GetRandom();
			}
			else if (JobDemandHelper.EquipmentDemands.Contains(employeeInstance.demands[i]))
			{
				employeeInstance.demands[i] = JobDemandHelper.EquipmentDemands.GetRandom();
			}
		}
	}

	public void CancelPendingReplacements()
	{
		foreach (HeadhunterReplacementData headhunterReplacementData in headhunterReplacementDataList)
		{
			headhunterReplacementData.employeeInstance.OnRemove();
			EmployeeHelper.GetEmployeeInstances().RemoveAll((EmployeeInstance x) => x.id == headhunterReplacementData.employeeInstance.id);
			EmployeeHelper.EmployeeInstancesDictionary.Remove(headhunterReplacementData.employeeInstance.id);
		}
	}

	public void UnAssignEmployee()
	{
		assignedEmployeeId = null;
	}
}
