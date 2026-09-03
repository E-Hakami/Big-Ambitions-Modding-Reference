using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using Buildings.Schedule;
using Entities;
using Helpers;
using PlayerActivity;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class BizManSchedule : MonoBehaviour
{
	public ScheduleAutoFillerUI scheduleAutoFillerUI;

	public ScheduleConfirm scheduleConfirm;

	[SerializeField]
	private BizManBusiness bizManBusiness;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private ItemType attachedItemTypeFilter;

	[Header("UI Components")]
	[SerializeField]
	private ScheduleScrollerController scrollerController;

	[SerializeField]
	private ScheduleDaySelectionController daySelectionController;

	[SerializeField]
	private ScheduleHeaderController headerController;

	private readonly List<ScheduleAutoFiller> _activeAutoFillers = new List<ScheduleAutoFiller>();

	private bool _isInitialized;

	private void Awake()
	{
		ScheduleHelper.Business = bizManBusiness;
		ScheduleHelper.AttachedItemTypeFilter = attachedItemTypeFilter;
		WorkShiftHelper.ScrollRect = scrollRect;
	}

	private void Start()
	{
		if (!_isInitialized)
		{
			daySelectionController.SetUp(OnDaySelected);
			headerController.SetUp(OnSearchFieldChanged);
			ScheduleHelper.PauseScheduleScroll.AddListener(delegate(bool pause)
			{
				scrollRect.enabled = !pause;
			});
			_isInitialized = true;
		}
	}

	private void OnDestroy()
	{
		foreach (ScheduleAutoFiller activeAutoFiller in _activeAutoFillers)
		{
			activeAutoFiller.onProgress.RemoveAllListeners();
			activeAutoFiller.onCompleted.RemoveAllListeners();
			activeAutoFiller.RequestCancel();
		}
	}

	private void OnDisable()
	{
		if (!(InstanceBehavior<GameManager>.Instance == null))
		{
			scheduleConfirm.gameObject.SetActive(value: false);
			CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(bizManBusiness.buildingRegistration, TimeHelper.GetDayOfWeek());
			GlobalEvents.onBuildingRegistrationChange?.Invoke(bizManBusiness.buildingRegistration.Address);
			if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity is WorkActivity workActivity)
			{
				workActivity.SetTimeToWork();
				InstanceBehavior<UIs>.Instance.playerActivityUI.UpdateActivityDisplay();
			}
		}
	}

	public void LoadScheduler()
	{
		if (!_isInitialized)
		{
			Start();
		}
		ScheduleHelper.FetchEmployees(ScheduleHelper.Business.buildingRegistration.Address);
		ScheduleHelper.FetchWorkstations();
		daySelectionController.UpdateState();
		bool flag = false;
		foreach (ScheduleAutoFiller activeAutoFiller in _activeAutoFillers)
		{
			if (activeAutoFiller.Registration == ScheduleHelper.Business.buildingRegistration)
			{
				scheduleAutoFillerUI.Show(activeAutoFiller);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			scheduleAutoFillerUI.gameObject.SetActive(value: false);
		}
	}

	public bool HandleEscapeClick()
	{
		if (InstanceBehavior<UIs>.Instance.textInputUi.gameObject.activeSelf)
		{
			InstanceBehavior<UIs>.Instance.textInputUi.OnClose();
			return true;
		}
		if (WorkShiftDrag.CurrentDraggedWorkShift != null)
		{
			WorkShiftDrag.CurrentDraggedWorkShift.OnEndDrag(null);
			return true;
		}
		return false;
	}

	public static void AutoFillSchedule(BizManBusiness business)
	{
		List<EmployeeInstance> employeeInstances = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = business.address
		});
		business.buildingRegistration.AutoFillSchedule(employeeInstances, null, warnIfUnassigned: false, fast: false, inhibitSuccessNotification: true);
	}

	public void RegisterAutoFiller(ScheduleAutoFiller autoFiller)
	{
		_activeAutoFillers.Add(autoFiller);
		autoFiller.onCompleted.AddListener(delegate(ScheduleAutoFiller filler, bool _)
		{
			_activeAutoFillers.Remove(filler);
		});
		if (base.gameObject.activeInHierarchy && bizManBusiness.buildingRegistration == ScheduleHelper.Business.buildingRegistration)
		{
			scheduleAutoFillerUI.Show(autoFiller);
		}
	}

	public static void AbortAutoFillForBusiness(BuildingRegistration business, bool notify = false)
	{
		if (business == null || !InstanceBehavior<UIs>.Instance || !InstanceBehavior<UIs>.Instance.fullMenu.schedule)
		{
			return;
		}
		foreach (ScheduleAutoFiller activeAutoFiller in InstanceBehavior<UIs>.Instance.fullMenu.schedule._activeAutoFillers)
		{
			if (activeAutoFiller.Registration == business)
			{
				activeAutoFiller.RequestCancel();
				if (notify)
				{
					Dictionary<string, string> notificationData = new Dictionary<string, string> { { "businessName", business.BusinessName } };
					Notifications.Show(NotificationType.Warning, "bizman_schedule_auto_fill_notify_cancel", notificationData);
				}
				break;
			}
		}
	}

	private void OnDaySelected(ScheduleDay scheduleDay)
	{
		ScheduleHelper.CurrentScheduleDay = scheduleDay;
		ScheduleHelper.CacheWorkShifts();
		headerController.UpdateState();
		scrollerController.LoadList(ScheduleHelper.WorkstationsById.Values.ToList());
	}

	private void OnSearchFieldChanged(string searchValue)
	{
		scrollerController.FilterReload(searchValue);
	}
}
