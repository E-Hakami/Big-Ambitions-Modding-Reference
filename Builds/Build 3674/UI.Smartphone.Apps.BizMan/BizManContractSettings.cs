using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class BizManContractSettings : MonoBehaviour
{
	public TextLocalizationComponent contractTitleLabel;

	public Button endContractButton;

	public Button cancelContractButton;

	public Button orderButton;

	public Button urgentOrderButton;

	public TMP_Text totalCostLabel;

	public TextLocalizationComponent nextDeliveryDayLabel;

	public TextLocalizationComponent lockTimeLabel;

	public BizManDeliveriesProductsScrollerController bizManDeliveriesProductsScrollerController;

	public Toggle repeatingOrderToggle;

	private BizManDeliveries _bizManDeliveries;

	private DeliveryContract _deliveryContract;

	private BuildingRegistration _wholesalerRegistration;

	private List<string> _itemsForSale;

	public void Initialize()
	{
		_bizManDeliveries = GetComponentInParent<BizManDeliveries>();
		endContractButton.onClick.AddListener(EndContract);
		cancelContractButton.onClick.AddListener(CancelContract);
		orderButton.onClick.AddListener(StartOrder);
		urgentOrderButton.onClick.AddListener(MakeUrgentOrder);
		repeatingOrderToggle.onValueChanged.AddListener(OnRepeatingOrderToggleValueChanged);
	}

	public void ShowContractSettings(DeliveryContract newDeliveryContract)
	{
		_deliveryContract = newDeliveryContract;
		_wholesalerRegistration = BuildingHelper.GetBuildingRegistration(_deliveryContract.wholesaleAddress);
		contractTitleLabel.SetData("bizman_purchasingagents_contract_title".Localize(new
		{
			businessName = _wholesalerRegistration.BusinessName
		}));
		RefreshItems();
		UpdateLabels();
		cancelContractButton.gameObject.SetActive(_deliveryContract.enabled);
		urgentOrderButton.gameObject.SetActive(_deliveryContract.enabled);
		orderButton.gameObject.SetActive(!_deliveryContract.enabled);
		repeatingOrderToggle.SetIsOnWithoutNotify(_deliveryContract.repeatingOrder);
		base.gameObject.SetActive(value: true);
	}

	private void RefreshItems()
	{
		bizManDeliveriesProductsScrollerController.LoadProducts(_deliveryContract, UpdateLabels);
	}

	private void UpdateLabels()
	{
		if (_deliveryContract.enabled)
		{
			nextDeliveryDayLabel.SetData("timestamp_full".Localize(new
			{
				day = _deliveryContract.nextDeliveryDay,
				time = 8.GetFormattedTime()
			}));
		}
		else
		{
			nextDeliveryDayLabel.Key = "itempanelui_parkingzone_notavailable";
		}
		totalCostLabel.text = _deliveryContract.TotalPricePerDelivery.ToCurrencyFormat();
		SetLockTimeLabel();
	}

	private void SetLockTimeLabel()
	{
		if (_deliveryContract.isUrgentOrder)
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
		if (_deliveryContract.enabled && DeliveryHelper.IsLockPeriod())
		{
			return _deliveryContract.nextDeliveryDay != TimeHelper.GetNextDayOfWeekNumber(DayOfWeekOrdered.Monday);
		}
		return true;
	}

	private void EndContract()
	{
		if (!DeliveryHelper.CanModifyContract(_deliveryContract.nextDeliveryDay))
		{
			DeliveryHelper.ShowCantModifyContractNotification();
			return;
		}
		HudConfirm.Show(null, "hud_confirm_delivery_contract", delegate
		{
			_deliveryContract.Remove();
			_bizManDeliveries.RefreshData();
		});
	}

	private void CancelContract()
	{
		if (!DeliveryHelper.CanModifyContract(_deliveryContract.nextDeliveryDay))
		{
			DeliveryHelper.ShowCantModifyContractNotification();
			return;
		}
		_deliveryContract.enabled = false;
		_deliveryContract.isUrgentOrder = false;
		orderButton.gameObject.SetActive(value: true);
		cancelContractButton.gameObject.SetActive(value: false);
		urgentOrderButton.gameObject.SetActive(value: false);
		RefreshItems();
		UpdateLabels();
	}

	private void MakeUrgentOrder()
	{
		if (_deliveryContract.nextDeliveryDay == SaveGameManager.Current.Day + 1)
		{
			Notifications.ShowError("bizman_deliveries_notification_existing_delivery_for_tomorrow");
			return;
		}
		int percentage = Mathf.RoundToInt((DeliveryHelper.GetWholesaleUrgentFeeMultiplier() - 1f) * 100f);
		LanguageChangeEventDataHolder bodyData = new LanguageChangeEventDataHolder
		{
			Key = "bizman_deliveries_confirm_urgent_order_dynamic",
			Arguments = new { percentage }
		};
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			_deliveryContract.nextDeliveryDay = SaveGameManager.Current.Day + 1;
			_deliveryContract.isUrgentOrder = true;
			UpdateLabels();
		});
	}

	private void StartOrder()
	{
		if (!_deliveryContract.HasItemsToDeliver())
		{
			Notifications.ShowError("bizman_delivery_notification_no_items_to_deliver");
			return;
		}
		_deliveryContract.enabled = true;
		_deliveryContract.UpdateNextDeliveryDay();
		orderButton.gameObject.SetActive(value: false);
		cancelContractButton.gameObject.SetActive(value: true);
		urgentOrderButton.gameObject.SetActive(value: true);
		_bizManDeliveries.RefreshSelectedContractEntry(shouldShow: false);
		RefreshItems();
		UpdateLabels();
		GameEvent.Invoke("ba:gameevent_updateddeliverycontract");
	}

	private void OnRepeatingOrderToggleValueChanged(bool value)
	{
		_deliveryContract.repeatingOrder = value;
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
