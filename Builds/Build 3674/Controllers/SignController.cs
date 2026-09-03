using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using UI.Elements;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Controllers;

public class SignController : ItemController
{
	[SerializeField]
	private TextLocalizationComponent[] infoLabels;

	[SerializeField]
	private TextLocalizationComponent[] priceLabels;

	private bool _isVehicleInfo;

	private string _vehicleTypeName;

	private EntityController _linkedEntityController;

	private void OnEnable()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationUpdate));
		if (initialized)
		{
			SetInfo();
		}
		else
		{
			onItemInitialized.AddListener(SetInfo);
		}
	}

	private void OnDisable()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationUpdate));
	}

	public override int GetSelectedStockIndex()
	{
		if (string.IsNullOrEmpty(base.ItemInstance.linkedItemName))
		{
			return StockTypes.IndexOf("ba:itemname_undefined");
		}
		int num = StockTypes.IndexOf(base.ItemInstance.linkedItemName);
		if (num == -1)
		{
			return StockTypes.IndexOf("ba:itemname_undefined");
		}
		return num;
	}

	public override void OnIoEnter()
	{
		if (BuildingManager.IsInsideBuilding && base.BuildingContext.IsPlayerOwnedBusiness)
		{
			base.OnIoEnter();
			return;
		}
		_linkedEntityController = ((!string.IsNullOrEmpty(base.ItemInstance.linkedItemName)) ? InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(base.ItemInstance.linkedItemName, base.transform.position) : null);
		InstanceBehavior<OverlayManager>.Instance.ShowSimpleOverlay((_linkedEntityController != null) ? _linkedEntityController : this);
		HandleHighlight();
	}

	public override bool OnIoLeftClick()
	{
		if (string.IsNullOrEmpty(base.ItemInstance.linkedItemName) || (BuildingManager.IsInsideBuilding && base.BuildingContext.IsPlayerOwnedBusiness))
		{
			return base.OnIoLeftClick();
		}
		_linkedEntityController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(base.ItemInstance.linkedItemName, base.transform.position);
		return _linkedEntityController.OnIoLeftClick();
	}

	public override void OnIoExit()
	{
		if (_linkedEntityController != null && InstanceBehavior<OverlayManager>.Instance != null && InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(_linkedEntityController))
		{
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		}
		base.OnIoExit();
	}

	public override void OnStockOptionSelected(int stockIndex, Dropdown dropdown = null)
	{
		if (base.BuildingContext.IsPlayerOwnedBusiness && stockIndex >= 0 && stockIndex < StockTypes.Count)
		{
			base.ItemInstance.linkedItemName = ((StockTypes[stockIndex] == "ba:itemname_undefined") ? string.Empty : StockTypes[stockIndex]);
			SetInfo();
		}
	}

	protected override void UpdateStockTypes()
	{
		if (stockTypes == null)
		{
			stockTypes = new List<string>();
		}
		else
		{
			stockTypes.Clear();
			stockTypes.Add("ba:itemname_undefined");
		}
		List<string> list = ((!base.BuildingContext.Registration.HasValidAddress) ? (GameManager.IsDevMode ? ItemsGetter.AllItems.Select((Item x) => x.itemName).ToList() : ItemHelper.GetRetailProductItemNames()) : base.BuildingContext.Registration.cachedAvailableProducts);
		if (list != null && list.Count > 0)
		{
			stockTypes.AddRange(list);
		}
	}

	private void SetInfo()
	{
		if (!string.IsNullOrEmpty(customValue))
		{
			_isVehicleInfo = VehicleTypeHelper.IsValidVehicleType(customValue);
			if (!_isVehicleInfo)
			{
				UpdateLabels(customValue);
				return;
			}
			_vehicleTypeName = customValue;
		}
		if (base.ItemInstance != null)
		{
			ShowItemInfo((StockTypes == null) ? string.Empty : base.ItemInstance.linkedItemName);
		}
	}

	private void ShowItemInfo(string itemToShow)
	{
		(LanguageChangeEventDataHolder, int, float) itemInfo = GetItemInfo(itemToShow);
		UpdateLabels(itemInfo);
	}

	private (LanguageChangeEventDataHolder label, int amount, float price) GetItemInfo(string itemToShow)
	{
		if ((_isVehicleInfo && TryGetVehicleType(_vehicleTypeName, out var vehicleType)) || TryGetVehicleType(itemToShow, out vehicleType))
		{
			return GetVehicleInfo(vehicleType);
		}
		(string, int, float) tuple = (base.BuildingContext.Registration.RentedByPlayer ? GetPlayerItemInfo(itemToShow) : GetAIItemInfo(itemToShow));
		return (label: LocalizationHelper.GetItemLabel(tuple.Item1), amount: tuple.Item2, price: tuple.Item3);
	}

	private static (LanguageChangeEventDataHolder label, int amount, float price) GetVehicleInfo(VehicleType vehicleType)
	{
		return (label: new LanguageChangeEventDataHolder
		{
			Key = vehicleType.vehicleTypeName
		}, amount: 1, price: vehicleType.price);
	}

	private static bool TryGetVehicleType(string itemToShow, out VehicleType vehicleType)
	{
		vehicleType = null;
		if (string.IsNullOrEmpty(itemToShow))
		{
			return false;
		}
		vehicleType = VehicleTypeHelper.GetVehicleType(itemToShow);
		if (vehicleType != null)
		{
			return true;
		}
		Item byName = ItemsGetter.GetByName(itemToShow);
		if (string.IsNullOrEmpty(byName?.vehicleType))
		{
			return false;
		}
		vehicleType = VehicleTypeHelper.GetVehicleType(byName.vehicleType);
		return vehicleType != null;
	}

	private void UpdateLabels(string key)
	{
		string[] array = key.Split(',');
		LanguageChangeEventDataHolder data = new LanguageChangeEventDataHolder
		{
			Key = array[0]
		};
		LanguageChangeEventDataHolder data2 = ((array.Length > 1) ? new LanguageChangeEventDataHolder
		{
			Key = array[1]
		} : new LanguageChangeEventDataHolder
		{
			Key = ""
		});
		TextLocalizationComponent[] array2 = infoLabels;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SetData(data);
		}
		array2 = priceLabels;
		foreach (TextLocalizationComponent textLocalizationComponent in array2)
		{
			if (data2.Key == "")
			{
				textLocalizationComponent.gameObject.SetActive(value: false);
				continue;
			}
			textLocalizationComponent.SetData(data2);
			textLocalizationComponent.gameObject.SetActive(value: true);
		}
	}

	private void UpdateLabels((LanguageChangeEventDataHolder label, int amount, float price) itemInfo)
	{
		TextLocalizationComponent[] array = infoLabels;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetData(itemInfo.label);
		}
		array = priceLabels;
		foreach (TextLocalizationComponent textLocalizationComponent in array)
		{
			if (itemInfo.amount == 1)
			{
				textLocalizationComponent.SetData("common_value".Localize(new
				{
					value = itemInfo.price.ToCurrencyFormat()
				}));
			}
			else
			{
				textLocalizationComponent.SetData("pallet_shelf_price_label".Localize(new
				{
					totalPrice = itemInfo.price.ToShortCurrencyFormat(),
					quantity = itemInfo.amount
				}));
			}
		}
	}

	private (string itemName, int amount, float price) GetPlayerItemInfo(string itemToShow)
	{
		if (string.IsNullOrEmpty(itemToShow))
		{
			return (itemName: itemToShow, amount: 1, price: 0f);
		}
		float price = ItemHelper.GetPrice(itemToShow, base.BuildingContext.Registration);
		return (itemName: itemToShow, amount: 1, price: price);
	}

	private (string itemName, int amount, float price) GetAIItemInfo(string itemToShow)
	{
		if (string.IsNullOrEmpty(itemToShow))
		{
			return (itemName: itemToShow, amount: 1, price: 0f);
		}
		Item byName = ItemsGetter.GetByName(itemToShow);
		if (byName == null)
		{
			return (itemName: string.Empty, amount: 1, price: 0f);
		}
		int priceIndex = base.BuildingContext.Registration.GetPriceIndex();
		float num = ((base.BuildingContext.Registration.businessTypeName == "ba:businesstype_wholesalestore") ? (byName.GetWholesalePrice() * ProductMarketHelper.GetProductMarketEventMultiplier(byName.itemName, base.BuildingContext.Registration.Neighborhood)) : ItemHelper.GetPriceOnCurrentBusiness(itemToShow)) * ((float)priceIndex / 100f);
		int num2 = ((!playerItemPurchaserSettings.isQuantityItem) ? 1 : byName.boxSize);
		float item = num * (float)num2;
		return (itemName: itemToShow, amount: num2, price: item);
	}

	private void OnBuildingRegistrationUpdate(Address address)
	{
		if (base.ItemInstance != null && !(base.ItemInstance.AddressCached != address))
		{
			UpdateStockTypes();
			SetInfo();
		}
	}

	[ConsoleMethod("SetSignInfo", "Sets the localization key to display on the sign in hand", new string[] { })]
	public static void Command_SetSignInfo(string localizationKey, string priceLocalizationKey)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SignController signController)
		{
			signController.customValue = localizationKey + "," + priceLocalizationKey;
			Debug.Log("Sign " + signController.itemName + " will now display " + localizationKey + ", " + priceLocalizationKey + "!");
			signController.SetInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an SignController. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetSignInfo", "Sets the localization key to display on the sign in hand", new string[] { })]
	public static void Command_SetSignInfo(string localizationKey)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SignController signController)
		{
			signController.customValue = localizationKey;
			Debug.Log("Sign " + signController.itemName + " will now display " + localizationKey + "!");
			signController.SetInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an SignController. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetSignInfoAsSingleItem", "Sets the item name to display on the sign in hand as a single item", new string[] { }, AutoCompleteMap = new string[] { "signItemName=Items" })]
	public static void Command_SetSignInfoAsSingleItem(string signItemName)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SignController signController)
		{
			signController.playerItemPurchaserSettings.itemName = signItemName;
			signController.playerItemPurchaserSettings.enabled = false;
			signController.playerItemPurchaserSettings.itemQuantity = 1;
			Debug.Log("Item " + signController.itemName + " will now display 1x " + signItemName + " on sale!");
			signController.SetInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an SignController. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetSignInfoAsWholeSale", "Sets the item name to display on the sign in hand as whole sale item", new string[] { }, AutoCompleteMap = new string[] { "signItemName=Items" })]
	public static void Command_SetSignInfoAsWholeSale(string signItemName)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SignController signController)
		{
			signController.playerItemPurchaserSettings.itemName = signItemName;
			signController.playerItemPurchaserSettings.enabled = false;
			signController.playerItemPurchaserSettings.itemQuantity = 99;
			Item byName = ItemsGetter.GetByName(signItemName);
			Debug.Log($"Item {signController.itemName} will now display {byName.boxSize}x {signItemName} on sale!");
			signController.SetInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an SignController. Ensure you have one in hand and are in placement mode");
		}
	}

	[ConsoleMethod("SetSignInfoVehicle", "Sets the vehicle to display with the price on the sign in hand", new string[] { })]
	public static void Command_SetSignInfoVehicle(string vehicleTypeName)
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is SignController signController)
		{
			signController.customValue = vehicleTypeName;
			Debug.Log("Sign " + signController.itemName + " will now display " + vehicleTypeName + " on sale!");
			signController.SetInfo();
		}
		else
		{
			Debug.LogError("Couldn't find an SignController. Ensure you have one in hand and are in placement mode");
		}
	}
}
