using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public static class ScheduleHelper
{
	public const string OverworkedDaysTextKey = "bizman_schedule_employee_overworked_days";

	private const int ShiftLengthCap = 12;

	private static WorkShift SimulatedWorkShift;

	private static int CurrentSimulatedDayIndex;

	public static BizManBusiness Business { get; set; }

	public static ItemType AttachedItemTypeFilter { get; set; }

	public static ScheduleDay CurrentScheduleDay { get; set; }

	public static List<ScheduleDay> ScheduleDays => Business.buildingRegistration.scheduleDays;

	public static bool IsHeadquarters => Business.buildingRegistration.businessTypeName == "ba:businesstype_headquarters";

	public static List<EmployeeInstance> Employees { get; private set; }

	public static Dictionary<string, EmployeeInstance> EmployeesById { get; set; }

	public static bool[] OpeningHoursState { get; private set; }

	public static bool[] AddEmployeeIconVisibilityLookup { get; private set; }

	public static UnityEvent<int, bool> OnOpeningHourChanged { get; } = new UnityEvent<int, bool>();

	public static UnityEvent<int, string> OnRequestEmployeeSelection { get; } = new UnityEvent<int, string>();

	public static UnityEvent<bool> PauseScheduleScroll { get; } = new UnityEvent<bool>();

	public static UnityEvent<bool> RequestScheduleScrollerReload { get; } = new UnityEvent<bool>();

	private static Dictionary<string, List<WorkShift>> WorkShiftsByWorkstationId { get; } = new Dictionary<string, List<WorkShift>>();

	private static Dictionary<string, List<WorkShift>> WorkShiftsByEmployeeId { get; } = new Dictionary<string, List<WorkShift>>();

	public static UnityEvent OnWorkShiftChanged { get; } = new UnityEvent();

	public static Dictionary<string, ItemInstance> WorkstationsById { get; private set; }

	private static List<ScheduleDay> SimulatedScheduleDays { get; set; }

	public static ScheduleDay GetScheduleDay(int dayOfWeekOrderedIndex)
	{
		return ScheduleDays[dayOfWeekOrderedIndex - 1];
	}

	public static List<DayOfWeekOrdered> GetOverworkedDays(EmployeeInstance employee)
	{
		List<DayOfWeekOrdered> list = new List<DayOfWeekOrdered>();
		foreach (ScheduleDay scheduleDay in ScheduleDays)
		{
			if (employee.GetWorkingHoursOnDay(scheduleDay) > 14)
			{
				list.Add(scheduleDay.day);
			}
		}
		return list;
	}

	public static object GetOverworkedTooltipArguments(List<DayOfWeekOrdered> overworkedDays)
	{
		return new
		{
			overworkedDayNames = overworkedDays.MapToList((DayOfWeekOrdered day) => day.GetLocalizeKey()).Listify(localize: true)
		};
	}

	public static void CalculateOpeningHoursState()
	{
		OpeningHoursState = new bool[24];
		if (!CurrentScheduleDay.isOpen)
		{
			return;
		}
		foreach (OpeningHourSlot openingHourSlot in CurrentScheduleDay.openingHourSlots)
		{
			for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
			{
				OpeningHoursState[i] = true;
			}
		}
	}

	public static void GenerateAddEmployeeVisibilityLookup()
	{
		AddEmployeeIconVisibilityLookup = new bool[24];
		for (int i = 0; i < 24; i++)
		{
			bool flag = OpeningHoursState[i];
			bool flag2 = i > 0 && OpeningHoursState[i - 1];
			AddEmployeeIconVisibilityLookup[i] = flag && !flag2;
		}
	}

	public static void FetchEmployees(Address address)
	{
		Employees = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = address
		});
		if (EmployeesById == null)
		{
			EmployeesById = new Dictionary<string, EmployeeInstance>();
		}
		EmployeesById.Clear();
		foreach (EmployeeInstance employee in Employees)
		{
			EmployeesById[employee.id] = employee;
		}
	}

	public static EmployeeInstance GetScheduleEmployeeById(string id)
	{
		return EmployeesById.GetValueOrDefault(id);
	}

	public static Color GetEmployeeColor(string employeeId)
	{
		return SkillHelper.GetData(EmployeesById[employeeId].GetPrimarySkill()).associatedColorGradient.EvaluateRandom(employeeId.GetHashCode());
	}

	public static List<WorkShift> GetWorkShiftsByEmployeeId(string employeeId)
	{
		if (!WorkShiftsByEmployeeId.TryGetValue(employeeId, out var value))
		{
			return new List<WorkShift>();
		}
		return value;
	}

	public static bool IsEmployeeAvailable(string workstationId, string employeeId, int hour)
	{
		List<WorkShift> workShiftsByEmployeeId = GetWorkShiftsByEmployeeId(employeeId);
		if (HasSkillForWorkstation(workstationId, employeeId))
		{
			return workShiftsByEmployeeId.All((WorkShift shift) => !shift.IsHourInShift(hour));
		}
		return false;
	}

	public static void RegenerateSimulatedScheduleDays()
	{
		SimulatedWorkShift = null;
		CurrentSimulatedDayIndex = ScheduleDays.FindIndex((ScheduleDay x) => x == CurrentScheduleDay);
		SimulatedScheduleDays = ScheduleDays.Select((ScheduleDay day) => new ScheduleDay
		{
			openingHourSlots = day.openingHourSlots.ToList(),
			workShifts = day.workShifts.Select((WorkShift shift) => new WorkShift
			{
				startingHour = shift.startingHour,
				endingHour = shift.endingHour,
				itemInstanceId = shift.itemInstanceId,
				employeeId = shift.employeeId,
				type = shift.type
			}).ToList()
		}).ToList();
	}

	public static List<ScheduleDay> GetSimulatedScheduleDays(string workstationId, int hour, EmployeeInstance employee)
	{
		ScheduleDay scheduleDay = SimulatedScheduleDays[CurrentSimulatedDayIndex];
		int endingHour = CalculateEndingHour(hour, workstationId, employee.id);
		if (SimulatedWorkShift != null)
		{
			scheduleDay.workShifts.Remove(SimulatedWorkShift);
		}
		SimulatedWorkShift = new WorkShift
		{
			startingHour = hour,
			itemInstanceId = workstationId,
			employeeId = employee.id,
			type = ((!IsCleaningStation(workstationId)) ? WorkShiftType.Default : WorkShiftType.Cleaning),
			endingHour = endingHour
		};
		scheduleDay.workShifts.Add(SimulatedWorkShift);
		return SimulatedScheduleDays;
	}

	public static List<EmployeeInstance> GetEmployeesForDay(int dayIndex)
	{
		return GetScheduleDay(dayIndex).workShifts.Select((WorkShift w) => EmployeesById[w.employeeId]).Distinct().ToList();
	}

	public static void UpdateEmployeesAfterWorkShiftChange(List<EmployeeInstance> employees)
	{
		foreach (EmployeeInstance employee in employees)
		{
			UpdateEmployeeAfterWorkShiftChange(employee, singleEmployee: false);
		}
		if (IsHeadquarters)
		{
			UpdateHQPlans();
		}
		if (BusinessTypeHelper.GetData(Business).HasTag(TagRef.Businesstag.allowtheft))
		{
			Business.buildingRegistration.UpdateSecurityLevel();
			Business.UpdateSecurityInfo();
		}
	}

	private static void UpdateEmployeeAfterWorkShiftChange(string employeeId)
	{
		UpdateEmployeeAfterWorkShiftChange(EmployeesById[employeeId]);
	}

	private static void UpdateEmployeeAfterWorkShiftChange(EmployeeInstance employee, bool singleEmployee = true)
	{
		employee.UpdateWeeklyHoursAndDays(ScheduleDays);
		employee.UpdateAssignedWorkStationItems();
		if (singleEmployee && employee.HasAnySkillWithTag(TagRef.Skilltag.affectssecurity))
		{
			Business.buildingRegistration.UpdateSecurityLevel();
			Business.UpdateSecurityInfo();
		}
		if (!employee.IsAssignedToAnyWorkShift())
		{
			employee.UnAssignWork();
			employee.AddTodoTask(TodoTaskType.EmployeeIdle);
		}
		if (singleEmployee && IsHeadquarters)
		{
			UpdateHQPlans();
		}
		InstanceBehavior<UIs>.Instance.tasksUI.forceCheckForCompletedTodoTasks = true;
		SaveGameManager.MarkChange();
	}

	public static List<WorkShift> GetWorkShiftsByWorkstationId(string workstationId)
	{
		if (!WorkShiftsByWorkstationId.TryGetValue(workstationId, out var value))
		{
			return new List<WorkShift>();
		}
		return value;
	}

	public static void CacheWorkShifts()
	{
		WorkShiftsByWorkstationId.Clear();
		WorkShiftsByEmployeeId.Clear();
		foreach (WorkShift workShift in CurrentScheduleDay.workShifts)
		{
			AddShiftToCache(workShift);
		}
	}

	public static void AddShiftToCache(WorkShift shift)
	{
		if (!WorkShiftsByWorkstationId.ContainsKey(shift.itemInstanceId))
		{
			WorkShiftsByWorkstationId[shift.itemInstanceId] = new List<WorkShift>();
		}
		WorkShiftsByWorkstationId[shift.itemInstanceId].Add(shift);
		if (!WorkShiftsByEmployeeId.ContainsKey(shift.employeeId))
		{
			WorkShiftsByEmployeeId[shift.employeeId] = new List<WorkShift>();
		}
		WorkShiftsByEmployeeId[shift.employeeId].Add(shift);
	}

	public static void RemoveShiftFromCache(WorkShift shift)
	{
		WorkShiftsByWorkstationId[shift.itemInstanceId].RemoveAll((WorkShift x) => x.startingHour == shift.startingHour && x.endingHour == shift.endingHour && x.employeeId == shift.employeeId);
		WorkShiftsByEmployeeId[shift.employeeId].RemoveAll((WorkShift x) => x.startingHour == shift.startingHour && x.endingHour == shift.endingHour && x.itemInstanceId == shift.itemInstanceId);
	}

	public static void AddWorkShift(int hour, string workstationId, string employeeId)
	{
		int endingHour;
		if (IsHeadquarters && !WorkstationsById[workstationId].IsCleaningStation())
		{
			OpeningHourSlot openingHourSlot = CurrentScheduleDay.openingHourSlots[0];
			endingHour = openingHourSlot.endingHour;
			hour = openingHourSlot.startingHour;
		}
		else
		{
			endingHour = CalculateEndingHour(hour, workstationId, employeeId);
		}
		WorkShift workShift = new WorkShift
		{
			startingHour = hour,
			itemInstanceId = workstationId,
			employeeId = employeeId,
			type = GetWorkShiftType(workstationId),
			endingHour = endingHour
		};
		if (IsHeadquarters && !WorkstationsById[workstationId].IsCleaningStation())
		{
			foreach (ScheduleDay scheduleDay in ScheduleDays)
			{
				if (scheduleDay.isOpen)
				{
					scheduleDay.AddWorkShift(workShift.Copy());
				}
			}
		}
		else
		{
			CurrentScheduleDay.AddWorkShift(workShift);
		}
		AddShiftToCache(workShift);
		UpdateEmployeeAfterWorkShiftChange(employeeId);
		RequestScheduleScrollerReload.Invoke(arg0: false);
		OnWorkShiftChanged.Invoke();
		GameEvent.Invoke("ba:gameevent_employeeassigned");
	}

	public static void RemoveWorkShift(string workstationId, string employeeId, int startingHour)
	{
		WorkShift workShift = CurrentScheduleDay.workShifts.FirstOrDefault((WorkShift x) => x.startingHour == startingHour && x.itemInstanceId == workstationId);
		RemoveShiftFromCache(workShift);
		if (IsHeadquarters && !WorkstationsById[workstationId].IsCleaningStation())
		{
			foreach (ScheduleDay scheduleDay in ScheduleDays)
			{
				if (scheduleDay.isOpen)
				{
					scheduleDay.RemoveWorkShift(workShift);
				}
			}
		}
		else
		{
			CurrentScheduleDay.RemoveWorkShift(workShift);
		}
		UpdateEmployeeAfterWorkShiftChange(employeeId);
		RequestScheduleScrollerReload.Invoke(arg0: false);
		OnWorkShiftChanged.Invoke();
	}

	public static void EditWorkShift(string workstationId, string employeeId, int originalStartingHour, int newStartingHour, int newEndingHour)
	{
		WorkShift workShift = WorkShiftsByWorkstationId[workstationId].Find((WorkShift x) => x.startingHour == originalStartingHour);
		workShift.startingHour = newStartingHour;
		workShift.endingHour = newEndingHour;
		if (IsHeadquarters && !WorkstationsById[workstationId].IsCleaningStation())
		{
			foreach (ScheduleDay scheduleDay in ScheduleDays)
			{
				if (scheduleDay.isOpen)
				{
					WorkShift workShift2 = scheduleDay.workShifts.Find((WorkShift x) => x.startingHour == originalStartingHour && x.employeeId == employeeId);
					if (workShift2 != null)
					{
						workShift2.startingHour = newStartingHour;
						workShift2.endingHour = newEndingHour;
					}
				}
			}
		}
		UpdateEmployeeAfterWorkShiftChange(employeeId);
	}

	public static void MoveWorkShift(WorkShift workShift, int newStartingHour, int newEndingHour, string newWorkstationId)
	{
		if (workShift.startingHour == newStartingHour && workShift.endingHour == newEndingHour && workShift.itemInstanceId == newWorkstationId)
		{
			return;
		}
		if (!string.Equals(workShift.itemInstanceId, newWorkstationId, StringComparison.Ordinal))
		{
			RemoveShiftFromCache(workShift);
			workShift.itemInstanceId = newWorkstationId;
			workShift.type = GetWorkShiftType(newWorkstationId);
			AddShiftToCache(workShift);
		}
		workShift.startingHour = newStartingHour;
		workShift.endingHour = newEndingHour;
		if (IsHeadquarters && !WorkstationsById[newWorkstationId].IsCleaningStation())
		{
			foreach (ScheduleDay scheduleDay in ScheduleDays)
			{
				if (scheduleDay.isOpen)
				{
					WorkShift workShift2 = scheduleDay.workShifts.Find((WorkShift x) => x.startingHour == workShift.startingHour && x.employeeId == workShift.employeeId);
					if (workShift2 != null)
					{
						workShift2.startingHour = newStartingHour;
						workShift2.endingHour = newEndingHour;
						workShift2.itemInstanceId = newWorkstationId;
					}
				}
			}
		}
		UpdateEmployeeAfterWorkShiftChange(workShift.employeeId);
	}

	public static void UpdateHQPlans(BizManBusiness business = null)
	{
		if ((object)business == null)
		{
			business = Business;
		}
		foreach (HeadhunterPlan assignedPlansForHeadquarter in HeadhunterHelper.GetAssignedPlansForHeadquarters(business.buildingRegistration.Address))
		{
			if (assignedPlansForHeadquarter.HeadhunterInstance != null && !assignedPlansForHeadquarter.HeadhunterInstance.IsAssignedToAnyWorkShift())
			{
				assignedPlansForHeadquarter.assignedEmployeeId = null;
			}
		}
		foreach (HrManagerPlan assignedPlansForHeadquarter2 in HrManagerHelper.GetAssignedPlansForHeadquarters(business.buildingRegistration.Address))
		{
			if (assignedPlansForHeadquarter2.HrManagerInstance != null && !assignedPlansForHeadquarter2.HrManagerInstance.IsAssignedToAnyWorkShift())
			{
				assignedPlansForHeadquarter2.UnAssignEmployee();
			}
		}
		foreach (LogisticsManagerPlan assignedPlansForHeadquarter3 in LogisticsManagerHelper.GetAssignedPlansForHeadquarters(business.buildingRegistration.Address))
		{
			if (assignedPlansForHeadquarter3.LogisticsManagerInstance != null && !assignedPlansForHeadquarter3.LogisticsManagerInstance.IsAssignedToAnyWorkShift())
			{
				assignedPlansForHeadquarter3.assignedEmployeeId = null;
			}
		}
		foreach (ImportPartnership assignedPlansForHeadquarter4 in PurchasingAgentHelper.GetAssignedPlansForHeadquarters(business.buildingRegistration.Address))
		{
			if (assignedPlansForHeadquarter4.PurchasingAgentInstance != null && !assignedPlansForHeadquarter4.PurchasingAgentInstance.IsAssignedToAnyWorkShift())
			{
				assignedPlansForHeadquarter4.UnAssignEmployee();
			}
		}
		foreach (PricingManagerPlan plansForHeadquarter in PricingManagerHelper.GetPlansForHeadquarters(business.buildingRegistration.Address))
		{
			EmployeeInstance analystInstance = plansForHeadquarter.AnalystInstance;
			if (analystInstance != null && !analystInstance.IsAssignedToAnyWorkShift())
			{
				plansForHeadquarter.UnAssignEmployee();
			}
		}
	}

	private static WorkShiftType GetWorkShiftType(string workstationId)
	{
		if (!IsCleaningStation(workstationId))
		{
			return WorkShiftType.Default;
		}
		return WorkShiftType.Cleaning;
	}

	public static bool IsCleaningStation(this ItemInstance instance)
	{
		return instance.ItemCached.HasTag(TagRef.Itemtag.iscleaningstation);
	}

	public static bool IsCleaningStation(string workstationId)
	{
		return WorkstationsById[workstationId].ItemCached.HasTag(TagRef.Itemtag.iscleaningstation);
	}

	public static List<string> GetAttachedItems(ItemInstance station)
	{
		List<string> list = new List<string>();
		ItemInstance itemInstance = null;
		if (!string.IsNullOrEmpty(station.parentId))
		{
			itemInstance = Business.buildingRegistration.itemInstances.GetValueOrDefault(station.parentId);
		}
		if (itemInstance == null)
		{
			itemInstance = station;
		}
		if ((itemInstance.ItemCached.type & AttachedItemTypeFilter) != 0)
		{
			list.Add(itemInstance.itemName);
		}
		foreach (AttachableChild stackedItem in itemInstance.stackedItems)
		{
			if ((ItemsGetter.GetByName(stackedItem.childItemName).type & AttachedItemTypeFilter) != 0)
			{
				list.Add(stackedItem.childItemName);
			}
		}
		return list;
	}

	public static void FetchWorkstations()
	{
		List<ItemInstance> assignableItems = Business.buildingRegistration.GetAssignableItems();
		if (WorkstationsById == null)
		{
			WorkstationsById = new Dictionary<string, ItemInstance>(assignableItems.Count);
		}
		WorkstationsById.Clear();
		if (Business.buildingRegistration.businessTypeName == "ba:businesstype_cinema")
		{
			foreach (ItemInstance value in Business.buildingRegistration.itemInstances.Values)
			{
				if (value.itemName == "ba:itemname_screencinema")
				{
					WorkstationsById.Add(value.id, value);
				}
			}
		}
		else if (Business.buildingRegistration.businessTypeName == "ba:businesstype_theater")
		{
			WorkstationsById.Add(string.Empty, null);
		}
		foreach (ItemInstance item in assignableItems.OrderByDescending(IsCleaningStation).ThenBy((ItemInstance x) => x.itemName).ThenBy((ItemInstance x) => x.id))
		{
			WorkstationsById.Add(item.id, item);
		}
	}

	public static bool HasSkillForWorkstation(string workstationId, string employeeId)
	{
		ItemInstance itemInstance = WorkstationsById[workstationId];
		foreach (Skill skill in GetScheduleEmployeeById(employeeId).characterData.skills)
		{
			string name = skill.name;
			if (itemInstance.ItemCached.suitableSkills.Contains(name))
			{
				return true;
			}
		}
		return false;
	}

	public static void RenameWorkstation(string workstationId, string newName)
	{
		WorkstationsById[workstationId].SetAlias(newName);
		RequestScheduleScrollerReload.Invoke(arg0: true);
	}

	private static int CalculateEndingHour(int hour, string workstationId, string employeeId)
	{
		int num = hour + 1;
		if (!WorkstationsById[workstationId].IsCleaningStation())
		{
			foreach (OpeningHourSlot openingHourSlot in CurrentScheduleDay.openingHourSlots)
			{
				if (openingHourSlot.startingHour <= hour && openingHourSlot.endingHour > hour)
				{
					num = openingHourSlot.endingHour;
					break;
				}
			}
		}
		else
		{
			num = 24;
		}
		foreach (WorkShift item in GetWorkShiftsByWorkstationId(workstationId).Concat(GetWorkShiftsByEmployeeId(employeeId)))
		{
			int firstOverlapHour = item.GetFirstOverlapHour(hour, num);
			if (firstOverlapHour != -1)
			{
				num = firstOverlapHour;
				if (num <= hour)
				{
					break;
				}
			}
		}
		int b = hour + 12;
		return Mathf.Min(num, b);
	}

	public static void ClearScheduleDay(int dayIndex, Action onCompleted = null)
	{
		LanguageChangeEventDataHolder bodyData = "bizman_schedule_clear_schedule_day".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			GetScheduleDay(dayIndex).ClearWorkShifts();
			UpdateEmployeesAfterWorkShiftChange(EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = Business.buildingRegistration.Address,
				excludeBeingReplaced = true
			}));
			OnWorkShiftChanged.Invoke();
			onCompleted?.Invoke();
		});
	}

	public static void ClearAllSchedule(Action onCompleted = null)
	{
		LanguageChangeEventDataHolder bodyData = "bizman_schedule_clear_schedule_all".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			foreach (ScheduleDay scheduleDay in ScheduleDays)
			{
				scheduleDay.ClearWorkShifts();
			}
			UpdateEmployeesAfterWorkShiftChange(EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = Business.buildingRegistration.Address,
				excludeBeingReplaced = true
			}));
			OnWorkShiftChanged.Invoke();
			onCompleted?.Invoke();
		});
	}

	public static bool IsLicensingFeeActive(Address address, string itemId, DayOfWeekOrdered day)
	{
		if (SaveGameManager.Current.disabledLicensingFees == null)
		{
			return true;
		}
		foreach (GameInstance.DisabledLicensingFee disabledLicensingFee in SaveGameManager.Current.disabledLicensingFees)
		{
			if (disabledLicensingFee.address == address && disabledLicensingFee.day == day && disabledLicensingFee.itemId == itemId)
			{
				return false;
			}
		}
		return true;
	}

	public static void SetLicensingFeeActive(Address address, string itemId, DayOfWeekOrdered day, bool active)
	{
		if (active == IsLicensingFeeActive(address, itemId, day))
		{
			return;
		}
		if (active)
		{
			SaveGameManager.Current.disabledLicensingFees.RemoveAll((GameInstance.DisabledLicensingFee x) => x.address == address && x.itemId == itemId && x.day == day);
			return;
		}
		SaveGameManager.Current.disabledLicensingFees.Add(new GameInstance.DisabledLicensingFee
		{
			address = address,
			itemId = itemId,
			day = day
		});
	}

	public static bool IsLicensingFeePaidToday(Address address, string itemId)
	{
		if (SaveGameManager.Current.paidLicensingFeesToday == null)
		{
			return false;
		}
		foreach (var (address2, text) in SaveGameManager.Current.paidLicensingFeesToday)
		{
			if (address2 == address && text == itemId)
			{
				return true;
			}
		}
		return false;
	}

	public static void SetLicensingFeePaidToday(Address address, string itemId)
	{
		SaveGameManager.Current.paidLicensingFeesToday.Add((address, itemId));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Business = null;
		CurrentScheduleDay = null;
		Employees = null;
		EmployeesById = null;
		OpeningHoursState = null;
		AddEmployeeIconVisibilityLookup = null;
		WorkShiftsByWorkstationId.Clear();
		WorkShiftsByEmployeeId.Clear();
		WorkstationsById = null;
		SimulatedScheduleDays = null;
		SimulatedWorkShift = null;
		CurrentSimulatedDayIndex = 0;
	}
}
