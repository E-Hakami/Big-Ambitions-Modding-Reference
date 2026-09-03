using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.Items;
using Buildings.Retail.Businesses.CinemaTheater;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleCellView : BaTableCellView<ScheduleWorkstationModel>
{
	[BoxGroup("Disabled")]
	[SerializeField]
	private GameObject disabledOverlay;

	[BoxGroup("Disabled")]
	[SerializeField]
	private Image fadingBackground;

	[BoxGroup("Disabled")]
	[SerializeField]
	private float fadingBackgroundAlpha = 30f;

	[BoxGroup("Hours")]
	[SerializeField]
	private List<ScheduleCellHour> hours;

	[BoxGroup("Workstation")]
	[SerializeField]
	private TMP_InputField workstationNameInputField;

	[BoxGroup("Workstation")]
	[SerializeField]
	private TextLocalizationComponent customersPerHourLabel;

	[BoxGroup("Workstation")]
	[SerializeField]
	private TMP_Text attachedItemsLabel;

	[BoxGroup("Workstation")]
	[SerializeField]
	private ListTooltip attachedItemsTooltip;

	[BoxGroup("Workstation")]
	[SerializeField]
	private BasicTooltip itemNameTooltip;

	[BoxGroup("Workstation")]
	[SerializeField]
	private string attachedItemsTooltipDescriptionKey;

	[BoxGroup("Workstation")]
	[SerializeField]
	private string factoryItemsTooltipDescriptionKey;

	[BoxGroup("Workstation")]
	[SerializeField]
	private GameObject licensingFeeEnabledInfo;

	[BoxGroup("Workstation")]
	[SerializeField]
	private GameObject licensingFeeDisabledInfo;

	[BoxGroup("Workstation")]
	[SerializeField]
	private GameObject licensingFeePaidInfo;

	[BoxGroup("Workstation")]
	[SerializeField]
	private GameObject licensingFeeUnpaidInfo;

	[BoxGroup("Workstation")]
	[SerializeField]
	private TMP_Text licensingFeeLabel;

	[BoxGroup("Workstation")]
	[SerializeField]
	private Toggle toggle;

	[BoxGroup("Workstation")]
	[SerializeField]
	private Image workstationIcon;

	[BoxGroup("WorkShifts")]
	[SerializeField]
	private RectTransform workShiftParent;

	[BoxGroup("WorkShifts")]
	[SerializeField]
	private WorkShiftSlider workShiftSliderPrefab;

	private ScheduleWorkstationModel _data;

	private ItemInstance _workstation;

	private ItemAliasUpdateListener _aliasUpdateListener;

	private bool _isWorkShiftHelperSetUp;

	public string WorkstationId => _data.workstationId;

	public RectTransform WorkShiftParent => workShiftParent;

	protected override void Awake()
	{
		base.Awake();
		_aliasUpdateListener = new ItemAliasUpdateListener(RefreshWorkstationName);
	}

	private void Start()
	{
		workstationNameInputField.onEndEdit.AddListener(delegate(string value)
		{
			if (!(_data.workstationName == value))
			{
				ScheduleHelper.RenameWorkstation(_data.workstationId, value);
			}
		});
		toggle.onValueChanged.AddListener(OnToggleLicensingFeeChanged);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			WorkShiftHelper.SetUp(workShiftParent.rect.width);
			_isWorkShiftHelperSetUp = true;
		});
	}

	private void OnEnable()
	{
		ScheduleHelper.OnOpeningHourChanged.AddListener(UpdateHoursData);
	}

	private void OnDisable()
	{
		attachedItemsTooltip.Hide();
		ScheduleHelper.OnOpeningHourChanged.RemoveListener(UpdateHoursData);
		_aliasUpdateListener?.Clear();
		_workstation = null;
	}

	public override void SetData(ScheduleWorkstationModel data)
	{
		_data = data;
		workShiftParent.ClearChildren();
		base.name = "Workstation" + data.shiftType;
		SetWorkstationData();
		SetHoursData();
		if (!_isWorkShiftHelperSetUp)
		{
			CoroutineUtility.RunAfterOneFrame(SetWorkShiftsData);
		}
		else
		{
			SetWorkShiftsData();
		}
	}

	private void SetWorkstationData()
	{
		BuildingRegistration buildingRegistration = ScheduleHelper.Business.buildingRegistration;
		ItemInstance itemInstance = (_workstation = ScheduleHelper.WorkstationsById[_data.workstationId]);
		_aliasUpdateListener.ListenTo(itemInstance);
		bool flag = itemInstance == null || itemInstance.itemName == "ba:itemname_screencinema";
		bool flag2 = !ScheduleHelper.CurrentScheduleDay.isOpen;
		disabledOverlay.SetActive(flag2);
		fadingBackground.color = new Color(fadingBackground.color.r, fadingBackground.color.g, fadingBackground.color.b, flag2 ? (fadingBackgroundAlpha / 100f) : 1f);
		workstationIcon.sprite = itemInstance?.ItemCached.icon;
		workstationIcon.enabled = !flag;
		bool num = itemInstance != null && itemInstance.itemName.GetLocalization() != _data.workstationName;
		workstationNameInputField.SetTextWithoutNotify(_data.workstationName);
		workstationNameInputField.readOnly = itemInstance == null;
		if (num)
		{
			itemNameTooltip.enabled = true;
			itemNameTooltip.descriptionKey = itemInstance.itemName;
		}
		else
		{
			itemNameTooltip.enabled = false;
		}
		if (_data.customersPerHour > 0)
		{
			customersPerHourLabel.Key = ((_data.customersPerHour > 1) ? "bizman_schedule_customers_per_hour" : "bizman_schedule_one_customer_per_hour");
			if (_data.customersPerHour > 1)
			{
				customersPerHourLabel.Arguments = new
				{
					amount = _data.customersPerHour
				};
			}
			customersPerHourLabel.gameObject.SetActive(value: true);
		}
		else
		{
			customersPerHourLabel.gameObject.SetActive(value: false);
		}
		List<string> attachedItems = _data.attachedItems;
		if (attachedItems != null && attachedItems.Count > 0)
		{
			attachedItemsLabel.text = _data.attachedItems.Stringify(localize: true);
			attachedItemsTooltip.list = _data.attachedItems.Select((string x) => x.GetLocalization()).ToList();
			attachedItemsLabel.gameObject.SetActive(value: true);
			attachedItemsTooltip.enabled = true;
			attachedItemsTooltip.descriptionKey = (_data.isFactoryMachine ? factoryItemsTooltipDescriptionKey : attachedItemsTooltipDescriptionKey);
		}
		else
		{
			attachedItemsLabel.gameObject.SetActive(value: false);
			attachedItemsTooltip.enabled = false;
		}
		bool flag3 = flag && !flag2 && itemInstance != null;
		bool flag4 = flag && ScheduleHelper.IsLicensingFeeActive(buildingRegistration.Address, _data.workstationId, ScheduleHelper.CurrentScheduleDay.day);
		toggle.transform.parent.gameObject.SetActive(flag3);
		if (flag3)
		{
			toggle.SetIsOnWithoutNotify(flag4);
		}
		foreach (ScheduleCellHour hour in hours)
		{
			hour.gameObject.SetActive(!flag);
		}
		licensingFeeEnabledInfo.transform.parent.gameObject.SetActive(flag);
		licensingFeeEnabledInfo.SetActive(!flag2 & flag4);
		licensingFeeDisabledInfo.SetActive(!flag2 && !flag4);
		bool flag5 = IsToday();
		bool flag6 = LicensingFeesHelper.IsLicensingFeePaid(buildingRegistration, _data.workstationId);
		licensingFeePaidInfo.SetActive(flag5 & flag6);
		licensingFeeUnpaidInfo.SetActive((!flag2 & flag5) && toggle.isOn && !flag6);
		float val = ((itemInstance == null) ? CinemaTheaterHelper.TheaterStageLicensingFee : CinemaTheaterHelper.CinemaScreenLicensingFee);
		licensingFeeLabel.text = val.ToShortCurrencyFormat();
	}

	private void RefreshWorkstationName()
	{
		if (_workstation != null)
		{
			_data.workstationName = (string.IsNullOrEmpty(_workstation.alias) ? _workstation.itemName.GetLocalization() : _workstation.alias);
			SetWorkstationData();
		}
	}

	private void OnToggleLicensingFeeChanged(bool isOn)
	{
		if (!isOn)
		{
			Apply(newIsOn: false);
			return;
		}
		LanguageChangeEventDataHolder bodyData = new LanguageChangeEventDataHolder
		{
			Key = "bizman_schedule_licensing_fee_confirm_description",
			Arguments = new
			{
				amount = CinemaTheaterHelper.CinemaScreenLicensingFee.ToShortCurrencyFormat()
			}
		};
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			Apply(newIsOn: true);
		}, delegate
		{
			toggle.SetIsOnWithoutNotify(value: false);
		});
		void Apply(bool newIsOn)
		{
			BuildingRegistration buildingRegistration = ScheduleHelper.Business.buildingRegistration;
			ScheduleHelper.SetLicensingFeeActive(buildingRegistration.Address, _data.workstationId, ScheduleHelper.CurrentScheduleDay.day, newIsOn);
			bool flag = IsToday();
			if (newIsOn & flag)
			{
				LicensingFeesHelper.PayLicensingFees(buildingRegistration);
			}
			licensingFeeEnabledInfo.SetActive(newIsOn);
			licensingFeeDisabledInfo.SetActive(!newIsOn);
			bool flag2 = LicensingFeesHelper.IsLicensingFeePaid(buildingRegistration, _data.workstationId);
			licensingFeePaidInfo.SetActive(flag & flag2);
			licensingFeeUnpaidInfo.SetActive((flag & newIsOn) && !flag2);
		}
	}

	private static bool IsToday()
	{
		return ScheduleHelper.CurrentScheduleDay.day == TimeHelper.GetDayOfWeek();
	}

	private void SetHoursData()
	{
		if (string.IsNullOrEmpty(_data.workstationId))
		{
			return;
		}
		bool flag = ScheduleHelper.WorkstationsById[_data.workstationId].IsCleaningStation();
		for (int i = 0; i < 24; i++)
		{
			hours[i].SetUp(i, _data.workstationId, () => workShiftParent);
			hours[i].SetOpen(flag || ScheduleHelper.OpeningHoursState[i]);
		}
		UpdateHoursAddEmployeeIconVisibility();
	}

	private void SetWorkShiftsData()
	{
		foreach (WorkShift item in ScheduleHelper.GetWorkShiftsByWorkstationId(_data.workstationId))
		{
			WorkShiftSlider workShiftSlider = Object.Instantiate(workShiftSliderPrefab, workShiftParent);
			workShiftSlider.SetUp(OnWorkShiftChanged, workShiftParent, item);
			workShiftSlider.UpdateState(item);
		}
	}

	private void OnWorkShiftChanged()
	{
		UpdateHoursAddEmployeeIconVisibility();
	}

	private void UpdateHoursData(int hour, bool isOpen)
	{
		if (isOpen && IsToday())
		{
			LicensingFeesHelper.PayLicensingFees(ScheduleHelper.Business.buildingRegistration);
		}
		if (!string.IsNullOrEmpty(_data.workstationId) && !ScheduleHelper.IsCleaningStation(_data.workstationId))
		{
			hours[hour].SetOpen(isOpen);
			UpdateHoursAddEmployeeIconVisibility();
		}
	}

	private void UpdateHoursAddEmployeeIconVisibility()
	{
		if (!ScheduleHelper.CurrentScheduleDay.isOpen)
		{
			for (int i = 0; i < 24; i++)
			{
				hours[i].UpdateIconVisibility(visible: false);
				hours[i].SetOpen(isOpen: false);
			}
			return;
		}
		bool flag = ScheduleHelper.WorkstationsById[_data.workstationId].IsCleaningStation();
		bool[] addEmployeeIconVisibilityLookup = ScheduleHelper.AddEmployeeIconVisibilityLookup;
		List<WorkShift> workShiftsByWorkstationId = ScheduleHelper.GetWorkShiftsByWorkstationId(_data.workstationId);
		bool[] array = new bool[24];
		foreach (WorkShift item in workShiftsByWorkstationId)
		{
			for (int j = item.startingHour; j < item.endingHour; j++)
			{
				if (j >= 0 && j < 24)
				{
					array[j] = true;
				}
			}
		}
		bool flag2 = false;
		bool flag3 = false;
		for (int k = 0; k < 24; k++)
		{
			bool flag4 = array[k];
			bool flag5 = false;
			bool flag6 = flag2 && !flag4 && ScheduleHelper.OpeningHoursState[k];
			if (flag)
			{
				if (!flag4 && !flag3)
				{
					flag5 = true;
					flag3 = true;
				}
				else if (flag2 && !flag4)
				{
					flag5 = true;
				}
			}
			else if (addEmployeeIconVisibilityLookup[k] && !flag4)
			{
				flag5 = true;
			}
			hours[k].UpdateIconVisibility(flag5 | flag6);
			flag2 = flag4;
		}
	}
}
