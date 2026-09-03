using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PurchasingAgent;

public class PurchasingAgentPlanUI : MonoBehaviour
{
	public TextLocalizationComponent contractTitleLabel;

	public Button endPartnershipButton;

	public Button cancelContractButton;

	public Button orderButton;

	public Button urgentOrderButton;

	public TMP_Text nextDeliveryTotalLabel;

	public TextLocalizationComponent nextDeliveryDayLabel;

	public TextLocalizationComponent lockTimeLabel;

	public Toggle repeatingOrderToggle;

	public Toggle autoStockToggle;

	public TextLocalizationComponent amountLabel;

	public PurchasingAgentProductsScrollerController productsScrollerController;

	public NoManagerAssignedPopUp noManagerAssignedPopUp;

	public PurchasingAgentProductsMassActionsUI massActionsUI;

	[NonSerialized]
	public int lastSelectedProductIndex = -1;

	private PurchasingAgentsPlanList _purchasingAgentsPlanList;

	private ImportPartnership _currentImportPartnership;

	public List<ImportProduct> CurrentIpProducts => _currentImportPartnership.products;

	public bool IsContractActive => _currentImportPartnership.isActive;

	public void Initialize()
	{
		NoManagerAssignedPopUp obj = noManagerAssignedPopUp;
		obj.deletePlan = (Action)Delegate.Combine(obj.deletePlan, new Action(DeletePlan));
		_purchasingAgentsPlanList = GetComponentInParent<PurchasingAgentsPlanList>();
		endPartnershipButton.onClick.AddListener(EndPartnership);
		cancelContractButton.onClick.AddListener(CancelOrder);
		orderButton.onClick.AddListener(StartOrder);
		urgentOrderButton.onClick.AddListener(MakeUrgentOrder);
		repeatingOrderToggle.onValueChanged.AddListener(OnRepeatingOrderToggleValueChanged);
		autoStockToggle.onValueChanged.AddListener(OnAutoStockToggleValueChanged);
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		noManagerAssignedPopUp.Hide();
	}

	private void DeletePlan()
	{
		if (_currentImportPartnership == null)
		{
			Debug.LogError("No plan selected");
			return;
		}
		PurchasingAgentHelper.DeletePlan(_currentImportPartnership.id);
		SaveGameManager.MarkChange();
	}

	public void LoadPlan(ImportPartnership plan)
	{
		_currentImportPartnership = plan;
		PurchasingAgentProductsMassActionsUI.massActionSelectedProducts.Clear();
		massActionsUI.massActionAllToggle.SetIsOnWithoutNotify(value: false);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(_currentImportPartnership.importAddress);
		contractTitleLabel.SetData("bizman_purchasingagents_contract_title".Localize(new
		{
			businessName = buildingRegistration.BusinessName
		}));
		UpdateAmountLabel();
		CoroutineUtility.RunAfterOneFrame(RefreshItems);
		UpdateLabels();
		if (plan.employeeInstanceId == null)
		{
			noManagerAssignedPopUp.Show(!DeliveryHelper.CanModifyContract(plan.nextDeliveryDay));
		}
		else
		{
			noManagerAssignedPopUp.Hide();
		}
		cancelContractButton.gameObject.SetActive(_currentImportPartnership.isActive);
		orderButton.gameObject.SetActive(!_currentImportPartnership.isActive);
		urgentOrderButton.gameObject.SetActive(_currentImportPartnership.isActive);
		repeatingOrderToggle.SetIsOnWithoutNotify(_currentImportPartnership.isRepeatingOrder);
		autoStockToggle.SetIsOnWithoutNotify(_currentImportPartnership.isTarget);
		autoStockToggle.interactable = !_currentImportPartnership.isActive;
		base.gameObject.SetActive(value: true);
	}

	private void UpdateAmountLabel()
	{
		amountLabel.Key = (_currentImportPartnership.isTarget ? "bizman_purchasingagents_amount_target_on" : "bizman_purchasingagents_amount_to_buy");
	}

	private void UpdateLabels()
	{
		if (_currentImportPartnership.isActive)
		{
			nextDeliveryDayLabel.SetData("timestamp_full".Localize(new
			{
				day = _currentImportPartnership.nextDeliveryDay,
				time = 8.GetFormattedTime()
			}));
		}
		else
		{
			nextDeliveryDayLabel.Key = "itempanelui_parkingzone_notavailable";
		}
		float num = _currentImportPartnership.NextDeliveryTotal;
		if (_currentImportPartnership.isUrgentOrder)
		{
			num *= DeliveryHelper.GetImporterUrgentFeeMultiplier();
		}
		nextDeliveryTotalLabel.text = num.ToCurrencyFormat();
		SetLockTimeLabel();
	}

	private void SetLockTimeLabel()
	{
		if (_currentImportPartnership.isUrgentOrder)
		{
			lockTimeLabel.Key = "bizman_deliveries_urgent_order_placed";
		}
		else if (ShouldShowLockTime())
		{
			SetLockTime();
		}
		else
		{
			lockTimeLabel.Key = "itempanelui_parkingzone_notavailable";
		}
	}

	private void SetLockTime()
	{
		Timestamp nextLockPeriodStart = DeliveryHelper.GetNextLockPeriodStart();
		int num = Mathf.CeilToInt(nextLockPeriodStart.GetDifferenceInMinutes(TimeHelper.Now()) / 60f);
		int num2 = Mathf.FloorToInt((float)num / 24f);
		string timeLeft = ((num2 > 0) ? "common_days_left".Localize(new
		{
			days = num2
		}).ToString() : "common_hours_left".Localize(new
		{
			hours = num
		}).ToString());
		lockTimeLabel.SetData("bizman_purchasingagents_lock_time_content".Localize(new
		{
			day = nextLockPeriodStart.Day,
			time = nextLockPeriodStart.Hour.GetFormattedTime(),
			timeLeft = timeLeft
		}));
	}

	private bool ShouldShowLockTime()
	{
		if (_currentImportPartnership.isActive && DeliveryHelper.IsLockPeriod())
		{
			return _currentImportPartnership.nextDeliveryDay != TimeHelper.GetNextDayOfWeekNumber(DayOfWeekOrdered.Monday);
		}
		return true;
	}

	private void RefreshItems()
	{
		productsScrollerController.LoadProducts(_currentImportPartnership, UpdateLabels, massActionsUI);
	}

	private void EndPartnership()
	{
		if (!DeliveryHelper.CanModifyContract(_currentImportPartnership.nextDeliveryDay))
		{
			DeliveryHelper.ShowCantModifyContractNotification();
			return;
		}
		HudConfirm.Show(null, "contact_hud_confirm_end_parnetship", delegate
		{
			SaveGameManager.Current.importPartnerships.Remove(_currentImportPartnership);
			_purchasingAgentsPlanList.RefreshData();
			SaveGameManager.MarkChange();
		});
	}

	private void MakeUrgentOrder()
	{
		if (_currentImportPartnership.nextDeliveryDay == SaveGameManager.Current.Day + 1)
		{
			Notifications.ShowError("bizman_deliveries_notification_existing_delivery_for_tomorrow");
			return;
		}
		int percentage = Mathf.RoundToInt((DeliveryHelper.GetImporterUrgentFeeMultiplier() - 1f) * 100f);
		LanguageChangeEventDataHolder bodyData = new LanguageChangeEventDataHolder
		{
			Key = "bizman_deliveries_confirm_urgent_order_dynamic",
			Arguments = new { percentage }
		};
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			_currentImportPartnership.nextDeliveryDay = SaveGameManager.Current.Day + 1;
			_currentImportPartnership.isUrgentOrder = true;
			UpdateLabels();
		});
	}

	private void StartOrder()
	{
		if (!_currentImportPartnership.products.Any((ImportProduct x) => x.amount > 0))
		{
			Notifications.ShowError("bizman_delivery_notification_no_items_to_deliver");
			return;
		}
		if (_currentImportPartnership.products.Any((ImportProduct x) => x.amount > 0 && (x.assignedWarehouse == null || x.assignedWarehouse.IsUndefined())))
		{
			Notifications.ShowError("bizman_purchasingagents_contact_notification_no_warehouse_assigned");
			return;
		}
		_currentImportPartnership.isActive = true;
		_currentImportPartnership.nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay();
		orderButton.gameObject.SetActive(value: false);
		urgentOrderButton.gameObject.SetActive(value: true);
		cancelContractButton.gameObject.SetActive(value: true);
		autoStockToggle.interactable = !_currentImportPartnership.isActive;
		RefreshItems();
		UpdateLabels();
		SaveGameManager.MarkChange();
	}

	public void CancelOrder()
	{
		if (!DeliveryHelper.CanModifyContract(_currentImportPartnership.nextDeliveryDay))
		{
			DeliveryHelper.ShowCantModifyContractNotification();
			return;
		}
		_currentImportPartnership.isActive = false;
		_currentImportPartnership.isUrgentOrder = false;
		orderButton.gameObject.SetActive(value: true);
		urgentOrderButton.gameObject.SetActive(value: false);
		cancelContractButton.gameObject.SetActive(value: false);
		autoStockToggle.interactable = !_currentImportPartnership.isActive;
		RefreshItems();
		UpdateLabels();
		SaveGameManager.MarkChange();
	}

	private void OnRepeatingOrderToggleValueChanged(bool value)
	{
		_currentImportPartnership.isRepeatingOrder = value;
		SaveGameManager.MarkChange();
	}

	private void OnAutoStockToggleValueChanged(bool value)
	{
		_currentImportPartnership.isTarget = value;
		UpdateAmountLabel();
		productsScrollerController.UpdateIsTarget(_currentImportPartnership.isTarget);
		SaveGameManager.MarkChange();
	}
}
