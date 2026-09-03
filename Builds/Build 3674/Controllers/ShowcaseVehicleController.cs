using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SoundSystem;
using Buildings;
using Cinemachine;
using DG.Tweening;
using Data.VehicleColors;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using UI;
using UI.Notification;
using UI.PurchaseVehicle;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Vehicles;
using Vehicles.VehicleTypes;

namespace Controllers;

public class ShowcaseVehicleController : ItemController, IPurchasableAsset
{
	[Header("Showcase Vehicle")]
	public string vehicleName;

	[SerializeField]
	private CarFeatures carFeatures;

	private string _initialColorName;

	private string _playerChosenColor;

	private VehicleType _vehicleType;

	public override void Awake()
	{
		_vehicleType = VehicleTypeHelper.GetVehicleType(vehicleName);
		customOverlayHeaderKey = vehicleName;
		base.Awake();
	}

	public override void Start()
	{
		base.Start();
		SetInitialColor();
	}

	public string GetLocalizeKey()
	{
		return vehicleName;
	}

	public float GetPurchasePrice()
	{
		return GetVehicleType().price;
	}

	public string GetInitialColor()
	{
		return _initialColorName;
	}

	public List<(string key, string value)> GetSpecs()
	{
		string item = $"{_vehicleType.maxCargoCapacity}";
		(string, string) item2 = ("common_cargo_capacity", item);
		string item3 = $"{_vehicleType.maxFuel}";
		(string, string) item4 = ("vehicle_tank_capacity", item3);
		string item5 = (_vehicleType.autoParkSupported ? "common_yes" : "common_no").Localize().ToString();
		(string, string) item6 = ("vehicle_auto_park_support", item5);
		string item7 = ((_vehicleType.maxSpeed == 0) ? "-" : $"{_vehicleType.maxSpeed}");
		(string, string) item8 = ("vehicle_max_speed", item7);
		return new List<(string, string)> { item2, item4, item6, item8 };
	}

	public List<(string, Color32)> GetColors()
	{
		return InstanceBehavior<GlobalReferences>.Instance.vehicleColors.Select((VehicleColor x) => (name: x.name, tint: x.tint)).ToList();
	}

	public void SetColor(string colorName, bool updateVisuals = true)
	{
		VehicleColor vehicleColor = InstanceBehavior<GlobalReferences>.Instance.vehicleColors.FirstOrDefault((VehicleColor x) => x.name == colorName);
		if (vehicleColor == null)
		{
			Debug.LogError("Vehicle color " + colorName + " not found on GlobalReferences.vehicleColors");
			return;
		}
		_playerChosenColor = colorName;
		if (updateVisuals)
		{
			carFeatures.SetColor(vehicleColor);
		}
	}

	public void ResetColor()
	{
		if (!string.IsNullOrEmpty(_initialColorName))
		{
			SetColor(_initialColorName);
		}
	}

	public bool Purchase()
	{
		Transform transform = InstanceBehavior<BuildingManager>.Instance.cityBuildingController.customPositions.First();
		Bounds spawnPointBounds = new Bounds(transform.position, Vector3.one * 6f);
		if (SaveGameManager.Current.VehicleInstances.Any((VehicleInstance x) => spawnPointBounds.Contains(x.position)))
		{
			Notifications.ShowError("purchasevehicleui_notification_recently_purchase_vehicle_is_blocking", "purchasevehicleui_notification_recently_purchase_vehicle_is_blocking");
			return false;
		}
		VehicleType vehicleType = GetVehicleType();
		Dictionary<string, string> data = new Dictionary<string, string> { { "vehicleName", vehicleName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_vehiclebought", data);
		if (vehicleType.taxDeductible)
		{
			transactionInfo.SetTaxDeductibleName("tax_vehicle");
		}
		if (!GameManager.ChangeMoneySafe(0f - GetPurchasePrice(), transactionInfo, null, null, force: false, showNotification: true))
		{
			return false;
		}
		float fuel = vehicleType.maxFuel * Random.Range(0.97f, 0.98f);
		VehicleHelper.CreateAndSpawnVehicle(new VehicleInstance(vehicleName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = _playerChosenColor,
			fuel = fuel
		}, transform.position, transform.rotation);
		if (!string.IsNullOrEmpty(_initialColorName))
		{
			SetColor(_initialColorName);
		}
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, InstanceBehavior<GameManager>.Instance.playerController.transform.position, 1f, isPlayerCreatedSound: true);
		GameAnalytics.TrackPurchaseVehicle(vehicleName, SaveGameManager.Current.Day);
		return true;
	}

	public void Order(Address deliveryAddress, Contact vehicleStoreContact, bool showNotification)
	{
		VehicleStoreSettings vehicleStoreSettings = VehicleDeliveryHelper.GetVehicleStoreSettings(vehicleStoreContact.Address);
		float deliveryPrice = GetPurchasePrice() + vehicleStoreSettings.deliveryPrice;
		Timestamp timestamp = TimeHelper.Now().AddHours(vehicleStoreSettings.hoursToDeliver);
		VehicleDeliveryContract item = new VehicleDeliveryContract
		{
			deliveryAddress = deliveryAddress,
			vehicleStoreAddress = vehicleStoreContact.Address,
			deliveryDay = timestamp.Day,
			deliveryHour = timestamp.Hour,
			vehicleColor = _playerChosenColor,
			vehicleTypeName = vehicleName,
			deliveryPrice = deliveryPrice
		};
		SendVehicleStoreContractMessages(deliveryAddress, vehicleStoreContact, vehicleStoreSettings, showNotification);
		SaveGameManager.Current.vehicleDeliveryContracts.Add(item);
	}

	private void SendVehicleStoreContractMessages(Address deliveryAddress, Contact contact, VehicleStoreSettings vehicleStoreSettings, bool showNotification)
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{ "vehicleTypeName", vehicleName },
			{
				"businessName",
				BuildingHelper.GetBuildingRegistration(deliveryAddress).GetDisplayName()
			}
		};
		contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_contract_set_player", messageData, !showNotification));
		Dictionary<string, string> messageData2 = new Dictionary<string, string> { 
		{
			"amount",
			vehicleStoreSettings.hoursToDeliver.ToString()
		} };
		contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_contract_set_manager", messageData2, !showNotification), showNotification, sendNotificationInstantly: true);
	}

	public IEnumerator ShowcaseAnimation()
	{
		Transform spawnPoint = InstanceBehavior<BuildingManager>.Instance.cityBuildingController.customPositions.First();
		PurchaseVehicleUI.runningShowcaseAnimation = true;
		yield return UiFader.Fade();
		InstanceBehavior<UIs>.Instance.GetComponent<CanvasGroup>().alpha = 0f;
		PurchaseVehicleUI.OutdoorCameraToggled.Invoke(arg0: true);
		CinemachineVirtualCameraBase previewCamera = InstanceBehavior<GameManager>.Instance.vehiclePreviewCamera;
		previewCamera.transform.position = spawnPoint.position + spawnPoint.forward * 12f - spawnPoint.right * 6f + Vector3.up * 5f;
		previewCamera.LookAt = spawnPoint;
		InstanceBehavior<GameManager>.Instance.timeOfDayController.OnExitBuilding();
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = false;
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = spawnPoint;
		yield return CameraHelper.SetCameraRoutine(previewCamera);
		yield return UiFader.UnFade();
		yield return previewCamera.transform.DOMove(previewCamera.transform.position - spawnPoint.transform.forward * 4f, 2f).SetUpdate(isIndependentUpdate: true).WaitForCompletion();
		yield return new WaitForSecondsRealtime(1f);
		yield return UiFader.Fade();
		InstanceBehavior<UIs>.Instance.canvasGroup.alpha = 1f;
		PurchaseVehicleUI.OutdoorCameraToggled.Invoke(arg0: false);
		InstanceBehavior<GameManager>.Instance.timeOfDayController.OnEnterBuilding();
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = true;
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = InstanceBehavior<GameManager>.Instance.playerController.transform;
		yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.indoorCamera);
		PurchaseVehicleUI.runningShowcaseAnimation = false;
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.Close();
		yield return UiFader.UnFade();
		Notifications.Show(NotificationType.Success, "purchasevehicleui_notification_purchase_successfull");
		GameEvent.Invoke("ba:gameevent_purchasecompleted");
	}

	public IEnumerator CancelShowcaseAnimation()
	{
		yield return UiFader.Fade(0.2f);
		InstanceBehavior<UIs>.Instance.canvasGroup.alpha = 1f;
		PurchaseVehicleUI.OutdoorCameraToggled.Invoke(arg0: false);
		yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.indoorCamera);
		PurchaseVehicleUI.runningCancelShowcaseAnimation = false;
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.Close();
		InstanceBehavior<GameManager>.Instance.timeOfDayController.OnEnterBuilding();
		InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = true;
		GameManager.GetMainCamera().GetComponent<HDAdditionalCameraData>().volumeAnchorOverride = InstanceBehavior<GameManager>.Instance.playerController.transform;
		yield return UiFader.UnFade(0.2f);
		Notifications.Show(NotificationType.Success, "purchasevehicleui_notification_purchase_successfull");
		GameEvent.Invoke("ba:gameevent_purchasecompleted");
	}

	private void SetInitialColor()
	{
		if (string.IsNullOrEmpty(customValue))
		{
			_initialColorName = InstanceBehavior<GlobalReferences>.Instance.vehicleColors.GetRandomWeighted(InstanceBehavior<GlobalReferences>.Instance.vehicleColors.Select((VehicleColor x) => x.randomWeight).ToArray()).name;
		}
		else
		{
			_initialColorName = customValue;
		}
		SetColor(_initialColorName);
	}

	public void ShowPurchaseUi()
	{
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.SetAsset(this);
	}

	private VehicleType GetVehicleType()
	{
		if (_vehicleType != null)
		{
			return _vehicleType;
		}
		_vehicleType = VehicleTypeHelper.GetVehicleType(vehicleName);
		return _vehicleType;
	}

	[ConsoleMethod("SetVehicleShowcaseColor", "Sets the color for the vehicle showcase", new string[] { })]
	public static void Command_SetVehicleShowcaseColor(string colorName)
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced == null || !(currentPlaceableItemBeingPlaced is ShowcaseVehicleController showcaseVehicleController))
		{
			Debug.LogWarning("Couldn't find an Showcase Vehicle Controller. Ensure you have one in hand and are in placement mode");
			return;
		}
		VehicleColor vehicleColor = InstanceBehavior<GlobalReferences>.Instance.vehicleColors.FirstOrDefault((VehicleColor x) => x.name.ToLower().Contains(colorName.ToLower()));
		if (vehicleColor == null)
		{
			Debug.LogWarning("No vehicle color found with name " + colorName + ". Vehicle colors are on GlobalReferences => VehicleColors");
			return;
		}
		showcaseVehicleController.customValue = vehicleColor.name;
		showcaseVehicleController.SetInitialColor();
		Debug.Log(showcaseVehicleController.name + " is now set to color " + vehicleColor.name + " by default");
	}

	[ConsoleMethod("ResetVehicleShowcaseColor", "Resets the color for the vehicle showcase", new string[] { })]
	public static void Command_ResetVehicleShowcaseColor()
	{
		IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
		if (currentPlaceableItemBeingPlaced == null || !(currentPlaceableItemBeingPlaced is ShowcaseVehicleController showcaseVehicleController))
		{
			Debug.LogWarning("Couldn't find an Showcase Vehicle Controller. Ensure you have one in hand and are in placement mode");
			return;
		}
		showcaseVehicleController.customValue = null;
		showcaseVehicleController.SetInitialColor();
		Debug.Log(showcaseVehicleController.name + " default color has been reset");
	}
}
