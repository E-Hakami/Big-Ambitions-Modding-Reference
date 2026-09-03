using System.Collections.Generic;
using System.Linq;
using Buildings;
using Buildings.BuildingTypes.Special.MovingCompany;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class MovingServiceContractSettings : MonoBehaviour
{
	private const int InitialMovingDay = 1;

	private const int LastMovingDay = 7;

	[SerializeField]
	private UI.Elements.Dropdown selectedOriginAddressDropdown;

	[SerializeField]
	private UI.Elements.Dropdown selectedDestinationAddressDropdown;

	[SerializeField]
	private UI.Elements.Dropdown selectedDayDropdown;

	[SerializeField]
	private Toggle transferBizManSettingsToggle;

	[SerializeField]
	private TMP_Text movingFeeValue;

	[SerializeField]
	private TMP_Text feePerItemValue;

	[SerializeField]
	private TMP_Text totalCostValue;

	public Address selectedOriginAddress;

	public Address selectedDestinationAddress;

	public int selectedDay = -1;

	public int selectedHour = -1;

	public bool transferBizManSettings;

	private MovingServiceSettings _movingServiceSettings;

	private int[] _days;

	private List<BuildingRegistration> _destinationBuildingRegistrations;

	private float _itemsPrice;

	private List<BuildingRegistration> _originBuildingRegistrations;

	private string _originBuildingType;

	private void Start()
	{
		selectedDay = -1;
		Building building = BuildingHelper.GetBuilding(DialogController.current.contact.Address);
		_movingServiceSettings = building.SpecialService.settings as MovingServiceSettings;
		selectedDestinationAddressDropdown.InitEmpty("dialog_moving_service_no_addresses_available");
		SetUpListeners();
		UpdatePriceLabels();
		_originBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterOrigin);
		List<string> newOptions = _originBuildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList();
		selectedOriginAddressDropdown.SetOptions(newOptions, localize: false);
		_days = new int[7];
		for (int num = 0; num < _days.Length; num++)
		{
			_days[num] = SaveGameManager.Current.Day + 1 + num;
		}
		selectedDayDropdown.SetOptions(_days.Select((int x) => LocalizorManager.GetLocalization("common_day_number", new
		{
			number = x
		})).ToList(), localize: false);
		transferBizManSettingsToggle.isOn = false;
		if (_originBuildingRegistrations.Count == 0)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"businessName",
				building.SpecialService.businessName
			} };
			Notifications.Show(NotificationType.Error, "interior_installation_firm_no_addresses_available", notificationData);
			selectedOriginAddressDropdown.SetInteractable(interactable: false);
			selectedDestinationAddressDropdown.SetInteractable(interactable: false);
			selectedDayDropdown.SetInteractable(interactable: false);
		}
		else
		{
			selectedOriginAddressDropdown.SetInteractable(interactable: true);
			selectedDestinationAddressDropdown.SetInteractable(interactable: false);
			selectedDayDropdown.SetInteractable(interactable: true);
		}
	}

	private static bool PlayerBuildingFilterOrigin(BuildingRegistration buildingRegistration)
	{
		foreach (MovingServiceContract movingServiceContract in SaveGameManager.Current.movingServiceContracts)
		{
			if (movingServiceContract.originMovingAddress == buildingRegistration.Address || movingServiceContract.destinationMovingAddress == buildingRegistration.Address)
			{
				return false;
			}
		}
		return true;
	}

	private float GetTotalCost()
	{
		return _itemsPrice + _movingServiceSettings.movingFee;
	}

	private void SetUpListeners()
	{
		selectedOriginAddressDropdown.onOptionSelected.AddListener(OnSelectOriginAddress);
		selectedOriginAddressDropdown.SetPlaceholder("dialog_interior_select_address");
		selectedDestinationAddressDropdown.onOptionSelected.AddListener(OnSelectDestinationAddress);
		selectedDayDropdown.onOptionSelected.AddListener(OnSelectDay);
		selectedDayDropdown.SetPlaceholder("dialog_interior_select_day");
		transferBizManSettingsToggle.onValueChanged.AddListener(delegate
		{
			transferBizManSettings = transferBizManSettingsToggle.isOn;
		});
	}

	private void OnSelectOriginAddress(int index)
	{
		selectedOriginAddress = _originBuildingRegistrations[index].Address;
		_originBuildingType = _originBuildingRegistrations[index].BuildingCached.BuildingType;
		_destinationBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterDestination);
		if (_destinationBuildingRegistrations.Count > 0)
		{
			selectedDestinationAddressDropdown.SetPlaceholder("dialog_interior_select_address");
			selectedDestinationAddressDropdown.SetOptions(_destinationBuildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList(), localize: false);
			selectedDestinationAddressDropdown.SetInteractable(interactable: true);
		}
		else
		{
			selectedDestinationAddressDropdown.SetPlaceholder("dialog_moving_service_no_addresses_available");
			selectedDestinationAddressDropdown.SetInteractable(interactable: false);
			selectedDestinationAddressDropdown.ResetSelectedOption();
		}
		selectedDestinationAddress = null;
		_itemsPrice = 0f;
		_itemsPrice = MovingServiceHelper.GetItemsPrice(selectedOriginAddress, _movingServiceSettings.feePerItem);
		UpdatePriceLabels();
	}

	private bool PlayerBuildingFilterDestination(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.Address == selectedOriginAddress || buildingRegistration.GetBuildingType() != _originBuildingType)
		{
			return false;
		}
		foreach (MovingServiceContract movingServiceContract in SaveGameManager.Current.movingServiceContracts)
		{
			if (movingServiceContract.originMovingAddress == buildingRegistration.Address || movingServiceContract.destinationMovingAddress == buildingRegistration.Address)
			{
				return false;
			}
		}
		return true;
	}

	private void OnSelectDestinationAddress(int index)
	{
		selectedDestinationAddress = _destinationBuildingRegistrations[index].Address;
	}

	private void OnSelectDay(int index)
	{
		selectedDay = _days[index];
		selectedHour = Random.Range(10, 17);
	}

	private void UpdatePriceLabels()
	{
		movingFeeValue.text = _movingServiceSettings.movingFee.ToShortCurrencyFormat();
		totalCostValue.text = GetTotalCost().ToShortCurrencyFormat();
		feePerItemValue.text = _movingServiceSettings.feePerItem.ToShortCurrencyFormat();
	}
}
