using System;
using System.Collections.Generic;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Special.PrivateDriverService;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Streets;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace UI.Overlays;

[Serializable]
public class GasStationOverlay : IOverlay
{
	private const float JerryCanCost = 30f;

	private const float PricePerLiter = 1.3f;

	private const float WashCost = 50f;

	private GasStationTrigger _currentStationTrigger;

	private PrivateDriverGarageTrigger _privateDriverGarageTrigger;

	private CarController _subscribedCarController;

	private ButtonInfo BuyJerryCanButton => new ButtonInfo("BuyJerryCan", "gasstationoverlay_jerrycan", new
	{
		price = 30f.ToShortCurrencyFormat()
	}, "gray", BuyJerryCan, PlayerAction.SecondaryInteract, CanUseGasStationButton());

	public static void Show(GasStationTrigger stationTrigger)
	{
		GasStationOverlay gasStation = InstanceBehavior<UIs>.Instance.overlayUI.gasStation;
		gasStation.SetStationTrigger(stationTrigger);
		gasStation.SubscribeToVehicleMovingChanged();
		OverlayUI.Show(gasStation);
	}

	public static void Hide(GasStationTrigger stationTrigger = null)
	{
		GasStationOverlay gasStation = InstanceBehavior<UIs>.Instance.overlayUI.gasStation;
		if (!stationTrigger || !(gasStation._currentStationTrigger != stationTrigger))
		{
			gasStation.UnsubscribeFromVehicleMovingChanged();
			OverlayUI.Hide(gasStation);
		}
	}

	public static void Show(PrivateDriverGarageTrigger stationTrigger)
	{
		GasStationOverlay gasStation = InstanceBehavior<UIs>.Instance.overlayUI.gasStation;
		gasStation.SetStationTrigger(stationTrigger);
		gasStation.SubscribeToVehicleMovingChanged();
		OverlayUI.Show(gasStation);
	}

	public static void Hide(PrivateDriverGarageTrigger stationTrigger)
	{
		GasStationOverlay gasStation = InstanceBehavior<UIs>.Instance.overlayUI.gasStation;
		if (!stationTrigger || !(gasStation._privateDriverGarageTrigger != stationTrigger))
		{
			gasStation.UnsubscribeFromVehicleMovingChanged();
			OverlayUI.Hide(gasStation);
		}
	}

	private void SetStationTrigger(GasStationTrigger stationTrigger)
	{
		_currentStationTrigger = stationTrigger;
		_privateDriverGarageTrigger = null;
	}

	private void SetStationTrigger(PrivateDriverGarageTrigger stationTrigger)
	{
		_privateDriverGarageTrigger = stationTrigger;
		_currentStationTrigger = null;
	}

	private void SubscribeToVehicleMovingChanged()
	{
		UnsubscribeFromVehicleMovingChanged();
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle is CarController subscribedCarController)
		{
			_subscribedCarController = subscribedCarController;
			_subscribedCarController.onMovingChanged += OnVehicleMovingChanged;
		}
	}

	private void UnsubscribeFromVehicleMovingChanged()
	{
		if ((bool)_subscribedCarController)
		{
			_subscribedCarController.onMovingChanged -= OnVehicleMovingChanged;
			_subscribedCarController = null;
		}
	}

	private static void OnVehicleMovingChanged(bool _)
	{
		InstanceBehavior<UIs>.Instance.overlayUI.RefreshButtons();
	}

	private void Repair()
	{
		if (!CanUseGasStationButton())
		{
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null || !selectedVehicle.vehicleType.IsMotorVehicle)
		{
			return;
		}
		float num = selectedVehicle.vehicleInstance.CalculateRepairCost();
		if (num.RoundToInt() != 0)
		{
			if (GameManager.ChangeMoneySafe(0f - num, GetTransactionInfo(), null, null, force: false, showNotification: true))
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.CarRepair, _currentStationTrigger.transform.position, 1f, isPlayerCreatedSound: true);
				selectedVehicle.Repair();
				SaveGameManager.Current.achievementsData.totalRepairCost += num;
				GameEvent.Invoke(string.Empty);
			}
			Show(_currentStationTrigger);
		}
	}

	private void WashVehicle()
	{
		if (!CanUseGasStationButton())
		{
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (!(selectedVehicle == null) && selectedVehicle is CarController carController)
		{
			if (GameManager.ChangeMoneySafe(-50f, GetTransactionInfo(), null, null, force: false, showNotification: true))
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, _currentStationTrigger.transform.position, 1f, isPlayerCreatedSound: true);
				carController.Wash();
				GameEvent.Invoke(string.Empty);
			}
			Show(_currentStationTrigger);
		}
	}

	private void BuyJerryCan()
	{
		if (!CanUseGasStationButton())
		{
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			if (PlayerHelper.ItemInstanceInHands != null)
			{
				Notifications.ShowError("playeritempurchaser_notification_hands_full");
				return;
			}
			if (GameManager.ChangeMoneySafe(-30f, GetTransactionInfo(), null, null, force: false, showNotification: true))
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, _currentStationTrigger.transform.position, 1f, isPlayerCreatedSound: true);
				ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomByTag(TagRef.Itemtag.isfuelcontainer));
				itemInstance.priceOnPurchase = 30f;
				PlayerHelper.ItemInstanceInHands = itemInstance;
				SaveGameManager.Current.achievementsData.totalGasCost += 30f;
				GameEvent.Invoke(string.Empty);
			}
		}
		else
		{
			if (selectedVehicle.vehicleInstance.cargoInstances.Count >= selectedVehicle.vehicleType.maxCargoCapacity)
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string> { 
				{
					"type",
					selectedVehicle.GetName().ToString()
				} };
				Notifications.Show(NotificationType.Error, "managecargo_notification_vehicle_full", notificationData);
				return;
			}
			if (GameManager.ChangeMoneySafe(-30f, GetTransactionInfo(), null, null, force: false, showNotification: true))
			{
				CargoInstance cargoInstance = new CargoInstance(ItemsGetter.GetRandomByTag(TagRef.Itemtag.isfuelcontainer), 1, 30f);
				selectedVehicle.vehicleInstance.AddToCargo(cargoInstance);
				SaveGameManager.Current.achievementsData.totalGasCost += 30f;
				GameEvent.Invoke(string.Empty);
			}
		}
		GameAnalytics.TrackBuyJerryCan(_currentStationTrigger.cbc.buildingRegistration.Address.ToAnalyticsString());
		Show(_currentStationTrigger);
	}

	private void Refuel()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (!selectedVehicle)
		{
			return;
		}
		float num = (selectedVehicle.vehicleType.maxFuel - selectedVehicle.GetCurrentFuel()) * 1.3f;
		if (num.RoundToInt() != 0)
		{
			if (GameManager.ChangeMoneySafe(0f - num, GetTransactionInfo(), null, null, force: false, showNotification: true))
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.CarRefuel, _currentStationTrigger.transform.position, 1f, isPlayerCreatedSound: true);
				selectedVehicle.SetFuel(selectedVehicle.vehicleType.maxFuel);
				SaveGameManager.Current.achievementsData.totalGasCost += num;
				GameEvent.Invoke(string.Empty);
			}
			Show(_currentStationTrigger);
		}
	}

	private void DropOff()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (!selectedVehicle)
		{
			return;
		}
		VehicleInstance vehicleInstance = selectedVehicle.vehicleInstance;
		if (!SaveGameManager.Current.VehicleInstances.Contains(vehicleInstance))
		{
			return;
		}
		PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
		if (!activeContract)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"businessName",
				_privateDriverGarageTrigger.cbc.buildingRegistration.BusinessName
			} };
			Notifications.Show(NotificationType.Error, "ba:private_driver_garage_drop_off_no_contract", notificationData, 10f, "ba:private_driver_garage_drop_off_no_contract", null, notificationSound: true, trackOnSaveGame: false);
			return;
		}
		if (!activeContract.usableVehicleTypes.Contains(vehicleInstance.vehicleTypeName))
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { 
			{
				"businessName",
				_privateDriverGarageTrigger.cbc.buildingRegistration.BusinessName
			} };
			Notifications.Show(NotificationType.Error, "ba:private_driver_garage_drop_off_wrong_vehicle_type", notificationData2, 10f, "ba:private_driver_garage_drop_off_wrong_vehicle_type", null, notificationSound: true, trackOnSaveGame: false);
			return;
		}
		GameInstance current = SaveGameManager.Current;
		if (current.privateDriverVehicleInstances == null)
		{
			current.privateDriverVehicleInstances = new List<VehicleInstance>();
		}
		if (SaveGameManager.Current.privateDriverVehicleInstances.Count >= activeContract.maxCars)
		{
			Dictionary<string, string> notificationData3 = new Dictionary<string, string> { 
			{
				"businessName",
				_privateDriverGarageTrigger.cbc.buildingRegistration.BusinessName
			} };
			Notifications.Show(NotificationType.Error, "ba:private_driver_garage_drop_off_max_cars", notificationData3, 10f, "ba:private_driver_garage_drop_off_max_cars", null, notificationSound: true, trackOnSaveGame: false);
		}
		else
		{
			Hide();
			_privateDriverGarageTrigger.StartDropOff(vehicleInstance);
		}
	}

	private TransactionInfo GetTransactionInfo()
	{
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"businessName",
			_currentStationTrigger.cbc.buildingRegistration.BusinessName
		} };
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		bool num = selectedVehicle != null && selectedVehicle.vehicleType.taxDeductible;
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itempurchase", data);
		if (num)
		{
			transactionInfo.SetTaxDeductibleName("ba:businesstype_gasstation");
		}
		return transactionInfo;
	}

	public Vector3 GetTargetPosition()
	{
		if (!_privateDriverGarageTrigger)
		{
			return _currentStationTrigger.transform.position;
		}
		return _privateDriverGarageTrigger.transform.position;
	}

	public LabelInfo GetFirstLineLabel()
	{
		if (!_privateDriverGarageTrigger)
		{
			return new LabelInfo(_currentStationTrigger.cbc.buildingRegistration.BusinessName, localize: false);
		}
		return new LabelInfo("ba:private_driver_garage");
	}

	public LabelInfo GetSecondLineLeftLabel()
	{
		return null;
	}

	public LabelInfo GetSecondLineRightLabel()
	{
		return null;
	}

	public ButtonInfo[] GetButtons()
	{
		if (!VehicleHelper.IsInsideMotorVehicle())
		{
			return GetButtonsOutsideVehicle();
		}
		return GetButtonsInsideVehicle();
	}

	private ButtonInfo[] GetButtonsInsideVehicle()
	{
		if ((bool)_privateDriverGarageTrigger)
		{
			return new ButtonInfo[1] { DropOffButton() };
		}
		if (!_currentStationTrigger.isRepairStation)
		{
			return new ButtonInfo[2]
			{
				RefuelButton(),
				BuyJerryCanButton
			};
		}
		if (_currentStationTrigger.isTruckGarage == HasVehicleTag(TagRef.Vehicletag.istruck))
		{
			return new ButtonInfo[2]
			{
				RepairButton(),
				WashButton()
			};
		}
		return null;
	}

	private ButtonInfo[] GetButtonsOutsideVehicle()
	{
		if ((bool)_privateDriverGarageTrigger)
		{
			return null;
		}
		bool flag = VehicleHelper.GetCurrentVehicle()?.VehicleType.HasTag(TagRef.Vehicletag.isscooter) ?? false;
		if (!(_currentStationTrigger.isRepairStation | flag))
		{
			return new ButtonInfo[1] { BuyJerryCanButton };
		}
		return null;
	}

	private static bool HasVehicleTag(int tagIndex)
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if ((bool)selectedVehicle)
		{
			return selectedVehicle.vehicleType.HasTag(tagIndex);
		}
		return false;
	}

	private static bool CanUseGasStationButton()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if ((bool)selectedVehicle)
		{
			return Mathf.Approximately(selectedVehicle.CurrentSpeed, 0f);
		}
		return true;
	}

	private ButtonInfo RepairButton()
	{
		float num = VehicleHelper.GetCurrentVehicle().CalculateRepairCost();
		return new ButtonInfo("RepairVehicle", "gasstationoverlay_repair", new
		{
			price = num.ToShortCurrencyFormat()
		}, "blue", Repair, PlayerAction.Interact, Mathf.RoundToInt(num) > 0 && CanUseGasStationButton());
	}

	private ButtonInfo WashButton()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		return new ButtonInfo(interactable: !(selectedVehicle == null) && selectedVehicle is CarController carController && carController.vehicleInstance.dirtiness > 0.1f && !carController.isWashing && CanUseGasStationButton(), name: "WashVehicle", key: "gasstationoverlay_wash", arguments: new
		{
			price = 50f.ToShortCurrencyFormat()
		}, color: "blue", onClick: WashVehicle, playerAction: PlayerAction.SecondaryInteract);
	}

	private ButtonInfo RefuelButton()
	{
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		float num = (selectedVehicle.vehicleType.maxFuel - selectedVehicle.GetCurrentFuel()) * 1.3f;
		return new ButtonInfo("FillUpVehicle", "gasstationoverlay_fillup", new
		{
			price = num.ToShortCurrencyFormat()
		}, "blue", Refuel, PlayerAction.Interact, num >= 1f && CanUseGasStationButton());
	}

	private ButtonInfo DropOffButton()
	{
		return new ButtonInfo("DropOffVehicle", "ba:private_driver_garage_drop_off", "blue", DropOff, PlayerAction.Interact, interactable: true);
	}
}
