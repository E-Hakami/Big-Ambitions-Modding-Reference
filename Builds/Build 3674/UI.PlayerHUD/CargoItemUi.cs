using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using HGAttributes;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using TMPro;
using Tooltip;
using UI.ItemPanel;
using UI.MergeCargo;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;
using Vehicles.VehicleTypes;

namespace UI.PlayerHUD;

public class CargoItemUi : MonoBehaviour
{
	private const string SellConfirmKey = "itempanelui_hud_confirm_sellitem";

	private const string ActionEquipKey = "action_equip";

	private const string ActionReadKey = "action_read";

	private const string ActionPlaceKey = "itempanelui_buttons_place";

	private const string CantLoadHandtruckNotificationKey = "itempanelui_notification_cant_load_handtruck_into_handtruck";

	[SerializeField]
	private TextLocalizationComponent nameLabel;

	[SerializeField]
	private TMP_Text amountLabel;

	[SerializeField]
	private TMP_Text cargoAmount;

	[SerializeField]
	private TMP_Text priceLabel;

	[SerializeField]
	private GameObject cargoBoxIcon;

	[SerializeField]
	private LocalizedListTooltip bundleItemsTooltip;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Button discardButton;

	[SerializeField]
	private Button sellButton;

	[SerializeField]
	private Button itemButton;

	[SerializeField]
	private Button actionButton;

	[SerializeField]
	private TextLocalizationComponent actionButtonLabel;

	[Header("References")]
	[SerializeField]
	private Sprite stackSprite;

	[SerializeField]
	private PlayerActivityBalanceConfig readingBalanceConfig;

	[SerializeField]
	[AutocompleteDropdown("Items")]
	private List<string> contentItemNames = new List<string>();

	private bool _allowCargoSelection;

	private ICargoHolder _currentCargoHolder;

	private readonly List<string> _cargoAmountTagBlacklist = new List<string> { "ba:itemtag_isbag", "ba:itemtag_isshoppingcontainer" };

	public void SetUp(CargoItem cargoItem, ICargoHolder cargoHolder = null, bool allowCargoSelection = false)
	{
		PlayerHUD playerHUD = InstanceBehavior<UIs>.Instance.playerHUD;
		ManageCargoUi manageCargoUI = playerHUD.manageCargoUI;
		_currentCargoHolder = cargoHolder ?? manageCargoUI.currentCargoHolder;
		_allowCargoSelection = allowCargoSelection;
		CargoInstance firstCargoInstance = cargoItem.cargoInstances[0];
		base.gameObject.name = cargoItem.itemName;
		bool paid = firstCargoInstance.paid;
		bool isSealed = firstCargoInstance.IsSealed;
		nameLabel.Key = firstCargoInstance.itemName;
		UpdateCargoAmount(cargoItem.cargoInstances.Count);
		if (firstCargoInstance.nestedCargoInstances.Count > 0)
		{
			amountLabel.gameObject.SetActive(value: false);
			UpdateCargoVisibility(isVisible: false);
			if (isSealed)
			{
				NestedCargoInstance nestedCargoInstance = firstCargoInstance.nestedCargoInstances[0];
				nameLabel.SetData(LocalizationHelper.GetItemLabel(nestedCargoInstance.itemName, nestedCargoInstance.amount));
				bundleItemsTooltip.gameObject.SetActive(value: false);
			}
			else
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>(firstCargoInstance.nestedCargoInstances.Count);
				foreach (NestedCargoInstance nestedCargoInstance3 in firstCargoInstance.nestedCargoInstances)
				{
					if (dictionary.TryGetValue(nestedCargoInstance3.itemName, out var value))
					{
						dictionary[nestedCargoInstance3.itemName] = value + nestedCargoInstance3.amount;
					}
					else
					{
						dictionary[nestedCargoInstance3.itemName] = nestedCargoInstance3.amount;
					}
				}
				if (dictionary.Count == 1 && contentItemNames.Contains(firstCargoInstance.itemName))
				{
					NestedCargoInstance nestedCargoInstance2 = firstCargoInstance.nestedCargoInstances[0];
					int num = dictionary[nestedCargoInstance2.itemName];
					nameLabel.SetData(LocalizationHelper.GetItemLabel(nestedCargoInstance2.itemName));
					amountLabel.text = num.ToString();
					amountLabel.gameObject.SetActive(num > 1);
					UpdateCargoVisibility(num > 1);
					bundleItemsTooltip.gameObject.SetActive(value: false);
				}
				else
				{
					bundleItemsTooltip.list = dictionary.Select((KeyValuePair<string, int> kv) => LocalizationHelper.GetItemLabel(kv.Key, kv.Value).ToString()).ToList();
					bundleItemsTooltip.gameObject.SetActive(value: true);
				}
			}
		}
		else
		{
			int num2 = cargoItem.cargoInstances.SumValues((CargoInstance instance) => instance.amount);
			amountLabel.text = num2.ToString();
			bundleItemsTooltip.gameObject.SetActive(value: false);
			amountLabel.gameObject.SetActive(num2 > 1);
			UpdateCargoVisibility(num2 > 1);
		}
		if (cargoItem.cargoInstances.Count > 1)
		{
			backgroundImage.sprite = stackSprite;
		}
		priceLabel.gameObject.SetActive(!paid);
		float num3 = 0f;
		foreach (CargoInstance cargoInstance in cargoItem.cargoInstances)
		{
			num3 += (float)cargoInstance.amount * cargoInstance.pricePerUnit;
		}
		priceLabel.text = num3.ToShortCurrencyFormat();
		sellButton.onClick.RemoveAllListeners();
		discardButton.onClick.RemoveAllListeners();
		if (isSealed)
		{
			sellButton.gameObject.SetActive(value: false);
			discardButton.gameObject.SetActive(value: false);
		}
		else if (cargoItem.paid)
		{
			sellButton.gameObject.SetActive(cargoItem.IsSellable);
			discardButton.gameObject.SetActive(value: false);
			sellButton.onClick.AddListener(delegate
			{
				OnSellClick(cargoItem, firstCargoInstance);
			});
		}
		else
		{
			discardButton.gameObject.SetActive(value: true);
			sellButton.gameObject.SetActive(value: false);
			discardButton.onClick.AddListener(delegate
			{
				OnDiscardClick(firstCargoInstance);
			});
		}
		if (!paid)
		{
			backgroundImage.color = playerHUD.itemPanelUI.cargoUnpaidSlotColor;
		}
		itemButton.onClick.RemoveAllListeners();
		VehicleInstance vehicle = VehicleHelper.GetCurrentVehicle();
		if (_allowCargoSelection && vehicle != null)
		{
			itemButton.onClick.AddListener(delegate
			{
				PickCargoInstance(firstCargoInstance, vehicle);
			});
		}
		else
		{
			itemButton.onClick.AddListener(delegate
			{
				ClickItem(firstCargoInstance);
			});
		}
		SetupActionButton(firstCargoInstance);
		base.gameObject.SetActive(value: true);
	}

	private void UpdateCargoAmount(int value)
	{
		cargoAmount.text = $"x{value}";
	}

	private void UpdateCargoVisibility(bool isVisible)
	{
		bool flag = false;
		if (_currentCargoHolder is ItemInstance itemInstance)
		{
			foreach (string item in _cargoAmountTagBlacklist)
			{
				if (itemInstance.ItemCached.HasTag(item))
				{
					flag = true;
					break;
				}
			}
		}
		bool active = isVisible && !flag;
		cargoAmount.gameObject.SetActive(active);
		cargoBoxIcon.SetActive(active);
	}

	private void OnSellClick(CargoItem cargoItem, CargoInstance firstCargoInstance)
	{
		float sellPrice = firstCargoInstance.GetSellingPrice() * (float)cargoItem.cargoInstances.Count;
		string type = ((!amountLabel.gameObject.activeSelf || string.IsNullOrEmpty(amountLabel.text)) ? nameLabel.TextContainer.text : ("(" + amountLabel.text + ") " + nameLabel.TextContainer.text));
		LanguageChangeEventDataHolder bodyData = "itempanelui_hud_confirm_sellitem".Localize(new
		{
			type = type,
			price = sellPrice.ToShortCurrencyFormat()
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			if (firstCargoInstance.ItemCached.isSpecialGift)
			{
				ItemPanelUI.ConfirmDiscardingSpecialGift(OnConfirmSell);
			}
			else
			{
				OnConfirmSell();
			}
		});
		void OnConfirmSell()
		{
			foreach (CargoInstance cargoInstance in cargoItem.cargoInstances)
			{
				_currentCargoHolder.RemoveFromCargo(cargoInstance);
			}
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"itemSoldInfo",
				cargoItem.itemName.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itemsold", data);
			GameManager.ChangeMoneySafe(sellPrice, transactionInfo);
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
		}
	}

	private void OnDiscardClick(CargoInstance firstCargoInstance)
	{
		_currentCargoHolder.RemoveFromCargo(firstCargoInstance);
		GameEvent.Invoke("ba:gameevent_itemcargochanged");
	}

	private void SetupActionButton(CargoInstance firstCargoInstance)
	{
		actionButton.onClick.RemoveAllListeners();
		Item cachedItem;
		int num;
		int num2;
		if (_allowCargoSelection)
		{
			if (!firstCargoInstance.paid || firstCargoInstance.IsSealed)
			{
				actionButton.gameObject.SetActive(value: false);
				return;
			}
			cachedItem = firstCargoInstance.ItemCached;
			if (_currentCargoHolder is ItemInstance itemInstance)
			{
				num = (itemInstance.ItemCached.HasTag(TagRef.Itemtag.isbag) ? 1 : 0);
				if (num != 0)
				{
					num2 = (cachedItem.isConsumable ? 1 : 0);
					goto IL_00bb;
				}
			}
			else
			{
				num = 0;
			}
			num2 = 0;
			goto IL_00bb;
		}
		Item itemCached = firstCargoInstance.ItemCached;
		if (firstCargoInstance.paid && itemCached.isFurniture && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && _currentCargoHolder is ItemInstance itemInstance2 && itemInstance2.ItemCached.HasTag(TagRef.Itemtag.isdeliveryspot))
		{
			actionButtonLabel.Key = "itempanelui_buttons_place";
			actionButton.onClick.AddListener(delegate
			{
				ItemPanelUI.OnPlaceButtonClick(firstCargoInstance, _currentCargoHolder);
			});
			actionButton.gameObject.SetActive(value: true);
		}
		else
		{
			actionButton.gameObject.SetActive(value: false);
		}
		return;
		IL_00bb:
		bool flag = (byte)num2 != 0;
		bool flag2 = num != 0 && cachedItem.isAccessory;
		bool flag3 = num != 0 && cachedItem.isReadable;
		bool flag4 = (num != 0 || _currentCargoHolder is ItemInstance { itemName: "ba:itemname_closedcardboardbox" } || (_currentCargoHolder is VehicleInstance vehicleInstance && vehicleInstance.VehicleType.spawnInPlayerObject)) && cachedItem.isFurniture && BuildingManager.CanBuildOnCurrentBuilding;
		bool flag5 = flag | flag2 | flag3 | flag4;
		actionButton.gameObject.SetActive(flag5);
		if (!flag5)
		{
			return;
		}
		if (flag)
		{
			actionButtonLabel.Key = cachedItem.GetConsumeActionLocalizationKey();
			actionButton.onClick.AddListener(delegate
			{
				if (cachedItem.TryToConsume())
				{
					_currentCargoHolder.ReduceFromCargo(firstCargoInstance, 1);
				}
			});
		}
		else if (flag2)
		{
			actionButtonLabel.Key = "action_equip";
			actionButton.onClick.AddListener(delegate
			{
				OnEquipButtonClick(firstCargoInstance, _currentCargoHolder);
			});
		}
		else if (flag3)
		{
			actionButtonLabel.Key = "action_read";
			actionButton.onClick.AddListener(OnReadButtonClick);
		}
		else if (flag4)
		{
			actionButtonLabel.Key = "itempanelui_buttons_place";
			actionButton.onClick.AddListener(delegate
			{
				ItemPanelUI.OnPlaceButtonClick(firstCargoInstance, _currentCargoHolder);
			});
		}
	}

	private static void PickCargoInstance(CargoInstance cargoInstance, VehicleInstance fromHolder)
	{
		if (!InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.isPanelOpen)
		{
			fromHolder.RemoveFromCargo(cargoInstance);
			InstanceBehavior<GameManager>.Instance.selectedVehicle.ExitVehicle();
			InstanceBehavior<GameManager>.Instance.selectedVehicle = null;
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(cargoInstance);
			return;
		}
		ICargoHolder currentCargoHolder = InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.currentCargoHolder;
		if (currentCargoHolder == null)
		{
			Debug.LogError("Trying to move cargo with null to holder: no holder opened in manage cargo");
			return;
		}
		currentCargoHolder.MergeIntoCargo(cargoInstance);
		if (cargoInstance.amount > 0)
		{
			cargoInstance.TryToMoveCargoBetweenHolders(fromHolder, currentCargoHolder);
		}
		else
		{
			fromHolder.RemoveFromCargo(cargoInstance);
		}
	}

	private static void OnEquipButtonClick(CargoInstance firstCargoInstance, ICargoHolder cargoHolder)
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null && firstCargoInstance.ItemCached.accessoryType == AccessoryType.Hand)
		{
			Notifications.ShowError("ba:notification_cannot_do_action_during_activity", null, trackOnSaveGame: false);
			return;
		}
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		cargoHolder.ReduceFromCargo(firstCargoInstance, 1);
		if (PlayerController.HasAccessoryEquipped(firstCargoInstance.ItemCached.accessoryType))
		{
			playerController.UnEquipAccessoryOfType(firstCargoInstance.ItemCached.accessoryType);
		}
		CargoInstance cargoInstance = new CargoInstance(firstCargoInstance.itemName, 1, firstCargoInstance.pricePerUnit, firstCargoInstance.paid, firstCargoInstance.customColors);
		playerController.EquipAccessory(cargoInstance);
	}

	private void OnReadButtonClick()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity != null)
		{
			Notifications.ShowError("ba:notification_cannot_do_action_during_activity", null, trackOnSaveGame: false);
			return;
		}
		PlayerActivityUI.Show(new EntertainDevice
		{
			entertainType = EntertainType.Read,
			balanceConfig = readingBalanceConfig,
			preferSeating = true,
			maxSeatingDistance = 15f
		});
	}

	private void ClickItem(CargoInstance cargoInstance)
	{
		ICargoHolder cargoHolder2;
		if (!PlayerHelper.IsUsingVehicle)
		{
			ICargoHolder cargoHolder = (PlayerHelper.IsHoldingItem ? PlayerHelper.ItemInstanceInHands : null);
			cargoHolder2 = cargoHolder;
		}
		else
		{
			ICargoHolder cargoHolder = VehicleHelper.GetCurrentVehicle();
			cargoHolder2 = cargoHolder;
		}
		ICargoHolder cargoHolder3 = cargoHolder2;
		if (cargoHolder3 == _currentCargoHolder)
		{
			return;
		}
		string vehicleType = ItemsGetter.GetByName(cargoInstance.itemName).vehicleType;
		if (IsDeployableFromCargo(vehicleType))
		{
			if (cargoHolder3 != null)
			{
				if (PlayerHelper.IsUsingVehicle && VehicleHelper.GetCurrentVehicle().VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
				{
					Notifications.ShowError("itempanelui_notification_cant_load_handtruck_into_handtruck");
				}
			}
			else
			{
				VehicleSpawnerController.CreateVehicle(InstanceBehavior<GameManager>.Instance.playerController.transform, cargoInstance.itemName = vehicleType).EnterVehicle();
				_currentCargoHolder.RemoveFromCargo(cargoInstance);
				GameEvent.Invoke("ba:gameevent_itemcargochanged");
			}
			return;
		}
		if (cargoHolder3 == null)
		{
			_currentCargoHolder.RemoveFromCargo(cargoInstance);
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(cargoInstance);
			InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Close();
			return;
		}
		cargoHolder3.MergeIntoCargo(cargoInstance);
		if (cargoInstance.amount > 0)
		{
			if (cargoHolder3.TryToAddToCargo(cargoInstance))
			{
				_currentCargoHolder.RemoveFromCargo(cargoInstance);
			}
		}
		else
		{
			_currentCargoHolder.RemoveFromCargo(cargoInstance);
		}
		GameEvent.Invoke("ba:gameevent_itemcargochanged");
	}

	private static bool IsDeployableFromCargo(string vehicleTypeName)
	{
		if (string.IsNullOrEmpty(vehicleTypeName))
		{
			return false;
		}
		VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(vehicleTypeName);
		if (vehicleType != null)
		{
			return vehicleType.spawnInPlayerObject;
		}
		return false;
	}
}
