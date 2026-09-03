using System.Collections.Generic;
using BigAmbitions.Items;
using Blueprints;
using BusinessLayoutSets;
using Controllers;
using Dialogs;
using Extensions;
using Helpers;
using Services;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;
using Vehicles;
using Vehicles.VehicleTypes;

namespace Buildings;

public class VehicleContractSettings : MonoBehaviour
{
	public static bool disableDeliveryOnNextInit;

	[SerializeField]
	private UI.Elements.Dropdown vehicleDropdown;

	[SerializeField]
	private UI.Elements.Dropdown warehouseDropdown;

	[SerializeField]
	private Toggle deliveryToggle;

	[SerializeField]
	private Transform colorTemplate;

	[SerializeField]
	private TextMeshProUGUI deliveryPriceLabel;

	[SerializeField]
	private TextMeshProUGUI vehiclePriceLabel;

	[SerializeField]
	private TextMeshProUGUI totalPriceLabel;

	[SerializeField]
	private GameObject deliveryArea;

	[HideInInspector]
	public ShowcaseVehicleController selectedVehicle;

	[HideInInspector]
	public Address selectedAddress;

	[HideInInspector]
	public bool isDelivery;

	private readonly List<ContractVehicleForSale> _vehicles = new List<ContractVehicleForSale>();

	private readonly HashSet<string> _vehiclesAdded = new HashSet<string>();

	private readonly List<string> _vehiclesDropdownOptions = new List<string>();

	private readonly List<string> _warehouseDropdownOptions = new List<string>();

	private readonly List<BuildingRegistration> _warehouses = new List<BuildingRegistration>();

	private List<(string name, Color32 color)> _colors;

	private float _deliveryPrice;

	private GameObject _selectedColorOutline;

	private float _totalPrice;

	private float _vehiclePrice;

	private VehicleStoreSettings _vehicleStoreSettings;

	[HideInInspector]
	public ContractVehicleForSale selectedVehicleForSale;

	private void Start()
	{
		bool num = disableDeliveryOnNextInit;
		disableDeliveryOnNextInit = false;
		_vehicleStoreSettings = VehicleDeliveryHelper.GetVehicleStoreSettings(DialogController.current.contact.Address);
		SetVehicleDropdown();
		if (num)
		{
			SetDeliveryUiHidden();
		}
		else
		{
			SetWarehouseDropdown();
			SetDeliveryToggle();
		}
		UpdatePrices();
	}

	private void SetDeliveryUiHidden()
	{
		isDelivery = false;
		selectedAddress = null;
		if (deliveryArea != null)
		{
			deliveryArea.SetActive(value: false);
		}
		if (deliveryToggle != null)
		{
			deliveryToggle.gameObject.SetActive(value: false);
		}
		if (warehouseDropdown != null)
		{
			warehouseDropdown.gameObject.SetActive(value: false);
		}
	}

	private void SetDeliveryToggle()
	{
		deliveryToggle.gameObject.SetActive(DialogController.current.dialogType == DialogType.Physical);
		deliveryToggle.onValueChanged.AddListener(OnDeliveryValueChanged);
		deliveryToggle.isOn = DialogController.current.dialogType == DialogType.PhoneCall;
	}

	public void DisableDeliveryOptions()
	{
		isDelivery = false;
		selectedAddress = null;
		if (deliveryArea != null)
		{
			deliveryArea.SetActive(value: false);
		}
		if (deliveryToggle != null)
		{
			deliveryToggle.isOn = false;
			deliveryToggle.interactable = false;
			deliveryToggle.gameObject.SetActive(value: false);
		}
		if (warehouseDropdown != null)
		{
			warehouseDropdown.gameObject.SetActive(value: false);
		}
		UpdatePrices();
		ForceRefreshUi();
	}

	private void ForceRefreshUi()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (rectTransform != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
		}
		RectTransform rectTransform2 = ((rectTransform != null) ? (rectTransform.parent as RectTransform) : null);
		if (rectTransform2 != null)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform2);
		}
		Canvas.ForceUpdateCanvases();
	}

	private void SetVehicleDropdown()
	{
		SetListOfVehiclesForSale();
		_vehiclesDropdownOptions.Clear();
		foreach (ContractVehicleForSale vehicle in _vehicles)
		{
			_vehiclesDropdownOptions.Add(vehicle.VehicleName);
		}
		vehicleDropdown.SetOptions(_vehiclesDropdownOptions);
		vehicleDropdown.onOptionSelected.AddListener(OnVehicleSelected);
		if (_vehicles.Count <= 0)
		{
			selectedVehicle = null;
			selectedVehicleForSale = null;
		}
		else
		{
			vehicleDropdown.SelectOption(0);
		}
	}

	private void SetWarehouseDropdown()
	{
		_warehouses.AddRange(VehicleDeliveryHelper.GetAvailableWarehousesToDeliver());
		foreach (BuildingRegistration warehouse in _warehouses)
		{
			_warehouseDropdownOptions.Add(warehouse.GetDisplayName());
		}
		warehouseDropdown.SetOptions(_warehouseDropdownOptions, localize: false);
		warehouseDropdown.SetPlaceholder("dialog_select_address");
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

	private void OnWarehouseSelected(int index)
	{
		if (isDelivery || (!(deliveryToggle == null) && deliveryToggle.interactable))
		{
			selectedAddress = _warehouses[index].Address;
			deliveryToggle.isOn = true;
			UpdatePrices();
		}
	}

	private void OnVehicleSelected(int index)
	{
		if (index >= 0 && index < _vehicles.Count)
		{
			selectedVehicleForSale = _vehicles[index];
			selectedVehicle = selectedVehicleForSale.ShowcaseVehicleController;
			UpdatePrices();
			SetUpColorsList();
		}
	}

	private void OnDeliveryValueChanged(bool isOn)
	{
		if (!isOn)
		{
			isDelivery = false;
			selectedAddress = null;
			UpdatePrices();
		}
		else if (_warehouseDropdownOptions.Count <= 0)
		{
			Notifications.ShowError("purchasevehicleui_no_warehouses_available");
			deliveryToggle.isOn = false;
		}
		else
		{
			isDelivery = true;
			UpdatePrices();
		}
	}

	private void UpdatePrices()
	{
		if (selectedVehicleForSale == null)
		{
			_vehiclePrice = 0f;
			_deliveryPrice = 0f;
			_totalPrice = 0f;
			vehiclePriceLabel.SetText(_vehiclePrice.ToShortCurrencyFormat());
			deliveryPriceLabel.SetText(_deliveryPrice.ToShortCurrencyFormat());
			totalPriceLabel.SetText(_totalPrice.ToShortCurrencyFormat());
		}
		else
		{
			_vehiclePrice = VehicleTypeHelper.GetVehicleType(selectedVehicleForSale.VehicleName).price;
			_deliveryPrice = ((isDelivery && selectedAddress != null && _vehicleStoreSettings != null) ? _vehicleStoreSettings.deliveryPrice : 0f);
			_totalPrice = _vehiclePrice + _deliveryPrice;
			vehiclePriceLabel.SetText(_vehiclePrice.ToShortCurrencyFormat());
			deliveryPriceLabel.SetText(_deliveryPrice.ToShortCurrencyFormat());
			totalPriceLabel.SetText(_totalPrice.ToShortCurrencyFormat());
		}
	}

	private void SetUpColorsList()
	{
		if (selectedVehicleForSale != null)
		{
			colorTemplate.ResetTemplate();
			_colors = selectedVehicleForSale.GetColors();
			for (int i = 0; i < _colors.Count; i++)
			{
				(string, Color32) color = _colors[i];
				SetUpColor(color, i == 0);
			}
		}
	}

	private void SetUpColor((string name, Color32 color) color, bool select)
	{
		Transform entry = Object.Instantiate(colorTemplate, colorTemplate.parent);
		entry.name = color.name;
		entry.GetComponent<Image>().color = color.color;
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			SelectColor(color.name, entry);
		});
		if (select)
		{
			_selectedColorOutline = entry.Find("Selected").gameObject;
			_selectedColorOutline.SetActive(value: true);
		}
		entry.gameObject.SetActive(value: true);
	}

	private void SelectColor(string colorName, Transform entry)
	{
		if (_selectedColorOutline != null)
		{
			_selectedColorOutline.SetActive(value: false);
		}
		_selectedColorOutline = entry.Find("Selected").gameObject;
		_selectedColorOutline.SetActive(value: true);
		selectedVehicleForSale?.SetColor(colorName, updateVisuals: false);
	}

	private void SetListOfVehiclesForSale()
	{
		_vehicles.Clear();
		_vehiclesAdded.Clear();
		Address address = DialogController.current.contact.Address;
		string id = DialogController.current.contact.id;
		if (TryAddVehiclesFromService(id, address) || address == null)
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration == null || string.IsNullOrEmpty(buildingRegistration.businessTypeName) || string.IsNullOrEmpty(buildingRegistration.Layout))
		{
			return;
		}
		BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(buildingRegistration.businessTypeName, new BuildingSizeInfo(buildingRegistration), buildingRegistration.Layout, warnIfNotFound: false);
		if (orLoadBusinessLayoutSet?.Items == null)
		{
			return;
		}
		foreach (BusinessLayoutSets.Item item in orLoadBusinessLayoutSet.Items)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = item.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled)
			{
				BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.playerItemPurchaserSettings.itemName);
				ItemController itemController = PrefabHelper.LoadItemControllerFromPrefab(byName.itemName);
				TryAddVehicleByVehicleType(byName.vehicleType, itemController as ShowcaseVehicleController);
			}
		}
	}

	private bool TryAddVehiclesFromService(string contactId, Address address)
	{
		List<string> list;
		if (ContractItemsForSaleService.TryGetVehiclesForContact(contactId, out var vehicleNames))
		{
			list = vehicleNames;
		}
		else
		{
			if (!(address != null) || !ContractItemsForSaleService.TryGetVehiclesForAddress(address, out var vehicleNames2))
			{
				return false;
			}
			list = vehicleNames2;
		}
		foreach (string item in list)
		{
			TryAddVehicleByVehicleType(item);
		}
		return _vehicles.Count > 0;
	}

	private void TryAddVehicleByVehicleType(string vehicleTypeName, ShowcaseVehicleController showcaseVehicle = null)
	{
		VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(vehicleTypeName);
		if (!(vehicleType == null) && !_vehiclesAdded.Contains(vehicleType.vehicleTypeName))
		{
			_vehicles.Add(new ContractVehicleForSale(vehicleType, showcaseVehicle));
			_vehiclesAdded.Add(vehicleType.vehicleTypeName);
		}
	}
}
