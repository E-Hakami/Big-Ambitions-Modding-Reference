using System;
using System.Collections.Generic;
using System.Linq;
using AI.Employees;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared;
using Buildings.Office.Headquarters;
using Entities.Employee.JobDemands;
using Extensions;
using Helpers;
using Localizor;
using Streets;
using UI;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities;

[Serializable]
public class EmployeeInstance
{
	[Serializable]
	public class TrainingInstance
	{
		public string skill;

		public int startDay;
	}

	public const int BonusCooldownDays = 30;

	private const int PoachingCooldownDays = 20;

	private const float MaxSkillValue = 100f;

	private const float ReplacedEmployeeSalaryMultiplier = 1.25f;

	private const string EmployeeWagesTaxDeductibleName = "econoview_row_salaries";

	private const string NameWithSkillTextKey = "employee_name_with_skill";

	public static readonly DistributedWork<EmployeeInstance> UpdateSatisfactionWork = new DistributedWork<EmployeeInstance>(delegate(EmployeeInstance x)
	{
		x.UpdateSatisfaction();
	}, 10000);

	private static readonly List<string> DemandsToIgnore = new List<string>();

	public string id;

	public CharacterData characterData = new CharacterData();

	public int dayHired;

	public List<string> demands = new List<string>();

	public float hourlyWage;

	public Address assignedAddress;

	public List<string> assignedWorkStationItems = new List<string>();

	public float satisfaction;

	public string assignedHrManagerPlanId;

	public TrainingInstance trainingSession;

	public int nextSickDay;

	public bool isAbsent;

	public bool isReplaced;

	public int temporalId;

	public int workedHoursToday;

	public int workedHoursThisWeek;

	public List<DayOfWeekOrdered> assignedWeeklyDays = new List<DayOfWeekOrdered>();

	public int assignedWeeklyHours;

	public bool hasSendQuitWarning;

	public bool sendRetirementNotice;

	public bool isTrainingDay;

	public CandidateInfo candidateInfo;

	public int workedDays;

	public float initialCombinedSkillAmount;

	public float poachProposedHourlyWage;

	public EmployeeComplaintData complaintData = new EmployeeComplaintData();

	public bool isBeingReplaced;

	public bool poached;

	[FormerlySerializedAs("poachedByRivalName")]
	public string poachedByRivalId;

	[Obsolete]
	public bool hired;

	[Obsolete]
	public bool declined;

	[Obsolete]
	public List<Skill> skills = new List<Skill>();

	[Obsolete]
	public string assignedHRManager;

	[Obsolete]
	public string presetId;

	[SerializeField]
	private int lastBonusDay;

	[SerializeField]
	private int lastWorkedDay = -1;

	[SerializeField]
	private int lastPoachDay = -1;

	[SerializeField]
	private int poachDeadlineDays;

	public int Years => TimeHelper.GetYearsByDays(characterData.ageInDays);

	public bool IsCandidate => candidateInfo != null;

	public bool IsPoachable
	{
		get
		{
			if (lastPoachDay >= 0)
			{
				return lastPoachDay + 20 <= SaveGameManager.Current.Day;
			}
			return true;
		}
	}

	public bool HasExpiredPoachDeadline
	{
		get
		{
			if (poachDeadlineDays > 0)
			{
				return lastPoachDay + poachDeadlineDays < SaveGameManager.Current.Day;
			}
			return false;
		}
	}

	public bool IsTraining => trainingSession != null;

	public EmployeeInstance()
	{
		id = UuidHelper.GenerateBase64Uuid();
	}

	public virtual void Initialize()
	{
	}

	public virtual void Reset()
	{
		CustomerDemandHelper.ReloadCachedFulfilled(assignedAddress);
		assignedAddress = null;
	}

	public void ResetEmployee()
	{
		characterData.ageInDays = RecruitmentHelper.GetRandomEmployeeAgeInDays();
		hasSendQuitWarning = false;
		workedDays = 0;
		workedHoursToday = 0;
		workedHoursThisWeek = 0;
		poachDeadlineDays = 0;
		dayHired = SaveGameManager.Current.Day;
		lastBonusDay = 0;
		lastPoachDay = -1;
		lastWorkedDay = -1;
		complaintData.ResetHoursUntilNextComplaint();
		isBeingReplaced = false;
		poached = false;
		poachedByRivalId = string.Empty;
	}

	public string GetEmployeeNameWithInfo()
	{
		return "employee_name_with_skill".Localize(new
		{
			name = (isBeingReplaced ? EmployeeHelper.GetAwaitingReplacementText() : characterData.name),
			skillValue = Mathf.FloorToInt(GetSkillValue(GetPrimarySkill()))
		}).ToString();
	}

	public bool HasSkill(string skill)
	{
		List<Skill> list = characterData.skills;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].name == skill)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasAnySkillWithTag(int tag)
	{
		return characterData.skills.Select((Skill x) => SkillHelper.GetData(x.name)).Any((SkillData x) => x?.HasTag(tag) ?? true);
	}

	public bool HasAnySkillWithTag(string tag)
	{
		return characterData.skills.Select((Skill x) => SkillHelper.GetData(x.name)).Any((SkillData x) => x?.HasTag(tag) ?? true);
	}

	public string GetPrimarySkill()
	{
		return characterData.skills.First().name;
	}

	public float GetSkillValue(string skillName)
	{
		return GetSkill(skillName)?.value ?? 0f;
	}

	public void IncreaseSkill(string skillName, float amount)
	{
		Skill skill = GetSkill(skillName);
		if (skill == null)
		{
			skill = new Skill
			{
				name = skillName
			};
			characterData.skills.Add(skill);
		}
		IncreaseSkill(skill, amount);
	}

	public void IncreaseSkill(Skill skill, float amount)
	{
		skill.value += amount;
		if (skill.value > 100f)
		{
			skill.value = 100f;
		}
	}

	public bool CanTrainSkill(Skill skill)
	{
		if (trainingSession == null && !isAbsent)
		{
			return skill.value < 100f;
		}
		return false;
	}

	private Skill GetSkill(string skillName)
	{
		foreach (Skill skill in characterData.skills)
		{
			if (skill.name == skillName)
			{
				return skill;
			}
		}
		Debug.LogWarning("Skill " + skillName + " not found for employee " + (characterData.name ?? "Player"));
		return null;
	}

	public void IncreaseWageFromTraining(Skill skill, float previousSkillValue)
	{
		if (!(skill.value <= previousSkillValue))
		{
			float f = EmployeeHelper.GetSkillWageBoost(skill.value) / EmployeeHelper.GetSkillWageBoost(previousSkillValue);
			float p = ((characterData.skills.Count > 0 && characterData.skills[0] == skill) ? 0.7f : (0.7f / (float)characterData.skills.Count));
			hourlyWage *= Mathf.Pow(f, p);
		}
	}

	public bool IsAssignedToAnyWorkShift()
	{
		if (!IsAssignedToAnyBusiness())
		{
			return false;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
		if (HasSkill("ba:skill_deliverydriver"))
		{
			if (buildingRegistration is Warehouse warehouse)
			{
				return warehouse.IsDriverAssignedToAnySlot(this);
			}
			return false;
		}
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				if (workShift.employeeId == id)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsAssignedToSpecificWorkShift(WorkShiftType type)
	{
		return BuildingHelper.GetBuildingRegistration(assignedAddress)?.scheduleDays.Exists((ScheduleDay x) => x.workShifts.Exists((WorkShift y) => y.employeeId == id && y.type == type)) ?? false;
	}

	public bool IsAssignedToAnyBusiness()
	{
		if (assignedAddress != null)
		{
			return !assignedAddress.IsUndefined();
		}
		return false;
	}

	public void UpdateAssignedWorkStationItems()
	{
		if (assignedWorkStationItems == null)
		{
			assignedWorkStationItems = new List<string>();
		}
		assignedWorkStationItems.Clear();
		if (assignedAddress == null || assignedAddress.IsUndefined())
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
		for (int i = 0; i < buildingRegistration.scheduleDays.Count; i++)
		{
			for (int j = 0; j < buildingRegistration.scheduleDays[i].workShifts.Count; j++)
			{
				if (buildingRegistration.scheduleDays[i].workShifts[j].employeeId != id)
				{
					continue;
				}
				if (!buildingRegistration.itemInstances.TryGetValue(buildingRegistration.scheduleDays[i].workShifts[j].itemInstanceId, out var value))
				{
					break;
				}
				if (!assignedWorkStationItems.Contains(value.itemName))
				{
					assignedWorkStationItems.Add(value.itemName);
				}
				if (!string.IsNullOrEmpty(value.parentId))
				{
					if (!buildingRegistration.itemInstances.TryGetValue(value.parentId, out value))
					{
						break;
					}
					if (!assignedWorkStationItems.Contains(value.itemName))
					{
						assignedWorkStationItems.Add(value.itemName);
					}
				}
				foreach (AttachableChild stackedItem in value.stackedItems)
				{
					if (buildingRegistration.itemInstances.TryGetValue(stackedItem.childId, out var value2) && !assignedWorkStationItems.Contains(value2.itemName))
					{
						assignedWorkStationItems.Add(value2.itemName);
					}
				}
			}
		}
	}

	public string GetCurrentTask()
	{
		if (trainingSession != null)
		{
			return "myemployees_task_training";
		}
		if (isAbsent && !isReplaced)
		{
			return "myemployees_task_absent";
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
		if (buildingRegistration == null)
		{
			return null;
		}
		if (HasSkill("ba:skill_deliverydriver"))
		{
			if (buildingRegistration is Warehouse warehouse)
			{
				VehicleSlot assignedSlot = warehouse.vehicleSlots.Find((VehicleSlot x) => x.employeeDriverId == id);
				if (assignedSlot != null && !string.IsNullOrEmpty(assignedSlot.vehicleInstanceId))
				{
					VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.SingleOrDefault((VehicleInstance x) => x.id == assignedSlot.vehicleInstanceId);
					if (vehicleInstance != null)
					{
						return vehicleInstance.vehicleTypeName;
					}
				}
			}
		}
		else
		{
			WorkShift workShift = null;
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				workShift = scheduleDay.workShifts.FirstOrDefault((WorkShift workShift2) => workShift2.employeeId == id);
				if (workShift != null)
				{
					break;
				}
			}
			if (workShift == null)
			{
				return null;
			}
			if (buildingRegistration.itemInstances.TryGetValue(workShift.itemInstanceId, out var value))
			{
				return value.itemName;
			}
		}
		return null;
	}

	public bool IsWorking(bool specificShiftType = false, WorkShiftType workShiftType = WorkShiftType.Default)
	{
		return GetWorkShiftAssignedInThisMoment(specificShiftType, workShiftType) != null;
	}

	public WorkShift GetWorkShiftAssignedInThisMoment(bool specificShiftType = false, WorkShiftType workShiftType = WorkShiftType.Default)
	{
		if (!IsEmployeeAvailable())
		{
			return null;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
		if (buildingRegistration?.scheduleDays == null)
		{
			return null;
		}
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek(SaveGameManager.Current.Day);
		for (int i = 0; i < buildingRegistration.scheduleDays.Count; i++)
		{
			if (buildingRegistration.scheduleDays[i].day != dayOfWeek)
			{
				continue;
			}
			if (!buildingRegistration.scheduleDays[i].isOpen)
			{
				return null;
			}
			List<WorkShift> workShifts = buildingRegistration.scheduleDays[i].workShifts;
			for (int j = 0; j < workShifts.Count; j++)
			{
				if (!(workShifts[j].employeeId != id) && workShifts[j].startingHour <= SaveGameManager.Current.Hour && workShifts[j].endingHour > SaveGameManager.Current.Hour)
				{
					if (specificShiftType && workShifts[j].type != workShiftType)
					{
						return null;
					}
					return workShifts[j];
				}
			}
		}
		return null;
	}

	public bool IsEmployeeAvailable()
	{
		if (!isBeingReplaced)
		{
			if (isAbsent)
			{
				return isReplaced;
			}
			return true;
		}
		return false;
	}

	public void UpdateWeeklyHoursAndDays(List<ScheduleDay> scheduleDays = null)
	{
		assignedWeeklyDays.Clear();
		assignedWeeklyHours = 0;
		if (assignedAddress == null || assignedAddress.IsUndefined())
		{
			return;
		}
		if (HasSkill("ba:skill_deliverydriver"))
		{
			if (BuildingHelper.GetBuildingRegistration(assignedAddress) is Warehouse warehouse && warehouse.IsDriverAssignedToAnySlot(this))
			{
				assignedWeeklyDays.AddRange(Enum.GetValues(typeof(DayOfWeekOrdered)).Cast<DayOfWeekOrdered>());
				assignedWeeklyHours = 40;
			}
			else
			{
				assignedWeeklyDays.Clear();
				assignedWeeklyHours = 0;
			}
			return;
		}
		if (scheduleDays == null)
		{
			scheduleDays = BuildingHelper.GetBuildingRegistration(assignedAddress).scheduleDays;
			if (scheduleDays == null)
			{
				return;
			}
		}
		foreach (ScheduleDay scheduleDay in scheduleDays)
		{
			int workingHoursOnDay = GetWorkingHoursOnDay(scheduleDay);
			if (workingHoursOnDay > 0)
			{
				assignedWeeklyDays.Add(scheduleDay.day);
				assignedWeeklyHours += workingHoursOnDay;
			}
		}
	}

	public int GetWorkingHoursOnDay(ScheduleDay scheduleDay)
	{
		if (!scheduleDay.isOpen)
		{
			return 0;
		}
		int num = 0;
		foreach (WorkShift workShift in scheduleDay.workShifts)
		{
			if (!(workShift.employeeId != id))
			{
				num += workShift.endingHour - workShift.startingHour;
			}
		}
		return num;
	}

	public int WorkingHoursOnDay(ScheduleDay scheduleDay, WorkShiftType type)
	{
		return scheduleDay.workShifts.Where((WorkShift x) => x.employeeId == id && scheduleDay.isOpen && x.type == type).Sum((WorkShift x) => x.endingHour - x.startingHour);
	}

	public bool HasAnyShiftBetweenHours(int firstHour, int secondHour, List<ScheduleDay> scheduleDays = null)
	{
		if (scheduleDays == null)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
			if (buildingRegistration == null)
			{
				return false;
			}
			scheduleDays = buildingRegistration.scheduleDays;
		}
		foreach (ScheduleDay scheduleDay in scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				continue;
			}
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				if (!(workShift.employeeId != id) && Overlaps(firstHour, secondHour, workShift.startingHour, workShift.endingHour))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool Overlaps(int startA, int endA, int startB, int endB)
	{
		if (startA < endB)
		{
			return startB < endA;
		}
		return false;
	}

	public void FinishTraining()
	{
		string skill = trainingSession.skill;
		trainingSession = null;
		Skill skill2 = GetSkill(skill);
		float value = skill2.value;
		IncreaseSkill(skill2, 10f);
		IncreaseWageFromTraining(skill2, value);
		if (!IsAssignedToAnyBusiness())
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.EmployeeUnassigned, assignedAddress, null, null, id);
		}
		GameEvent.Invoke("ba:gameevent_trainingfinished");
	}

	public void GenerateDemands(string hoursPerWeekDemand = null)
	{
		int targetDemandsAmount = GetTargetDemandsAmount();
		if (targetDemandsAmount != 0)
		{
			AddHoursPerWeekDemands(hoursPerWeekDemand);
			AddJobSpecificDemands();
			for (int num = targetDemandsAmount - demands.Count; num > 0; num--)
			{
				AddRandomDemand();
			}
		}
	}

	public void AddHoursPerWeekDemands(string specificDemand = null)
	{
		foreach (string demand in demands)
		{
			if (JobDemandHelper.TypeDemands.Contains(demand))
			{
				return;
			}
		}
		if (!string.IsNullOrEmpty(specificDemand))
		{
			demands.Add(specificDemand);
			return;
		}
		string randomHoursPerWeekDemandForSkill = JobDemandHelper.GetRandomHoursPerWeekDemandForSkill(GetPrimarySkill());
		if (!string.IsNullOrEmpty(randomHoursPerWeekDemandForSkill))
		{
			demands.Add(randomHoursPerWeekDemandForSkill);
		}
	}

	public void AddJobSpecificDemands()
	{
		foreach (string demand in demands)
		{
			if (JobDemandHelper.GetByName(demand).isSkillSpecific)
			{
				return;
			}
		}
		string randomJobSpecificDemandForSkill = JobDemandHelper.GetRandomJobSpecificDemandForSkill(GetPrimarySkill());
		if (!string.IsNullOrEmpty(randomJobSpecificDemandForSkill) && !demands.Contains(randomJobSpecificDemandForSkill))
		{
			demands.Add(randomJobSpecificDemandForSkill);
		}
	}

	public int UpdateDemands()
	{
		int num = GetTargetDemandsAmount() - demands.Count;
		int num2 = 0;
		while (num > 0)
		{
			AddRandomDemand(JobDemandHelper.NonAdditiveDemands.ToList(), !IsCandidate);
			num--;
			num2++;
		}
		return num2;
	}

	private void AddRandomDemand(List<string> demandsToIgnore = null, bool sendMessage = false)
	{
		DemandsToIgnore.Clear();
		if (demandsToIgnore != null)
		{
			DemandsToIgnore.AddRange(demandsToIgnore);
		}
		if (!TutorialHelper.HasCompletedObjective("tutorial_quest_logistics_network_objective_5") || this is HRManager)
		{
			DemandsToIgnore.AddRange(JobDemandHelper.HealthInsuranceDemands);
		}
		string randomDemandForSkill = JobDemandHelper.GetRandomDemandForSkill(GetPrimarySkill(), demands, DemandsToIgnore);
		if (!string.IsNullOrEmpty(randomDemandForSkill))
		{
			demands.Add(randomDemandForSkill);
			if (sendMessage && !JobDemandHelper.GetByName(randomDemandForSkill).Fulfilled(this, null))
			{
				Dictionary<string, string> data = new Dictionary<string, string> { { "jobDemandName", randomDemandForSkill } };
				SendMessage("ba:messagetype_employee_contact_message_new_demand", data);
			}
		}
	}

	private int GetTargetDemandsAmount()
	{
		float num = 0f;
		foreach (Skill skill in characterData.skills)
		{
			num += skill.value;
		}
		return JobDemandHelper.GetIdealNumberOfDemands(GetPrimarySkill(), num);
	}

	public void UpdateSatisfaction()
	{
		float num = 0f;
		int num2 = 0;
		for (int i = 0; i < demands.Count; i++)
		{
			JobDemand byName = JobDemandHelper.GetByName(demands[i]);
			if (!(byName == null))
			{
				if (byName.Fulfilled(this, null))
				{
					num += byName.Impact;
					num2++;
				}
				else
				{
					num -= byName.Impact;
				}
			}
		}
		float num3 = (float)num2 / (float)demands.Count;
		int num4 = ((num3 <= 0.34f) ? 60 : ((!(num3 <= 0.67f)) ? 100 : 80));
		int num5 = num4;
		if (satisfaction > (float)num5)
		{
			num = -0.6f;
		}
		else if (Mathf.RoundToInt(satisfaction * 100f) == num5 * 100)
		{
			num = 0f;
		}
		satisfaction += Mathf.Clamp(num, -0.6f, 0.6f);
		satisfaction = Mathf.Clamp(satisfaction, 0f, 100f);
	}

	public virtual void WorkHourly()
	{
	}

	public virtual void WorkDaily()
	{
	}

	public void Resign()
	{
		Address address = assignedAddress;
		OnRemove();
		InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.employeeId == id));
		PayWage();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (HasAnySkillWithTag(TagRef.Skilltag.affectssecurity))
		{
			buildingRegistration?.UpdateSecurityLevel();
		}
		EmployeeHelper.ResignedEmployeesAndLastRegistrationAssigned.Add((characterData.name, buildingRegistration));
	}

	public void ResignBeforeReplacement()
	{
		Address address = assignedAddress;
		InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.employeeId == id));
		PayWage();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (HasAnySkillWithTag(TagRef.Skilltag.affectssecurity))
		{
			buildingRegistration?.UpdateSecurityLevel();
		}
	}

	public virtual void UnAssignWork()
	{
		if (HasSkill("ba:skill_purchasingagent"))
		{
			PurchasingAgentHelper.GetAssignedPlanForPurchasingAgent(id)?.UnAssignEmployee();
		}
	}

	public void PayWage(BuildingRegistration registration = null)
	{
		float num = workedHoursToday;
		bool flag = IsAssignedToAnyBusiness();
		if (flag && HasSkill("ba:skill_deliverydriver"))
		{
			num = 5.7f;
		}
		if (num != 0f)
		{
			if (registration == null)
			{
				registration = BuildingHelper.GetBuildingRegistration(assignedAddress);
			}
			float num2 = num * hourlyWage;
			if (isReplaced)
			{
				num2 *= 1.25f;
			}
			string type = ((!flag) ? "ba:transaction_unassignedwage" : (isReplaced ? "ba:transaction_replacementwage" : "ba:transaction_wage"));
			string taxDeductibleName = (isReplaced ? "ba:transaction_replacementwage_label" : "econoview_row_salaries");
			Dictionary<string, string> data = new Dictionary<string, string>
			{
				{ "employee", characterData.name },
				{
					"businessName",
					registration?.BusinessName ?? "Unassigned"
				}
			};
			TransactionInfo transactionInfo = new TransactionInfo(type, "ba:transactioncategory_salaryexpenses", data);
			transactionInfo.SetTaxDeductibleName(taxDeductibleName);
			GameManager.ChangeMoneySafe(0f - num2, transactionInfo, SaveGameManager.Current.Day - 1, assignedAddress, force: true);
		}
	}

	public void RemoveEmployee()
	{
		Address address = assignedAddress;
		OnRemove();
		EmployeeHelper.GetEmployeeInstances().RemoveAll((EmployeeInstance x) => x.id == id);
		EmployeeHelper.EmployeeInstancesDictionary.Remove(id);
		InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.employeeId == id));
		if (HasAnySkillWithTag(TagRef.Skilltag.affectssecurity))
		{
			BuildingHelper.GetBuildingRegistration(address)?.UpdateSecurityLevel();
		}
	}

	public virtual void OnRemove()
	{
		EmployeeHelper.UnassignEmployeeFromAllWorkshifts(this);
		BizManSchedule.AbortAutoFillForBusiness(BuildingHelper.GetBuildingRegistration(assignedAddress), notify: true);
		HrManagerHelper.GetPlanFromId(assignedHrManagerPlanId)?.assignedEmployees.Remove(id);
	}

	public Contact GetContact()
	{
		return Contact.GetContact(characterData.name, ContactCategoryName.Employees, "employee_contact_description");
	}

	public void SendMessage(string messageKey, Dictionary<string, string> data = null, bool isSpecial = false, AdditionalMessageData additionalData = null)
	{
		string value = "common_unassigned";
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(assignedAddress);
		if (buildingRegistration != null)
		{
			value = buildingRegistration.BusinessName;
		}
		if (data == null)
		{
			data = new Dictionary<string, string>();
		}
		if (!data.ContainsKey("employeeName"))
		{
			data.Add("employeeName", characterData.name);
		}
		data.TryAdd("businessName", value);
		if (!data.ContainsKey("skillKey"))
		{
			data.Add("skillKey", characterData.skills.OrderByDescending((Skill x) => x.value).First().name);
		}
		GetContact().SendMessage(new TextMessage(messageKey, data, read: false, isNewInteraction: false, isSpecial, additionalData));
	}

	public List<AppearanceElementData> GetUniformElements(string presetId)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
		{
			return EmployeeHelper.GetUniformElements(characterData.skills.Select((Skill x) => x.name).ToList(), characterData.gender);
		}
		EmployeePreset employeePreset = SaveGameManager.Current.employeePresets.FirstOrDefault((EmployeePreset x) => x.id == presetId);
		if (characterData.gender != Gender.Female)
		{
			return employeePreset?.maleElements;
		}
		return employeePreset?.femaleElements;
	}

	public void AddTodoTask(TodoTaskType taskType, bool invokeQuestAffectingChange = true)
	{
		if (!(InstanceBehavior<UIs>.Instance == null))
		{
			IEnumerable<TodoTask> tasks = SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.employeeId == id);
			InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(tasks);
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(taskType, assignedAddress, null, null, id);
			if (invokeQuestAffectingChange)
			{
				GameEvent.Invoke(string.Empty);
			}
		}
	}

	public string GetDemandOfTypeLocalized<T>() where T : JobDemand
	{
		foreach (string demand in demands)
		{
			if (JobDemandHelper.GetByName(demand) is T val)
			{
				return val.GetLocalization();
			}
		}
		return "-";
	}

	public string GetReplacementReasonLocalized()
	{
		if (IsRetiringInLessThanDays(7))
		{
			int daysBeforeRetiring = characterData.ageInDays - 67 * SaveGameManager.Current.gameVariables.daysPerYear;
			return "bizman_headhunters_retiring_in_days".Localize(new { daysBeforeRetiring }).ToString();
		}
		return "bizman_headhunters_low_satisfaction".Localize(new
		{
			percentage = Mathf.FloorToInt(satisfaction)
		}).ToString();
	}

	public ReplacementReason GetReplacementReason()
	{
		if (IsRetiringInLessThanDays(7))
		{
			return ReplacementReason.Retirement;
		}
		if (!poached)
		{
			return ReplacementReason.Satisfaction;
		}
		return ReplacementReason.Poached;
	}

	public bool IsRetiringInLessThanDays(int days)
	{
		return 67 * SaveGameManager.Current.gameVariables.daysPerYear - characterData.ageInDays < days;
	}

	public bool ReplaceAutomaticallyOnRetire()
	{
		HeadhunterPlan assignedHeadhunterPlan = GetAssignedHeadhunterPlan();
		if (assignedHeadhunterPlan == null || !assignedHeadhunterPlan.automaticallyReplaceOnRetire)
		{
			return false;
		}
		if (string.IsNullOrEmpty(assignedHeadhunterPlan.assignedEmployeeId))
		{
			return false;
		}
		return assignedHeadhunterPlan.SetEmployeeToReplace(this);
	}

	public bool ReplaceAutomaticallyOnResign()
	{
		HeadhunterPlan assignedHeadhunterPlan = GetAssignedHeadhunterPlan();
		if (assignedHeadhunterPlan == null || !assignedHeadhunterPlan.automaticallyReplaceOnResign)
		{
			return false;
		}
		if (string.IsNullOrEmpty(assignedHeadhunterPlan.assignedEmployeeId))
		{
			return false;
		}
		return assignedHeadhunterPlan.SetEmployeeToReplace(this);
	}

	public HeadhunterPlan GetAssignedHeadhunterPlan()
	{
		return HeadhunterHelper.GetAssignedPlanForHrManagerPlan(assignedHrManagerPlanId);
	}

	public float GetCustomerSatisfaction(ItemInstance stationInstance)
	{
		Item byName = ItemsGetter.GetByName(stationInstance.itemName);
		foreach (Skill skill in characterData.skills)
		{
			if (byName.suitableSkills.Contains(skill.name))
			{
				return skill.value * satisfaction * 0.01f;
			}
		}
		return 0f;
	}

	public float GetBonusAmount()
	{
		return Mathf.Round((100f - satisfaction) * hourlyWage * 8f * 30f / 100f);
	}

	public bool CanGiveBonus()
	{
		if (GetBonusAmount() > hourlyWage * 8f)
		{
			return GetDaysUntilBonusAvailability() <= 0;
		}
		return false;
	}

	public int GetDaysUntilBonusAvailability()
	{
		if (lastBonusDay != 0)
		{
			return Math.Clamp(30 - (SaveGameManager.Current.Day - lastBonusDay), 0, 30);
		}
		return 0;
	}

	public int GetLastBonusDay()
	{
		return lastBonusDay;
	}

	public bool GiveBonus()
	{
		float bonusAmount = GetBonusAmount();
		if (bonusAmount <= 0f)
		{
			return false;
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "employee", characterData.name } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_employeebonus", data);
		transactionInfo.SetTaxDeductibleName("ba:transaction_employeebonus_label");
		if (!GameManager.ChangeMoneySafe(0f - bonusAmount, transactionInfo, null, null, force: false, showNotification: true))
		{
			return false;
		}
		satisfaction = 100f;
		lastBonusDay = SaveGameManager.Current.Day;
		complaintData.UpdateHoursUntilNextComplaintDueToBonus();
		return true;
	}

	public void AddWorkedDay()
	{
		if (lastWorkedDay != SaveGameManager.Current.Day)
		{
			lastWorkedDay = SaveGameManager.Current.Day;
			workedDays++;
		}
	}

	public void PoachByRival(string rivalId, int deadlineDays, int percentageRaise)
	{
		poachedByRivalId = rivalId;
		poachDeadlineDays = deadlineDays;
		poachProposedHourlyWage = hourlyWage * (1f + (float)percentageRaise / 100f);
		lastPoachDay = SaveGameManager.Current.Day;
		AdditionalMessageData additionalData = new AdditionalMessageData
		{
			contextButtonData = new List<TextMessage.ContextButtonData>
			{
				new TextMessage.ContextButtonData("dialog_decline_button", ContextButton.BackgroundColor.gray, DeclinePoachPayRaise),
				new TextMessage.ContextButtonData("dialog_accept_button", ContextButton.BackgroundColor.blue, AcceptPoachPayRaise)
			}
		};
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{
				"amount",
				poachProposedHourlyWage.ToCurrencyFormat()
			},
			{
				"rivalName",
				rivalId.GetRivalName()
			},
			{
				"days",
				deadlineDays.ToString()
			}
		};
		SendMessage("ba:messagetype_rivals_poach_employee_offer", data, isSpecial: false, additionalData);
	}

	private void AcceptPoachPayRaise()
	{
		poachedByRivalId = string.Empty;
		poachDeadlineDays = 0;
		hourlyWage = poachProposedHourlyWage;
		SendMessage("ba:messagetype_rivals_poach_employee_accept");
	}

	private void DeclinePoachPayRaise()
	{
		poachDeadlineDays = 0;
		poached = true;
		MoveEmployeeToRival("ba:messagetype_rivals_poach_employee_decline");
	}

	public void PoachDeadlineExpired()
	{
		poachDeadlineDays = 0;
		poached = true;
		MoveEmployeeToRival("ba:messagetype_rivals_poach_employee_expired");
	}

	private void MoveEmployeeToRival(string messageType)
	{
		string preferredBusinessType = BuildingHelper.GetBuildingRegistration(assignedAddress)?.businessTypeName ?? (from x in BusinessTypeHelper.GetAllPlayerAvailableBusinesses()
			where x.employeePrimarySkills.Contains(GetPrimarySkill())
			select x).GetRandom().businessTypeName;
		BuildingRegistration random = RivalsHelper.GetRivalData(poachedByRivalId).ownedBusinesses.Where((BuildingRegistration x) => x.businessTypeName == preferredBusinessType).GetRandom();
		if (random == null)
		{
			Resign();
			RemoveEmployee();
			return;
		}
		AiBusinessEmployeeData item = (from x in random.aiEmployees
			where x.primarySkillName == GetPrimarySkill()
			orderby x.primarySkillValue
			select x).FirstOrDefault();
		random.aiEmployees.Remove(item);
		EmployeeInstance employeeInstance = this.Copy();
		employeeInstance.ResetEmployee();
		employeeInstance.characterData = characterData;
		employeeInstance.poachedByRivalId = poachedByRivalId;
		employeeInstance.id = UuidHelper.GenerateBase64Uuid();
		employeeInstance.assignedAddress = random.Address;
		BuildingRegistration buildingRegistration = random;
		if (buildingRegistration.poachedEmployees == null)
		{
			buildingRegistration.poachedEmployees = new List<EmployeeInstance>();
		}
		random.poachedEmployees.Add(employeeInstance);
		if (!ReplaceAutomaticallyOnResign())
		{
			if (!string.IsNullOrEmpty(messageType))
			{
				SendMessage(messageType);
			}
			Resign();
			RemoveEmployee();
		}
	}

	public void StopPoachingRivalSurrender(string rivalName)
	{
		poachDeadlineDays = 0;
		satisfaction *= 0.6f;
		AdditionalMessageData additionalData = new AdditionalMessageData
		{
			contextButtonData = new List<TextMessage.ContextButtonData>
			{
				new TextMessage.ContextButtonData("dialog_decline_button", ContextButton.BackgroundColor.gray, delegate
				{
					SendMessage("ba:messagetype_rivals_poach_employee_raise_declined");
				}),
				new TextMessage.ContextButtonData("dialog_accept_button", ContextButton.BackgroundColor.blue, delegate
				{
					hourlyWage = poachProposedHourlyWage;
					satisfaction = 100f;
					SendMessage("ba:messagetype_rivals_poach_employee_raise_accepted");
				})
			}
		};
		Dictionary<string, string> data = new Dictionary<string, string> { { "rivalName", rivalName } };
		SendMessage("ba:messagetype_rivals_poach_employee_rival_surrender", data, isSpecial: false, additionalData);
	}

	public void RunComplaintIfNeeded(bool forceComplaint = false)
	{
		if ((!forceComplaint && complaintData.hoursUntilNextComplaint > 0) || complaintData.isComplaining)
		{
			return;
		}
		foreach (Complaint complaint in ComplaintHelper.Complaints)
		{
			if (complaint.ConditionToComplainMet(this))
			{
				StartComplaint(complaint);
				return;
			}
		}
		complaintData.ResetHoursUntilNextComplaint();
	}

	private void StartComplaint(Complaint complaint, bool ignoreComplaintLimit = false)
	{
		if (!isAbsent && (ignoreComplaintLimit || ComplaintHelper.CanComplain()))
		{
			var (messageKey, data, flag) = complaint.GetComplaintMessage(this);
			SendMessage(messageKey, data);
			SaveGameManager.Current.daysWithComplaints.Enqueue(SaveGameManager.Current.Day);
			if (complaint.hoursToHandleComplaint == 0)
			{
				ComplaintResign(flag);
			}
			else
			{
				complaintData.SetDataFromComplaint(complaint.Copy(), flag);
			}
		}
	}

	public void UpdateCurrentComplaintState()
	{
		if (complaintData.isComplaining)
		{
			complaintData.complaintDeadlineHours--;
			if (complaintData.currentComplaint.ComplaintHandled(this))
			{
				complaintData.isComplaining = false;
			}
			else if (complaintData.complaintDeadlineHours <= 0)
			{
				ComplaintResign(complaintData.hasRival);
			}
		}
	}

	private void ComplaintResign(bool hasRival)
	{
		poached = hasRival;
		satisfaction = 0f;
		if (!ReplaceAutomaticallyOnResign())
		{
			Resign();
		}
	}

	public void RunHourly()
	{
		GameInstance current = SaveGameManager.Current;
		if (trainingSession == null)
		{
			if (IsWorking())
			{
				workedHoursToday++;
				workedHoursThisWeek++;
				AddWorkedDay();
				WorkHourly();
			}
			if (!isAbsent && !isTrainingDay && !UpdateSatisfactionWork.Enqueue(this))
			{
				UpdateSatisfaction();
			}
		}
		else if (current.Day > trainingSession.startDay && current.Hour == 17)
		{
			EmployeeHelper.FinishedTrainingEmployees.Add(this, trainingSession.skill);
			FinishTraining();
			isTrainingDay = true;
		}
		RunComplaintsHourly();
	}

	private void RunComplaintsHourly()
	{
		complaintData.UpdateHoursUntilNextComplaintHourly();
		LowSatisfactionComplaint lowSatisfaction = ComplaintHelper.LowSatisfaction;
		if (trainingSession == null && !isAbsent && !isTrainingDay && !isBeingReplaced && satisfaction <= 0f && lowSatisfaction.ConditionToComplainMet(this))
		{
			StartComplaint(lowSatisfaction, ignoreComplaintLimit: true);
			return;
		}
		UpdateCurrentComplaintState();
		RunComplaintIfNeeded();
	}
}
