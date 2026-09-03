using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using Buildings;
using Entities;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Vehicles;

namespace UI.PurchaseVehicle;

public class PurchaseVehicleUI : MonoBehaviour
{
	private const float MinColorCellSize = 80f;

	private const float MaxColorCellSize = 130f;

	private const int MinCellNumber = 4;

	private const int MaxColorCellNumber = 12;

	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private TextLocalizationComponent assetName;

	[SerializeField]
	private TextMeshProUGUI assetPrice;

	[SerializeField]
	private Transform specTemplate;

	[SerializeField]
	private Transform colorTemplate;

	[SerializeField]
	private GridLayoutGroup colorsGridLayoutGroup;

	[SerializeField]
	private Button cancelButton;

	[SerializeField]
	private Button purchaseButton;

	[SerializeField]
	private GameObject deliveryPart;

	[SerializeField]
	private TextMeshProUGUI deliveryPrice;

	[SerializeField]
	private Toggle deliveryToggle;

	[SerializeField]
	private UI.Elements.Dropdown warehouseDropdown;

	private List<(string name, Color32 color)> _colors;

	private IPurchasableAsset _purchasableAsset;

	private string _initialAssetColor;

	private GameObject _selectedColorOutline;

	private int _selectedColorIndex = -1;

	private VehicleStoreSettings _vehicleStoreSettings;

	public static readonly UnityEvent<bool> OutdoorCameraToggled = new UnityEvent<bool>();

	public static bool runningShowcaseAnimation;

	public static bool runningCancelShowcaseAnimation;

	private readonly List<string> _warehouseDropdownOptions = new List<string>();

	private readonly List<BuildingRegistration> _warehouses = new List<BuildingRegistration>();

	private Coroutine _purchaseAnimationCoroutine;

	public static bool IsShowcaseAnimationRunning
	{
		get
		{
			if (!runningShowcaseAnimation)
			{
				return runningCancelShowcaseAnimation;
			}
			return true;
		}
	}

	public static bool IsPanelOpen { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsPanelOpen = false;
		runningShowcaseAnimation = false;
		runningCancelShowcaseAnimation = false;
	}

	private void Start()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (IsPanelOpen)
			{
				ChangeVisibility(!isOpen);
			}
		});
		GlobalEvents.RegisterOnGameLoadedCallback(SetUpKeysLabels);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		deliveryToggle.onValueChanged.AddListener(OnDeliveryToggleChanged);
		warehouseDropdown.onOptionSelected.AddListener(OnWarehouseSelected);
		warehouseDropdown.onMiddleBoxClicked.AddListener(OnWarehouseDropdownMiddleBoxClicked);
	}

	private void OnWarehouseDropdownMiddleBoxClicked()
	{
		if (_warehouseDropdownOptions.Count <= 0)
		{
			Notifications.ShowError("purchasevehicleui_no_warehouses_available");
		}
	}

	private void OnWarehouseSelected(int _)
	{
		deliveryToggle.isOn = true;
	}

	private void SetUpKeysLabels()
	{
		cancelButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Cancel.AsSuffix();
		purchaseButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Interact.AsSuffix();
	}

	private void OnDeliveryToggleChanged(bool isDelivery)
	{
		if (IsPanelOpen)
		{
			if (isDelivery && _warehouseDropdownOptions.Count <= 0)
			{
				Notifications.ShowError("purchasevehicleui_no_warehouses_available");
				deliveryToggle.isOn = false;
			}
			else
			{
				SetPurchaseButtonText(isDelivery);
				SetAssetPrice(isDelivery);
			}
		}
	}

	private void SetPurchaseButtonText(bool isDelivery)
	{
		purchaseButton.transform.GetLanguageChangeEventByName("Label").Key = (isDelivery ? "furniture_delivery_dialog_order" : "common_purchase");
	}

	private void SetAssetPrice(bool isDelivery)
	{
		float num = _purchasableAsset.GetPurchasePrice();
		if (isDelivery)
		{
			num += _vehicleStoreSettings.deliveryPrice;
		}
		assetPrice.text = num.ToShortCurrencyFormat();
	}

	private void SelectColor(string colorName, Transform entry)
	{
		if (_selectedColorOutline != null)
		{
			_selectedColorOutline.SetActive(value: false);
		}
		_selectedColorOutline = entry.Find("Selected").gameObject;
		_selectedColorOutline.SetActive(value: true);
		_purchasableAsset.SetColor(colorName);
	}

	public void Purchase()
	{
		if (_purchasableAsset == null || runningShowcaseAnimation)
		{
			return;
		}
		if (deliveryToggle.isOn)
		{
			if (warehouseDropdown.SelectedOptionIndex < 0)
			{
				Notifications.ShowError("purchasevehicleui_no_warehouse_selected_error");
				return;
			}
			Address address = _warehouses[warehouseDropdown.SelectedOptionIndex].Address;
			Contact contact = Contact.GetContact(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, ContactCategoryName.FurnitureAndEquipment);
			_purchasableAsset.Order(address, contact, showNotification: true);
			Close();
		}
		else if (_purchasableAsset.Purchase())
		{
			RunShowcaseAnimation();
			Close();
		}
	}

	public void RunShowcaseAnimation()
	{
		_purchaseAnimationCoroutine = CoroutineUtility.Run(_purchasableAsset.ShowcaseAnimation());
	}

	public void CancelShowcaseAnimation()
	{
		runningShowcaseAnimation = false;
		runningCancelShowcaseAnimation = true;
		CoroutineUtility.StopRunning(_purchaseAnimationCoroutine);
		StartCoroutine(_purchasableAsset.CancelShowcaseAnimation());
	}

	public void Close()
	{
		if (!runningShowcaseAnimation && !runningCancelShowcaseAnimation)
		{
			ChangeVisibility(visible: false);
			InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PurchaseVehicleUI);
			if (_purchasableAsset != null)
			{
				_purchasableAsset.ResetColor();
				_purchasableAsset = null;
			}
			IsPanelOpen = false;
		}
	}

	public void SetAsset(IPurchasableAsset purchasableAsset, bool initUi = true)
	{
		_purchasableAsset = purchasableAsset;
		if (!initUi)
		{
			return;
		}
		IsPanelOpen = true;
		_vehicleStoreSettings = null;
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		if (buildingRegistration != null)
		{
			_vehicleStoreSettings = VehicleDeliveryHelper.GetVehicleStoreSettings(buildingRegistration.Address);
		}
		_colors = purchasableAsset.GetColors();
		SetUpColorsList();
		SetColorsCellSize();
		_selectedColorIndex = _colors.FindIndex(((string name, Color32 color) x) => x.name == purchasableAsset.GetInitialColor());
		assetName.Key = _purchasableAsset.GetLocalizeKey();
		SetAssetPrice(isDelivery: false);
		specTemplate.ResetTemplate();
		foreach (var spec in purchasableAsset.GetSpecs())
		{
			Transform obj = UnityEngine.Object.Instantiate(specTemplate, specTemplate.parent);
			obj.GetLanguageChangeEventByName("Label").Key = spec.key;
			obj.GetLabelByName("Value").text = spec.value;
			obj.gameObject.SetActive(value: true);
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.PurchaseVehicleUI);
		ChangeVisibility(visible: true);
		InitDeliveryOptions();
	}

	private void SetColorsCellSize()
	{
		float num = ((_colors.Count <= 4) ? 130f : ((_colors.Count < 12) ? Mathf.Lerp(130f, 80f, (float)(_colors.Count - 4) / 8f) : 80f));
		colorsGridLayoutGroup.cellSize = new Vector2(num, num);
	}

	private void InitDeliveryOptions()
	{
		bool flag = _vehicleStoreSettings != null && _vehicleStoreSettings.canDeliver;
		deliveryPart.SetActive(flag);
		SetAssignedWarehouseDropdown();
		deliveryToggle.isOn = false;
		if (flag)
		{
			deliveryPrice.text = "purchasevehicleui_delivertowarehouse".Localize(new
			{
				amount = _vehicleStoreSettings.deliveryPrice.ToShortCurrencyFormat()
			}).ToString();
		}
	}

	private void SetAssignedWarehouseDropdown()
	{
		_warehouses.Clear();
		_warehouses.AddRange(VehicleDeliveryHelper.GetAvailableWarehousesToDeliver());
		_warehouseDropdownOptions.Clear();
		_warehouseDropdownOptions.AddRange(_warehouses.Select((BuildingRegistration x) => x.GetDisplayName()));
		warehouseDropdown.SetOptions(_warehouseDropdownOptions, localize: false);
		warehouseDropdown.SetPlaceholder("dialog_select_address");
	}

	private void SetUpColorsList()
	{
		colorTemplate.ResetTemplate();
		foreach (var color in _colors)
		{
			SetUpColor(color);
		}
	}

	private void SetUpColor((string name, Color32 color) color)
	{
		Transform entry = UnityEngine.Object.Instantiate(colorTemplate, colorTemplate.parent);
		entry.name = color.name;
		entry.GetComponent<Image>().color = color.color;
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectColor(color.name, entry);
		});
		if (color.name == _purchasableAsset.GetInitialColor())
		{
			_selectedColorOutline = entry.Find("Selected").gameObject;
			_selectedColorOutline.SetActive(value: true);
		}
		entry.gameObject.SetActive(value: true);
	}

	private void ChangeVisibility(bool visible)
	{
		panel.SetActive(visible);
		if (visible)
		{
			cancelButton.interactable = true;
			BasicTooltip component = purchaseButton.GetComponent<BasicTooltip>();
			purchaseButton.interactable = true;
			component.enabled = false;
		}
	}
}
