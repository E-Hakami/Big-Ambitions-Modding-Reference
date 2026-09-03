using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using DG.Tweening;
using EmployeeStations;
using Helpers;
using JimmysUnityUtilities;
using Streets;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace PlayerActivity;

public class WorkActivity : IPlayerActivity
{
	private readonly JobInstance _jobInstance;

	private readonly Job _job;

	private readonly EmployeeStationController _employeeStationController;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToWork;

	private int _minutesWorked;

	private Timestamp _finishTime;

	private bool IsWorkingInPlayerBuilding => _jobInstance == null;

	private LabelInfo BusinessClosedLabel => new LabelInfo("workpanel_business_is_currently_closed", null, InstanceBehavior<GlobalReferences>.Instance.colors.red);

	private LabelInfo DayOffLabel => new LabelInfo("playerhud_currentjob_day_off", null, InstanceBehavior<GlobalReferences>.Instance.colors.orange);

	private LabelInfo NoShiftsAssignedLabel => new LabelInfo("workui_no_shifts_for_today", null, InstanceBehavior<GlobalReferences>.Instance.colors.orange);

	private LabelInfo ShiftNotYetStartedLabel => new LabelInfo("workpanel_business_shift_not_yet_started", null, InstanceBehavior<GlobalReferences>.Instance.colors.yellow);

	private LabelInfo ShiftStartsSoonLabel => new LabelInfo("workpanel_business_shift_starts_soon", null, InstanceBehavior<GlobalReferences>.Instance.colors.yellow);

	private LabelInfo EmployeeCurrentlyAssignedLabel => new LabelInfo("workpanel_employee_is_currently_assigned", new { _employeeStationController.employee.employeeInstance.characterData.name }, InstanceBehavior<GlobalReferences>.Instance.colors.yellow);

	private LabelInfo OpeningSoonLabel => new LabelInfo("workpanel_business_opening_soon", null, InstanceBehavior<GlobalReferences>.Instance.colors.yellow);

	private ButtonInfo CancelButton => new ButtonInfo("CancelWork", "common_cancel", "gray", CancelWorking, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartWork", "workui_start", "blue", StartWorking, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopWork", "workui_stop", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return false;
	}

	public WorkActivity(JobInstance jobInstance, EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_employeeStationController = attachedEntity as EmployeeStationController;
		_jobInstance = jobInstance;
		if (jobInstance != null)
		{
			_job = JobHelper.GetJob(jobInstance.address);
		}
		SetTimeToWork();
	}

	public void LinkPointAndClickObject(EntityController entityController)
	{
	}

	public PlayerActivityState GetState()
	{
		return _state;
	}

	public PlayerActivityState GetStateBeforeFinishing()
	{
		return _stateBeforeFinishing;
	}

	public void ChangeState(PlayerActivityState state)
	{
		_state = state;
	}

	private void StartWorking()
	{
		if (SaveGameManager.Current.Energy <= 0f)
		{
			Notifications.ShowInsufficientEnergy();
			return;
		}
		_state = PlayerActivityState.MovingTowardsActivity;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_employeeStationController, delegate
				{
					if (_state == PlayerActivityState.MovingTowardsActivity)
					{
						_state = PlayerActivityState.Started;
						PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
						playerController.SetNavigationBlocker(NavigationBlocker.WorkActivity);
						EnergyHelper.RemoveEnergySpender("move");
						EnergyHelper.AddEnergySpender("work", _employeeStationController.workEnergyConsumption);
						GameEvent.Invoke("ba:gameevent_workingatemployeestation");
						_employeeStationController.UnassignEmployee();
						_employeeStationController.Occupied = true;
						_employeeStationController.AssignEmployee(playerController.Character, PlayerHelper.GetPlayerEmployeeInstance());
						Vector3 firstQueuePosition = ((IWaitingLineHolder)_employeeStationController).GetFirstQueuePosition();
						firstQueuePosition.y = playerController.transform.position.y;
						playerController.Character.transform.DOLookAt(firstQueuePosition, 0.2f);
						playerController.Character.LinkToPointAndClickObject(_employeeStationController);
					}
				});
			}
		});
	}

	public void Perform(int minutes)
	{
		_minutesWorked += minutes;
		if (_finishTime.IsInThePast())
		{
			if (HasToKeepWorking())
			{
				SetTimeToWork();
			}
			else
			{
				Finish();
			}
		}
	}

	private bool HasToKeepWorking()
	{
		if (TimeHelper.CurrentHour == 0 && IsWorkingInPlayerBuilding)
		{
			return GetPlayerBusinessOpeningHourSlot(onlyCurrentSlot: true) != null;
		}
		return false;
	}

	public void Finish()
	{
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		float num = (float)_minutesWorked / 60f;
		if (!IsWorkingInPlayerBuilding)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(_jobInstance.address);
			ScheduleDay scheduleDay = _job.scheduleDays.FirstOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
			int num2 = ((scheduleDay == null) ? 8 : (scheduleDay.openingHourSlots[0].endingHour - scheduleDay.openingHourSlots[0].startingHour));
			if (num > (float)num2)
			{
				num = num2;
			}
			int num3 = Mathf.FloorToInt(num * _job.hourlySalary);
			Dictionary<string, string> data = new Dictionary<string, string>
			{
				{
					"address",
					buildingRegistration.Address.ToFormattedString()
				},
				{ "businessName", buildingRegistration.BusinessName }
			};
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_playerjobsalary", "ba:transactioncategory_salaryincome", data);
			GameManager.ChangeMoneySafe(num3, transactionInfo);
		}
		float amount = num * Random.Range(0.05f, 0.1f);
		PlayerHelper.GetPlayerEmployeeInstance().IncreaseSkill("ba:skill_customerservice", amount);
		_employeeStationController.UnassignEmployee();
		_employeeStationController.Occupied = false;
		if (InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
				{
					_employeeStationController.UpdateEmployee();
				}
				else
				{
					InstanceBehavior<BuildingManager>.Instance.AssignAiToEmployeeStation(_employeeStationController);
				}
			});
		}
		EnergyHelper.RemoveEnergySpender("work");
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.WorkActivity);
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		CancelWorking();
	}

	private void CancelWorking()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesWorked = 0;
	}

	public void SetTimeToWork()
	{
		if (_state == PlayerActivityState.Running && !HasShiftAssignedToday())
		{
			_minutesToWork = 0;
			Finish();
			return;
		}
		int num = ((_job == null) ? (GetPlayerBusinessOpeningHourSlot()?.endingHour ?? (-1)) : ((_job.scheduleDays.SingleOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek())?.workShifts?.FirstOrDefault())?.endingHour ?? (-1)));
		Timestamp timestamp = new Timestamp(SaveGameManager.Current.Day, (num == -1) ? 24 : num, 0f);
		if (timestamp.IsInThePast())
		{
			timestamp.Day++;
		}
		_finishTime = timestamp;
		_minutesToWork = Mathf.CeilToInt(timestamp.GetTotalMinutes() - TimeHelper.NowInMinutes()) + _minutesWorked;
	}

	private bool IsJobShiftActive()
	{
		if (IsWorkingInPlayerBuilding)
		{
			ScheduleDay scheduleDay = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.scheduleDays.FirstOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
			List<OpeningHourSlot> list = scheduleDay?.openingHourSlots;
			if (list != null && list.Count > 0)
			{
				return scheduleDay.openingHourSlots.Any((OpeningHourSlot x) => x.startingHour <= SaveGameManager.Current.Hour && x.endingHour > SaveGameManager.Current.Hour);
			}
			return false;
		}
		ScheduleDay scheduleDay2 = _job.scheduleDays.FirstOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
		List<WorkShift> list2 = scheduleDay2?.workShifts;
		if (list2 != null && list2.Count > 0)
		{
			return scheduleDay2.workShifts.Any((WorkShift x) => x.startingHour <= SaveGameManager.Current.Hour && x.endingHour > SaveGameManager.Current.Hour);
		}
		return false;
	}

	private int GetHoursUntilNextJobShift()
	{
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		int hour = SaveGameManager.Current.Hour;
		for (int i = 0; i < 7; i++)
		{
			DayOfWeekOrdered dayToCheck = (DayOfWeekOrdered)((int)(dayOfWeek + i) % 7 + 1);
			ScheduleDay scheduleDay = _job.scheduleDays.FirstOrDefault((ScheduleDay x) => x.day == dayToCheck);
			if (scheduleDay == null || scheduleDay.workShifts.Count == 0)
			{
				continue;
			}
			foreach (WorkShift item in scheduleDay.workShifts.OrderBy((WorkShift x) => x.startingHour))
			{
				if (i == 0 && hour < item.startingHour)
				{
					return item.startingHour - hour;
				}
				if (i > 0)
				{
					return 24 - hour + (i - 1) * 24 + item.startingHour;
				}
			}
		}
		return -1;
	}

	private bool HasShiftAssignedToday()
	{
		if (IsWorkingInPlayerBuilding)
		{
			return GetPlayerBusinessOpeningHourSlot() != null;
		}
		if (!HasDayOff())
		{
			return !HasEndedTodaysShift();
		}
		return false;
	}

	private bool HasDayOff()
	{
		ScheduleDay scheduleDay = _job?.scheduleDays.SingleOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
		if (scheduleDay == null)
		{
			return false;
		}
		if (!IsWorkingInPlayerBuilding)
		{
			return !scheduleDay.isOpen;
		}
		return false;
	}

	private bool HasEndedTodaysShift()
	{
		ScheduleDay scheduleDay = _job?.scheduleDays.SingleOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
		if (scheduleDay == null)
		{
			return true;
		}
		return scheduleDay.workShifts.First().endingHour <= SaveGameManager.Current.Hour;
	}

	private OpeningHourSlot GetPlayerBusinessOpeningHourSlot(bool onlyCurrentSlot = false)
	{
		List<OpeningHourSlot> list = BuildingHelper.GetTodaySchedule(InstanceBehavior<BuildingManager>.Instance.buildingRegistration)?.openingHourSlots;
		OpeningHourSlot openingHourSlot = null;
		if (list != null && !InstanceBehavior<BuildingManager>.Instance.buildingRegistration.temporarilyClosed)
		{
			foreach (OpeningHourSlot item in list)
			{
				if (SaveGameManager.Current.Hour >= item?.startingHour && SaveGameManager.Current.Hour < item.endingHour)
				{
					openingHourSlot = item;
					break;
				}
				if (!onlyCurrentSlot && SaveGameManager.Current.Hour < item?.startingHour)
				{
					if (openingHourSlot == null)
					{
						openingHourSlot = item;
					}
					else if (item.startingHour < openingHourSlot.startingHour)
					{
						openingHourSlot = item;
					}
				}
			}
		}
		return openingHourSlot;
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo((_job == null) ? "workui_headline_label_default" : "workui_headline_label_with_job", (_job == null) ? null : new
		{
			jobName = _job.localizeKey
		}, InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		if (_state == PlayerActivityState.Running)
		{
			return null;
		}
		List<LabelInfo> list = new List<LabelInfo>();
		if (!InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			list.Add(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.OpensWithinHours(2) ? OpeningSoonLabel : BusinessClosedLabel);
		}
		else if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.temporarilyClosed)
		{
			list.Add(BusinessClosedLabel);
		}
		if (HasDayOff())
		{
			list.Add(DayOffLabel);
		}
		else if (!HasShiftAssignedToday())
		{
			list.Add(NoShiftsAssignedLabel);
		}
		else if (!IsWorkingInPlayerBuilding)
		{
			WorkShift workHoursToday = JobHelper.GetWorkHoursToday(_jobInstance);
			if (workHoursToday == null || workHoursToday.endingHour <= SaveGameManager.Current.Hour)
			{
				list.Add(NoShiftsAssignedLabel);
			}
			else
			{
				int hoursUntilNextJobShift = GetHoursUntilNextJobShift();
				if (hoursUntilNextJobShift >= 0 && hoursUntilNextJobShift <= 2)
				{
					list.Add(ShiftStartsSoonLabel);
				}
				else if (workHoursToday.startingHour > SaveGameManager.Current.Hour)
				{
					list.Add(ShiftNotYetStartedLabel);
				}
			}
		}
		if (IsWorkingInPlayerBuilding && _employeeStationController.employee != null && _employeeStationController.employee.employeeInstance.IsEmployeeAvailable())
		{
			list.Add(EmployeeCurrentlyAssignedLabel);
		}
		if (list.Count != 0)
		{
			return list.ToArray();
		}
		return null;
	}

	public ButtonInfo[] GetButtons()
	{
		List<ButtonInfo> list;
		switch (_state)
		{
		case PlayerActivityState.NotStarted:
			list = new List<ButtonInfo> { CancelButton };
			if (IsWorkingInPlayerBuilding || !HasShiftAssignedToday())
			{
				goto IL_0049;
			}
			if (!IsJobShiftActive())
			{
				int hoursUntilNextJobShift = GetHoursUntilNextJobShift();
				if (hoursUntilNextJobShift < 0 || hoursUntilNextJobShift > 2)
				{
					goto IL_0049;
				}
			}
			goto IL_006b;
		case PlayerActivityState.Running:
			return new ButtonInfo[1] { StopButton };
		default:
			{
				return null;
			}
			IL_0077:
			return list.ToArray();
			IL_0049:
			if (IsWorkingInPlayerBuilding && (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.OpensWithinHours(2) || IsJobShiftActive()))
			{
				goto IL_006b;
			}
			goto IL_0077;
			IL_006b:
			list.Add(StartButton);
			goto IL_0077;
		}
	}

	public bool HasTimeMachine()
	{
		return _state == PlayerActivityState.Running;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		return Mathf.CeilToInt(_finishTime.GetMinutesFromNow());
	}

	public Timestamp GetTimeMachineGoal()
	{
		return _finishTime;
	}

	public bool HasFastForward()
	{
		return _state == PlayerActivityState.Running;
	}

	public bool HasProgressBar()
	{
		return _state == PlayerActivityState.Running;
	}

	public float GetProgressBarPercentageValue()
	{
		return 100f * (float)(_minutesToWork - GetRemainingMinutesForTimeMachine()) / (float)_minutesToWork;
	}

	public (string, object) GetProgressBarLabel()
	{
		int remainingMinutesForTimeMachine = GetRemainingMinutesForTimeMachine();
		int num = Mathf.FloorToInt((float)remainingMinutesForTimeMachine / 60f);
		int minutes = remainingMinutesForTimeMachine - num * 60;
		object item = new
		{
			hours = num,
			minutes = minutes
		};
		return ("playeractivityui_time_remaining", item);
	}

	public bool HasSlider()
	{
		return false;
	}

	public int GetMinSliderValue()
	{
		return 0;
	}

	public int GetMaxSliderValue()
	{
		return 0;
	}

	public float GetCurrentSliderValue()
	{
		return 0f;
	}

	public void OnSliderValueChanged(int value)
	{
	}

	public (string, object) GetSliderInfo()
	{
		return (null, null);
	}
}
