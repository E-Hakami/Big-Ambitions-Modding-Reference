using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using PlayerActivity;
using PlayerActivity.Tennis;
using Seasons;
using TMPro;
using UI.Elements;
using UI.InteriorDesigner;
using UI.Load;
using UI.MiniMenu;
using UI.Notification;
using UI.Overlays;
using UI.PlayerHUD;
using UI.Purchase;
using UI.Smartphone;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.ItemPanel;

[RequireComponent(typeof(Transform))]
public class ItemPanelUI : MonoBehaviour
{
	public static readonly HashSet<string> VehiclesThatCanNotBeParked = new HashSet<string>();

	public CanvasGroup panel;

	public VehicleInfoPanel vehicleInfo;

	public TextLocalizationComponent itemNameLabel;

	public Button autoParkButton;

	public Button sellButton;

	public Button parkButton;

	public Button sleepButton;

	public Button grabButton;

	public Button discardButton;

	public Button leaveButton;

	public Button placeButton;

	public TextLocalizationComponent placeButtonLabel;

	public Transform metaInfo;

	public GameObject cargoContainer;

	public Transform cargoSlotTemplate;

	public Color cargoUnpaidSlotColor;

	public Transform maintenanceInfo;

	public ProgressBar maintenanceProgress;

	private VehicleController _vehicleController;

	private List<CargoItem> _cargoItems = new List<CargoItem>();

	private bool _isInMiniGame;

	[NonSerialized]
	public readonly UnityEvent onPaidForItems = new UnityEvent();

	[NonSerialized]
	public ItemInstance selectedItemInstance;

	public static bool IsSelling { get; private set; }

	public static bool IsVisible { get; private set; }

	private void Awake()
	{
		panel.alpha = 0f;
		panel.gameObject.SetActive(value: false);
	}

	private void Start()
	{
		IsVisible = false;
		onPaidForItems.AddListener(SetupCargoSlots);
		onPaidForItems.AddListener(SetUnpaidAmount);
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new Action(UpdateButtonsVisibility));
		PlacementSystem.onItemPlaced = (Action)Delegate.Combine(PlacementSystem.onItemPlaced, (Action)delegate
		{
			Toggle(isEnabled: false);
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			if (selectedItemInstance == null)
			{
				Toggle(isEnabled: false);
			}
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			UpdateButtonsVisibility();
			SetupCargoSlots();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			UpdateButtonsVisibility();
			SetupCargoSlots();
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (!PurchaseUI.IsPanelOpen && IsVisible)
			{
				base.gameObject.SetActive(!isOpen);
			}
		});
		GlobalEvents.RegisterOnGameLoadedCallback(SetUpKeysLabels);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPaused));
	}

	private void Update()
	{
		if (IsVisible && !LoadScene.isLoading && !CityMap.IsOpen && !FullMenu.IsOpen && !UI.MiniMenu.MiniMenu.IsOpen && !Feedback.IsOpen && !InstanceBehavior<BuildingManager>.Instance.enteringBuilding && !InstanceBehavior<BuildingManager>.Instance.exitingBuilding)
		{
			if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
			{
				vehicleInfo.UpdateInfo();
			}
			if (!GameManager.ShouldBlockKeyboardShortcuts() && !InteriorDesignerUI.IsOpen)
			{
				ProcessInputs();
			}
		}
	}

	private void OnDestroy()
	{
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Remove(PlacementSystem.onPlacementModeEnd, new Action(UpdateButtonsVisibility));
		PlacementSystem.onItemPlaced = (Action)Delegate.Remove(PlacementSystem.onItemPlaced, (Action)delegate
		{
			Toggle(isEnabled: false);
		});
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPaused));
	}

	private void ProcessInputs()
	{
		if (PlayerAction.Sell.Pressed() && sellButton.gameObject.activeSelf && sellButton.interactable)
		{
			ClickSell();
		}
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			if (PlayerAction.Sleep.Pressed())
			{
				ClickSleep();
			}
			if (PlayerAction.SecondaryInteract.Pressed() && InstanceBehavior<UIs>.Instance.overlayUI?.GetCurrentOverlayType != typeof(ElevatorOverlay))
			{
				ClickPark();
			}
		}
		if (PlayerAction.SecondaryInteract.Pressed() && placeButton.gameObject.activeSelf)
		{
			ClickPlace();
		}
	}

	public void HidePanel(bool hide)
	{
		panel.alpha = ((!hide) ? 1 : 0);
		panel.interactable = !hide;
	}

	private void OnPaused(bool paused)
	{
		if (PlacementSystem.IsInPlacementMode)
		{
			paused = false;
		}
		autoParkButton.interactable = !paused;
		parkButton.interactable = !paused;
		sleepButton.interactable = !paused;
		leaveButton.interactable = !paused;
		sellButton.interactable = !paused || !VehicleHelper.IsInsideVehicle();
	}

	private void SetUpKeysLabels()
	{
		sellButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Sell.AsSuffix();
		sleepButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Sleep.AsSuffix();
		parkButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.SecondaryInteract.AsSuffix();
		autoParkButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.SpecialInteract.AsSuffix();
	}

	private void UpdateButtonsVisibility()
	{
		if (selectedItemInstance != null && !PlacementSystem.IsInPlacementMode && BuildingManager.CanBuildOnCurrentBuilding)
		{
			Item itemCached = selectedItemInstance.ItemCached;
			if ((object)itemCached != null && itemCached.HasTag(TagRef.Itemtag.isbox))
			{
				placeButtonLabel.Key = "itempanelui_buttons_place_box";
				placeButton.gameObject.SetActive(value: true);
			}
			else
			{
				Item itemCached2 = selectedItemInstance.ItemCached;
				if ((object)itemCached2 != null && itemCached2.HasTag(TagRef.Itemtag.isbag) && selectedItemInstance.cargoInstances.Count > 0)
				{
					placeButtonLabel.Key = "itempanelui_buttons_place";
					placeButton.gameObject.SetActive(value: true);
				}
				else
				{
					placeButton.gameObject.SetActive(value: false);
				}
			}
		}
		else
		{
			placeButton.gameObject.SetActive(value: false);
		}
		ItemInstance itemInstance = PlacementSystem.CurrentPlaceableItemBeingPlaced?.GetItemInstance();
		grabButton.gameObject.SetActive(selectedItemInstance != null && PlacementSystem.IsInPlacementMode && selectedItemInstance.ItemCached.canBeGrabbed && !selectedItemInstance.ItemCached.HasTag(TagRef.Itemtag.isshoppingcontainer));
		sellButton.gameObject.SetActive((VehicleHelper.IsInsideVehicle() && vehicleInfo.isCommonVehicle && VehicleHelper.GetCurrentVehicle().GetSellingPrice() > 0f) || (selectedItemInstance != null && !selectedItemInstance.ItemCached.HasTag(TagRef.Itemtag.cannotsell) && selectedItemInstance.cargoInstances.All((CargoInstance x) => x.paid && !x.IsSealed) && !PlacementSystem.IsInPlacementMode && selectedItemInstance.ItemCached.canBeGrabbed) || (PlacementSystem.IsInPlacementMode && itemInstance != null && !itemInstance.ItemCached.HasTag(TagRef.Itemtag.cannotsell) && itemInstance.cargoInstances.All((CargoInstance x) => x.paid && !x.IsSealed) && itemInstance.ItemCached.canBeGrabbed));
	}

	public void SetItemInstance(ItemInstance itemInstance, bool spawnIntoPlayerHands = true, bool changeOnlyVisibility = false)
	{
		_vehicleController = null;
		selectedItemInstance = itemInstance;
		_isInMiniGame = false;
		cargoContainer.SetActive(value: false);
		UpdateNameLabel();
		parkButton.gameObject.SetActive(value: false);
		sleepButton.gameObject.SetActive(value: false);
		leaveButton.gameObject.SetActive(value: false);
		vehicleInfo.gameObject.SetActive(value: false);
		maintenanceInfo.gameObject.SetActive(value: false);
		autoParkButton.gameObject.SetActive(value: false);
		UpdateButtonsVisibility();
		discardButton.gameObject.SetActive((itemInstance.ItemCached.canBeGrabbed || selectedItemInstance.ItemCached.HasTag(TagRef.Itemtag.ismop)) && !itemInstance.cargoInstances.Any((CargoInstance x) => x.IsSealed));
		_cargoItems.Clear();
		Toggle(isEnabled: true);
		if (itemInstance.cargoInstances.Any((CargoInstance x) => !x.paid))
		{
			SetUnpaidAmount();
		}
		else
		{
			SetMeta(itemInstance.ItemCached.itemPanelMetaView);
		}
		if (spawnIntoPlayerHands)
		{
			if (itemInstance.ItemCached.HasTag(TagRef.Itemtag.ismop))
			{
				PlayerHelper.PlayerController.Character.AddHandObject("Mop");
				MopController componentInChildren = PlayerHelper.PlayerController.Character.rightHand.GetComponentInChildren<MopController>();
				componentInChildren.ItemInstance = itemInstance;
				componentInChildren.AssignToPlayer();
			}
			else
			{
				ItemController itemController = PrefabHelper.CreatePrefabItem(itemInstance.itemName);
				if ((bool)itemController)
				{
					itemController.ItemInstance = itemInstance;
					itemController.TogglePhysics(physicsEnabled: false);
					InstanceBehavior<GameManager>.Instance.playerController.Character.SetHandContent(itemController.transform);
				}
			}
		}
		SetupCargoSlots();
		if (!changeOnlyVisibility)
		{
			GlobalEvents.onItemSelected?.Invoke(itemInstance);
		}
	}

	public void UpdateNameLabel()
	{
		if (selectedItemInstance != null)
		{
			itemNameLabel.SetData(selectedItemInstance.GetLabel());
		}
	}

	public void SetVehicle(VehicleController vehicle)
	{
		selectedItemInstance = null;
		_vehicleController = vehicle;
		_isInMiniGame = false;
		vehicleInfo.Initialize(vehicle);
		itemNameLabel.SetData(LanguageChangeEventDataHolder.Create(vehicle.vehicleInstance.vehicleTypeName));
		SetMeta(ItemPanelMetaView.None);
		parkButton.gameObject.SetActive(value: true);
		autoParkButton.gameObject.SetActive(vehicle.vehicleType.autoParkSupported);
		sleepButton.gameObject.SetActive(vehicleInfo.CanSleep());
		cargoContainer.SetActive(value: false);
		discardButton.gameObject.SetActive(value: false);
		grabButton.gameObject.SetActive(value: false);
		leaveButton.gameObject.SetActive(value: false);
		maintenanceInfo.gameObject.SetActive(value: false);
		placeButton.gameObject.SetActive(value: false);
		_cargoItems.Clear();
		UpdateButtonsVisibility();
		if (vehicle.showCargoInItemPanel)
		{
			SetupCargoSlots();
		}
		SetUnpaidAmount();
		Toggle(isEnabled: true);
	}

	public void SetMiniGameMode(LanguageChangeEventDataHolder label)
	{
		selectedItemInstance = null;
		_vehicleController = null;
		_isInMiniGame = true;
		itemNameLabel.SetData(label);
		SetMeta(ItemPanelMetaView.None);
		vehicleInfo.gameObject.SetActive(value: false);
		parkButton.gameObject.SetActive(value: false);
		autoParkButton.gameObject.SetActive(value: false);
		sleepButton.gameObject.SetActive(value: false);
		cargoContainer.SetActive(value: false);
		discardButton.gameObject.SetActive(value: false);
		grabButton.gameObject.SetActive(value: false);
		leaveButton.gameObject.SetActive(value: true);
		maintenanceInfo.gameObject.SetActive(value: false);
		placeButton.gameObject.SetActive(value: false);
		_cargoItems.Clear();
		UpdateButtonsVisibility();
		Toggle(isEnabled: true);
	}

	private void DiscardItem(ItemInstance instance)
	{
		if (instance.ItemCached.HasTag(TagRef.Itemtag.isshoppingcontainer) && instance.cargoInstances.Count > 0)
		{
			LanguageChangeEventDataHolder bodyData = "hud_confirm_return_items_to_shelf".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				ReturnUnpaidItemsToShelf(instance);
				Discard();
			});
		}
		else
		{
			Discard();
		}
		void Discard()
		{
			ItemController itemControllerByID = ItemHelper.GetItemControllerByID(instance.id);
			if ((bool)itemControllerByID)
			{
				itemControllerByID.RemoveFromParentPlaceableItem(updateWarningIcon: false);
				InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(itemControllerByID);
			}
			instance.Discard();
		}
	}

	private void ReturnUnpaidItemsToShelf(ItemInstance itemInstance)
	{
		foreach (CargoInstance cargoInstance in itemInstance.cargoInstances)
		{
			ReturnUnpaidItemsToShelf(cargoInstance, itemInstance.AddressCached);
		}
	}

	private void ReturnUnpaidItemsToShelf(CargoInstance cargoInstance, Address address)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !cargoInstance.paid)
		{
			cargoInstance.ReturnToAShelf(address);
		}
	}

	private void SetUnpaidAmount()
	{
		float num = 0f;
		if (PlayerHelper.IsUsingVehicle)
		{
			num += VehicleHelper.GetCurrentVehicle().cargoInstances.Where((CargoInstance x) => !x.paid).Sum((CargoInstance x) => (float)x.amount * x.pricePerUnit);
		}
		else if (selectedItemInstance != null)
		{
			num += selectedItemInstance.cargoInstances.Where((CargoInstance x) => !x.paid).Sum((CargoInstance x) => (float)x.amount * x.pricePerUnit);
		}
		if (num > 0f)
		{
			SetMeta(ItemPanelMetaView.Shopping);
			metaInfo.Find("Shopping/UnpaidAmount").GetComponent<TextMeshProUGUI>().text = num.ToShortCurrencyFormat();
		}
		else if (selectedItemInstance != null)
		{
			SetMeta(selectedItemInstance.ItemCached.itemPanelMetaView);
			UpdateButtonsVisibility();
		}
		else if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			SetMeta(ItemPanelMetaView.None);
		}
	}

	public void ClickPark()
	{
		if (panel.gameObject.activeSelf && !PlayerActivityUI.IsPanelOpen && !(InstanceBehavior<GameManager>.Instance.selectedVehicle == null) && !SubwaySystem.IsRiding && parkButton.gameObject.activeSelf && parkButton.interactable && !InstanceBehavior<UIs>.Instance.gameSpeed.Paused && !InstanceBehavior<GameManager>.Instance.playerController.VehicleNavigationDisabled)
		{
			if (VehiclesThatCanNotBeParked.Contains(InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.id))
			{
				Notifications.ShowError("notification_clickpark_cannotpark");
			}
			else
			{
				InstanceBehavior<GameManager>.Instance.selectedVehicle.ExitVehicle();
			}
		}
	}

	public void ClickAutoPark()
	{
		if (!PlayerActivityUI.IsPanelOpen && !InstanceBehavior<GameManager>.Instance.playerController.VehicleNavigationDisabled && (bool)InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			VehicleParkingHelper componentInChildren = InstanceBehavior<GameManager>.Instance.selectedVehicle.GetComponentInChildren<VehicleParkingHelper>();
			if ((bool)componentInChildren)
			{
				StartCoroutine(componentInChildren.ParkInClosestSpot());
			}
		}
	}

	public void ClickPlace()
	{
		ClickPlace(forceClosedCardboardBox: false);
	}

	public void ClickPlace(bool forceClosedCardboardBox)
	{
		if (HudConfirm.isOpen || InstanceBehavior<BuildingManager>.Instance.exitingBuilding)
		{
			return;
		}
		if (selectedItemInstance.ItemCached.HasTag(TagRef.Itemtag.issealedcontainer))
		{
			forceClosedCardboardBox = true;
		}
		bool flag = false;
		string text = selectedItemInstance.itemName;
		Item itemCached = selectedItemInstance.ItemCached;
		if (selectedItemInstance.itemName == "ba:itemname_closedcardboardbox" && !forceClosedCardboardBox)
		{
			CargoInstance cargoInstance = selectedItemInstance.cargoInstances.First();
			if (cargoInstance.ItemCached.isFurniture)
			{
				text = cargoInstance.itemName;
				itemCached = cargoInstance.ItemCached;
				flag = true;
			}
		}
		if (!itemCached.FitsInBuilding())
		{
			Notifications.ShowError("itempanelui_notification_item_too_tall");
			return;
		}
		if (selectedItemInstance.ItemCached.isSeasonalForSale)
		{
			text = selectedItemInstance.ItemCached.GetItemNameBySeason(PlayerPrefSettings.SeasonalDecorations ? SeasonHelper.CurrentSeasonName : SeasonName.None);
			if (string.IsNullOrEmpty(text))
			{
				text = selectedItemInstance.ItemCached.GetItemNameBySeason(SeasonName.None);
				if (string.IsNullOrEmpty(text))
				{
					text = selectedItemInstance.ItemCached.itemsBySeason[0].itemName;
				}
			}
		}
		if (flag)
		{
			CargoInstance cargoInstance2 = selectedItemInstance.cargoInstances.First();
			ItemInstance itemInstance = cargoInstance2.InitializeNewInstance();
			cargoInstance2.ParseIntoItemInstance(itemInstance);
			if (TryToStartPlacingItem(itemCached, text, itemInstance))
			{
				PlayerHelper.ItemInstanceInHands.ReduceFromCargo(cargoInstance2, 1);
			}
		}
		else if (TryToStartPlacingItem(itemCached, text, selectedItemInstance))
		{
			PlayerHelper.ItemInstanceInHands = null;
		}
		InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Close();
	}

	private static bool TryToStartPlacingItem(Item itemToPlace, string itemNameToPlace, ItemInstance itemInstanceToPlace)
	{
		ItemController itemController = PrefabHelper.CreatePrefabItem(itemNameToPlace, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		if (itemController == null)
		{
			return false;
		}
		MouseController.Reset();
		if (itemToPlace.isSeasonalForSale)
		{
			itemController.itemName = itemToPlace.itemName;
			itemController.playerItemPurchaserSettings.enabled = true;
			itemController.playerItemPurchaserSettings.itemName = itemNameToPlace;
		}
		itemController.ItemInstance = itemInstanceToPlace;
		if (InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse())
		{
			itemController.ItemInstance.yRotation = Mathf.RoundToInt(InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.transform.rotation.eulerAngles.y);
		}
		if (itemController.StartPlacementMode(!PlayerAction.SecondaryInteract.Pressed()))
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.AddItemInstanceToBuilding(itemInstanceToPlace);
			return true;
		}
		UnityEngine.Object.Destroy(itemController.gameObject);
		return false;
	}

	public void ClickSleep()
	{
		if (sleepButton.gameObject.activeSelf && sleepButton.interactable && !InstanceBehavior<GameManager>.Instance.playerController.VehicleNavigationDisabled)
		{
			if (BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
			{
				Notifications.ShowError("itempanelui_notification_sleeping_not_in_this_building");
			}
			else
			{
				PlayerActivityUI.Show(_vehicleController.sleepEnvironment, _vehicleController);
			}
		}
	}

	public void ClickGrab()
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is ItemController itemController && itemController.TryToGrabItem())
		{
			PlacementHelper.CancelPlacementMode();
			UnityEngine.Object.Destroy(itemController.gameObject);
		}
	}

	public void ClickDiscard()
	{
		if (PlayerHelper.IsHoldingAMop && CleaningStationController.ReturnMopToStation())
		{
			return;
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			IPlaceableItem currentPlaceableItemBeingPlaced = PlacementSystem.CurrentPlaceableItemBeingPlaced;
			ItemController itemController = currentPlaceableItemBeingPlaced as ItemController;
			if ((object)itemController == null)
			{
				Debug.LogError("Trying to discard a non-ItemController error");
				return;
			}
			if (itemController.childItemControllers.Count != 0)
			{
				Notifications.ShowError("itempanelui_notification_cant_discard_with_attachments");
				return;
			}
			LanguageChangeEventDataHolder bodyData = "hud_confirm_discard_item".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				Item item = ((itemController.itemName == "ba:itemname_closedcardboardbox") ? itemController.ItemInstance.cargoInstances.First().ItemCached : itemController.Item);
				if (item != null && item.isSpecialGift)
				{
					ConfirmDiscardingSpecialGift(delegate
					{
						DiscardPlacementSelectedItem(itemController);
					});
				}
				else
				{
					DiscardPlacementSelectedItem(itemController);
				}
			});
		}
		else
		{
			if (!PlayerHelper.IsHoldingItem)
			{
				return;
			}
			Item itemInHands = PlayerHelper.ItemInHands;
			if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbag) && PlayerHelper.ItemInstanceInHands.cargoInstances.Count == 0)
			{
				PlayerHelper.ItemInstanceInHands = null;
			}
			else if (PlayerHelper.ItemInstanceInHands.cargoInstances.Any((CargoInstance x) => x.paid))
			{
				LanguageChangeEventDataHolder bodyData = "hud_confirm_discard_item".Localize();
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
				{
					if (PlayerHelper.ItemInstanceInHands.ItemCached.isSpecialGift)
					{
						ConfirmDiscardingSpecialGift(delegate
						{
							PlayerHelper.ItemInstanceInHands = null;
						});
					}
					else
					{
						PlayerHelper.ItemInstanceInHands = null;
					}
				});
			}
			else
			{
				PlayerHelper.ItemInstanceInHands = null;
			}
		}
	}

	private void DiscardPlacementSelectedItem(ItemController itemController)
	{
		PlacementHelper.CancelPlacementMode();
		UnityEngine.Object.Destroy(itemController.gameObject);
		DiscardItem(itemController.ItemInstance);
		if (_cargoItems.Count == 0)
		{
			Toggle(isEnabled: false);
		}
	}

	public void ClickLeave()
	{
		if (_isInMiniGame)
		{
			VideoGameSetup.RequestFinish();
			GolfPlatformController.RequestFinish();
			TennisCourt.RequestFinish();
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
			Toggle(isEnabled: false);
		}
	}

	public void ClickPlaceBox()
	{
		if (!HudConfirm.isOpen)
		{
			ClickPlace(forceClosedCardboardBox: true);
		}
	}

	public void ClickSell()
	{
		if (!panel.gameObject.activeSelf || (InstanceBehavior<GameManager>.Instance.selectedVehicle == null && selectedItemInstance == null) || Feedback.IsOpen || PlayerActivityUI.IsPanelOpen || InstanceBehavior<GameManager>.Instance.playerController.VehicleNavigationDisabled)
		{
			return;
		}
		if ((bool)InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			if (VehiclesThatCanNotBeParked.Contains(InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.id))
			{
				Notifications.ShowError("notification_clickpark_cannotpark");
				return;
			}
			VehicleController vehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
			IsSelling = true;
			try
			{
				vehicle.ExitVehicle();
			}
			finally
			{
				IsSelling = false;
			}
			CoroutineUtility.RunAfterFrameDelay(delegate
			{
				if (!vehicle.controlledByPlayer)
				{
					if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
					{
						Notifications.ShowError("itempanelui_notification_cant_sell_vehicle_at_current_position", "itempanelui_notification_cant_sell_vehicle_at_current_position");
					}
					else
					{
						float sellingPrice = vehicle.vehicleInstance.GetSellingPrice();
						LanguageChangeEventDataHolder sellingVehicleConfirmMessage = GetSellingVehicleConfirmMessage(vehicle, sellingPrice);
						HudConfirm.Show(default(LanguageChangeEventDataHolder), sellingVehicleConfirmMessage, delegate
						{
							Dictionary<string, string> data = new Dictionary<string, string> { 
							{
								"itemSoldInfo",
								vehicle.vehicleType.vehicleTypeName
							} };
							TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itemsold", data);
							GameManager.ChangeMoneySafe(sellingPrice, transactionInfo);
							Manager.TriggerColliderRemovedEvent(vehicle.vehicleCollider);
							if (vehicle.additionalCollider != null)
							{
								Manager.TriggerColliderRemovedEvent(vehicle.additionalCollider);
							}
							vehicle.vehicleInstance.Delete(vehicle);
							if (UndergroundParkingManager.IsInsideParking)
							{
								InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.UpdateData();
							}
						}, delegate
						{
							CarController carController = vehicle as CarController;
							if ((bool)carController)
							{
								carController.CheckIfWarehouseSlotShouldBeSet();
							}
						});
					}
				}
			}, 2);
			return;
		}
		ItemInstance itemInstance = (PlacementSystem.IsInPlacementMode ? PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance() : selectedItemInstance);
		if (itemInstance == null)
		{
			return;
		}
		float totalPrice = GetTotalSellingPrice(itemInstance);
		LanguageChangeEventDataHolder bodyData = LanguageChangeEventDataHolder.Create("itempanelui_hud_confirm_sellitem", new
		{
			type = itemInstance.GetLabel(),
			price = totalPrice.ToShortCurrencyFormat()
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			if (itemInstance.ItemCached.isSpecialGift)
			{
				ConfirmDiscardingSpecialGift(delegate
				{
					OnConfirmSellItem(totalPrice);
				});
			}
			else
			{
				OnConfirmSellItem(totalPrice);
			}
		});
	}

	private static float GetTotalSellingPrice(ItemInstance itemInstance)
	{
		float num = itemInstance.GetSellingPrice();
		if (!BuildingManager.IsInsideBuilding)
		{
			return num;
		}
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		foreach (AttachableChild stackedItem in itemInstance.stackedItems)
		{
			if (buildingRegistration.itemInstances.TryGetValue(stackedItem.childId, out var value))
			{
				num += GetTotalSellingPrice(value);
			}
		}
		return num;
	}

	private static LanguageChangeEventDataHolder GetSellingVehicleConfirmMessage(VehicleController vehicle, float sellingPrice)
	{
		return ((vehicle.vehicleInstance.cargoInstances.Count > 0) ? "itempanelui_hud_confirm_sell_vehicle_with_cargo" : "itempanelui_hud_confirm_sellvehicle").Localize(new
		{
			type = vehicle.vehicleType.vehicleTypeName,
			price = sellingPrice.ToShortCurrencyFormat()
		});
	}

	private void OnConfirmSellItem(float price)
	{
		bool isInPlacementMode = PlacementSystem.IsInPlacementMode;
		ItemInstance itemInstance = (isInPlacementMode ? PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance() : selectedItemInstance);
		if (isInPlacementMode)
		{
			PlacementHelper.CancelPlacementMode();
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string> { 
		{
			"itemSoldInfo",
			itemInstance.itemName.GetLocalization()
		} };
		if (itemInstance.cargoInstances.Count == 1 && !isInPlacementMode)
		{
			CargoInstance cargoInstance = itemInstance.cargoInstances.First();
			dictionary["itemSoldInfo"] = cargoInstance.itemName.GetLocalization();
			dictionary["value"] = cargoInstance.amount.ToString();
		}
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itemsold", dictionary);
		GameManager.ChangeMoneySafe(price, transactionInfo);
		DiscardItemWithAttachedItems(itemInstance);
		if (isInPlacementMode && _cargoItems.Count == 0)
		{
			Toggle(isEnabled: false);
		}
	}

	private void DiscardItemWithAttachedItems(ItemInstance itemInstance)
	{
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		for (int num = itemInstance.stackedItems.Count - 1; num >= 0; num--)
		{
			AttachableChild attachableChild = itemInstance.stackedItems[num];
			if (buildingRegistration.itemInstances.TryGetValue(attachableChild.childId, out var value))
			{
				DiscardItemWithAttachedItems(value);
			}
		}
		DiscardItem(itemInstance);
	}

	public static void ConfirmDiscardingSpecialGift(Action onConfirm)
	{
		LanguageChangeEventDataHolder bodyData = LanguageChangeEventDataHolder.Create("itempanelui_hud_confirm_discard_special_gift");
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirm, null, null, null, allowConfirmationSkip: false);
	}

	public void Toggle(bool isEnabled)
	{
		panel.DOKill();
		IsVisible = isEnabled;
		if (isEnabled)
		{
			SetVisibility(visible: true);
			return;
		}
		selectedItemInstance = null;
		InstanceBehavior<GameManager>.Instance.selectedVehicle = null;
		_isInMiniGame = false;
		SetVisibility(visible: false);
	}

	public void SetVisibility(bool visible)
	{
		panel.DOComplete();
		if (visible)
		{
			panel.gameObject.SetActive(value: true);
			panel.DOFade(1f, 0.5f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
			return;
		}
		TweenerCore<float, float, FloatOptions> tweenerCore = panel.DOFade(0f, 0.5f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		tweenerCore.onComplete = (TweenCallback)Delegate.Combine(tweenerCore.onComplete, (TweenCallback)delegate
		{
			panel.gameObject.SetActive(value: false);
		});
	}

	private void SetMeta(ItemPanelMetaView panelMetaView)
	{
		metaInfo.gameObject.SetActive(panelMetaView != ItemPanelMetaView.None);
		foreach (Transform item in metaInfo.transform)
		{
			item.gameObject.SetActive(item.name == panelMetaView.ToStringFast());
		}
		if ((uint)panelMetaView > 2u && panelMetaView == ItemPanelMetaView.Cleaning && PlayerHelper.IsHoldingAMop)
		{
			maintenanceInfo.gameObject.SetActive(value: true);
			RefreshMetaCleanliness();
		}
	}

	public void SetupCargoSlots()
	{
		ICargoHolder vehicleInstance;
		int num;
		bool allowCargoSelection;
		if (_vehicleController != null)
		{
			if (!_vehicleController.showCargoInItemPanel)
			{
				return;
			}
			vehicleInstance = _vehicleController.vehicleInstance;
			num = _vehicleController.vehicleType.maxCargoCapacity;
			allowCargoSelection = true;
		}
		else
		{
			if (selectedItemInstance == null || PlayerHelper.ItemInstanceInHands != selectedItemInstance)
			{
				return;
			}
			vehicleInstance = selectedItemInstance;
			num = selectedItemInstance.ItemCached.cargoCapacity;
			allowCargoSelection = true;
		}
		if (num == 0)
		{
			return;
		}
		cargoContainer.SetActive(value: true);
		cargoSlotTemplate.ResetTemplate();
		_cargoItems = CargoItem.ConvertCargoInstancesToCargoItems(vehicleInstance.GetCargoInstances());
		foreach (CargoItem item in _cargoItems.OrderBy((CargoItem x) => x.itemName))
		{
			Transform obj = UnityEngine.Object.Instantiate(cargoSlotTemplate, cargoSlotTemplate.parent.transform);
			obj.GetComponent<CargoItemUi>().SetUp(item, vehicleInstance, allowCargoSelection);
			obj.gameObject.SetActive(value: true);
		}
		cargoContainer.SetActive(value: true);
		SetUnpaidAmount();
	}

	public static void OnPlaceButtonClick(CargoInstance firstCargoInstance, ICargoHolder cargoHolder)
	{
		if (InstanceBehavior<BuildingManager>.Instance.exitingBuilding)
		{
			return;
		}
		if (!firstCargoInstance.ItemCached.FitsInBuilding())
		{
			Notifications.ShowError("itempanelui_notification_item_too_tall");
			return;
		}
		CargoInstance cargoInstance = new CargoInstance(firstCargoInstance.itemName, 1, firstCargoInstance.pricePerUnit, firstCargoInstance.paid, firstCargoInstance.customColors);
		if (firstCargoInstance.nestedCargoInstances.Count > 0)
		{
			cargoInstance.nestedCargoInstances = firstCargoInstance.nestedCargoInstances;
		}
		ItemInstance itemInstance = cargoInstance.InitializeNewInstance();
		cargoInstance.ParseIntoItemInstance(itemInstance);
		if (TryToStartPlacingItem(cargoInstance.ItemCached, cargoInstance.itemName, itemInstance))
		{
			cargoHolder.ReduceFromCargo(firstCargoInstance, 1);
		}
	}

	public void RefreshMetaCleanliness()
	{
		float maintenanceValue = 100f;
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		if ((object)instance != null && instance.IsPlayerOwnedBusiness)
		{
			maintenanceValue = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetCleanliness();
		}
		SetMaintenanceValue(maintenanceValue);
	}

	private void SetMaintenanceValue(float value)
	{
		maintenanceProgress.SetValue(value);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsSelling = false;
		IsVisible = false;
		VehiclesThatCanNotBeParked.Clear();
	}
}
