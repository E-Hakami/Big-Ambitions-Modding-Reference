using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using BusinessLayoutSets;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Player.SaveSystem.CompatibilityFixes;
using TMPro;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace UI.Dialog;

public class InteriorInstallationFirmDesignSettings : MonoBehaviour
{
	private const int InitialDayToDoInstallation = 1;

	private const int LastDayToDoInstallation = 7;

	[SerializeField]
	private Dropdown selectedAddressDropdown;

	[SerializeField]
	private Dropdown selectedDayDropdown;

	[SerializeField]
	private Dropdown designNameDropdown;

	[SerializeField]
	private TMP_Text installationFeeValue;

	[SerializeField]
	private TMP_Text layoutPriceValue;

	[SerializeField]
	private TMP_Text totalCostValue;

	[SerializeField]
	private TMP_Text missingModsWarning;

	public Address selectedAddress;

	public int selectedDay = -1;

	public string designName;

	public bool isBlueprint;

	public bool isCompatBlueprint;

	public bool hasDiscontinuedItems;

	private InteriorInstallationFirmSettings _interiorInstallationFirmSettings;

	private float _layoutPrice;

	private Address[] _addresses;

	private int[] _days;

	private (string name, bool isBlueprint)[] _designs;

	public BuildingRegistration BuildingRegistration => BuildingHelper.GetBuildingRegistration(selectedAddress);

	private float GetTotalCost()
	{
		return _layoutPrice + GetInstallationFee();
	}

	private float GetInstallationFee()
	{
		if (!isCompatBlueprint)
		{
			return InteriorInstallationFirmHelper.GetInstallationFee(selectedAddress);
		}
		return 0f;
	}

	private async void Start()
	{
		LoadingSpinner.Show();
		await BlueprintsFolderLoader.GetBlueprints();
		LoadingSpinner.Hide();
		selectedDay = -1;
		designName = null;
		Building building = BuildingHelper.GetBuilding(DialogController.current.contact.Address);
		_interiorInstallationFirmSettings = building.SpecialService.settings as InteriorInstallationFirmSettings;
		SetUpListeners();
		UpdatePriceLabels();
		List<BuildingRegistration> playerBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		List<string> newOptions = playerBuildingRegistrations.Select((BuildingRegistration x) => x.GetDisplayName()).ToList();
		_addresses = playerBuildingRegistrations.Select((BuildingRegistration x) => x.Address).ToArray();
		selectedAddressDropdown.SetOptions(newOptions, localize: false);
		_days = new int[7];
		for (int num = 0; num < _days.Length; num++)
		{
			_days[num] = SaveGameManager.Current.Day + 1 + num;
		}
		selectedDayDropdown.SetOptions(_days.Select((int x) => LocalizorManager.GetLocalization("common_day_number", new
		{
			number = x
		})).ToList(), localize: false);
		if (_addresses.Length == 0)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"businessName",
				building.SpecialService.businessName
			} };
			Notifications.Show(NotificationType.Error, "interior_installation_firm_no_addresses_available", notificationData);
			selectedAddressDropdown.SetInteractable(interactable: false);
			selectedDayDropdown.SetInteractable(interactable: false);
		}
		else
		{
			selectedAddressDropdown.SetInteractable(interactable: true);
			selectedDayDropdown.SetInteractable(interactable: true);
		}
	}

	private bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (!_interiorInstallationFirmSettings.buildingTypesThatCanInstall.Contains(buildingRegistration.GetBuildingType()))
		{
			return false;
		}
		foreach (InteriorInstallationFirmContract interiorInstallationFirmContract in SaveGameManager.Current.interiorInstallationFirmContracts)
		{
			if (interiorInstallationFirmContract.addressToDoTheInstallation == buildingRegistration.Address)
			{
				return false;
			}
		}
		return true;
	}

	private void SetUpListeners()
	{
		selectedAddressDropdown.onOptionSelected.AddListener(OnSelectAddress);
		selectedAddressDropdown.SetPlaceholder("dialog_interior_select_address");
		selectedDayDropdown.onOptionSelected.AddListener(OnSelectDay);
		selectedDayDropdown.SetPlaceholder("dialog_interior_select_day");
		designNameDropdown.onOptionSelected.AddListener(OnSelectDesign);
		designNameDropdown.SetPlaceholder("dialog_interior_select_design");
		designNameDropdown.SetOptions(new List<string>());
		designNameDropdown.SetInteractable(interactable: false);
	}

	private async void OnSelectAddress(int index)
	{
		selectedAddress = _addresses[index];
		isCompatBlueprint = false;
		LoadingSpinner.Show();
		string text = (BuildingTypeHelper.GetData(BuildingRegistration).HasTag(TagRef.Buildingtypetag.containsnobusiness) ? "ba:businesstype_empty" : BuildingRegistration.businessTypeName);
		string[] firmInteriors = InteriorInstallationFirmHelper.GetInteriorDesignsNamesForBuilding(BuildingRegistration.BuildingCached.BuildingType, new BuildingSizeInfo(BuildingRegistration), text);
		List<string> list = await InteriorInstallationFirmHelper.GetBlueprintNames(BuildingRegistration.BuildingCached.BuildingType, new BuildingSizeInfo(BuildingRegistration), text);
		_designs = new(string, bool)[firmInteriors.Length + list.Count];
		for (int i = 0; i < firmInteriors.Length; i++)
		{
			_designs[i] = (name: firmInteriors[i], isBlueprint: false);
		}
		for (int j = 0; j < list.Count; j++)
		{
			_designs[firmInteriors.Length + j] = (name: list[j], isBlueprint: true);
		}
		if (_designs.Length == 0)
		{
			Notifications.ShowError("interior_installation_firm_no_designs_for_selected_address");
			designNameDropdown.ResetSelectedOption();
			designName = null;
			designNameDropdown.SetInteractable(interactable: false);
		}
		else
		{
			designNameDropdown.SetOptions(_designs.Select(((string name, bool isBlueprint) x) => (!x.isBlueprint) ? ("interior_design_" + x.name).GetLocalization() : x.name).ToList(), localize: false);
			designNameDropdown.SetInteractable(interactable: true);
		}
		LoadingSpinner.Hide();
	}

	private void OnSelectDay(int index)
	{
		selectedDay = _days[index];
		isCompatBlueprint = false;
	}

	private void OnSelectDesign(int index)
	{
		designName = _designs[index].name;
		isBlueprint = _designs[index].isBlueprint;
		UpdateLayoutPrice();
	}

	private void UpdatePriceLabels()
	{
		installationFeeValue.text = GetInstallationFee().ToShortCurrencyFormat();
		totalCostValue.text = GetTotalCost().ToShortCurrencyFormat();
		layoutPriceValue.text = _layoutPrice.ToShortCurrencyFormat();
	}

	private async void UpdateLayoutPrice()
	{
		if (selectedAddress == null || designName == null)
		{
			_layoutPrice = 0f;
			isCompatBlueprint = false;
			UpdatePriceLabels();
			return;
		}
		missingModsWarning.enabled = false;
		if (isBlueprint)
		{
			Blueprint blueprint = await BlueprintsFolderLoader.GetBlueprint(designName);
			BusinessLayoutSet businessLayoutSet = await blueprint.GetLayout();
			missingModsWarning.enabled = blueprint.IsMissingMods();
			hasDiscontinuedItems = CompatibilityBlueprintValidator.ContainsInvalidItems(businessLayoutSet);
			isCompatBlueprint = blueprint.metadata.GetDataElementValue(DataElement.CompatBlueprint) == SaveGameManager.Current.characterId;
			_layoutPrice = (isCompatBlueprint ? 0f : businessLayoutSet.Items.Sum((Item x) => ItemHelper.GetDefaultMarketPrice(x.itemName)));
		}
		else
		{
			BusinessLayoutSet businessLayoutSet = InteriorInstallationFirmHelper.GetInteriorDesignLayout(designName, BuildingRegistration.BuildingCached.BuildingType, new BuildingSizeInfo(BuildingRegistration), BuildingRegistration.businessTypeName);
			_layoutPrice = businessLayoutSet.Items.Sum((Item x) => ItemHelper.GetDefaultMarketPrice(x.itemName));
			isCompatBlueprint = false;
			hasDiscontinuedItems = false;
		}
		UpdatePriceLabels();
	}
}
