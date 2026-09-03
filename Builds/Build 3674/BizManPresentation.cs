using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Buildings.Office.Headquarters;
using Buildings.Retail.Businesses.CinemaTheater;
using BusinessLayoutSets;
using Entities;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI;
using UI.Components;
using UI.Guiders;
using UI.Notification;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.BizMan.StartBusiness;
using UI.Smartphone.Apps.Persona;
using UnityEngine;
using UnityEngine.UI;
using Vehicles.DeliveryDriverJob;
using Vehicles.VehicleTypes;

public class BizManPresentation : MonoBehaviour
{
	public Transform businessSideAiOwned;

	[SerializeField]
	private Transform topInfoBox;

	[SerializeField]
	private Transform bottomInfoBox;

	[SerializeField]
	private Transform forSaleBox;

	[SerializeField]
	private Transform notForSaleBox;

	[SerializeField]
	private Transform forSaleOfferButton;

	[SerializeField]
	private Transform notForSaleOfferButton;

	[SerializeField]
	private Transform buyBuildingOfferBox;

	[SerializeField]
	private StartBusinessUI startBusinessUI;

	[SerializeField]
	private RentBuildingUI rentBuildingUI;

	[SerializeField]
	private HamptonsPurchaseBoxUI hamptonsPurchaseBoxUI;

	[SerializeField]
	private GameObject previewButton;

	public Image businessSideInfoIcon;

	public TextLocalizationComponent businessSideInfoBusinessType;

	public TextMeshProUGUI businessSideInfoBusinessName;

	public TextLocalizationComponent businessSideInfoBusinessDescription;

	public Transform businessSideInfoOpeningHours;

	public TextLocalizationComponent businessSideInfoCorporation;

	public TextLocalizationComponent businessSideInfoValuation;

	public TextLocalizationComponent businessSideOfferValuation;

	public TMP_InputField offerAmountInputField;

	public TextLocalizationComponent buildingSideInfoValuation;

	public TMP_InputField buyBuildingAmountInputField;

	public GameObject overtakeOfferPanel;

	public GameObject sendOvertakeOfferPanel;

	public Transform storeInventory;

	public GameObject showEmployeesButton;

	[SerializeField]
	private BizManBusiness bizManBusiness;

	[SerializeField]
	private Image buildingImage;

	[SerializeField]
	private ItemsListEntry storeEntryTemplate;

	private Texture2D _cachedBuildingImageTexture;

	private readonly HashSet<OpeningHourSlot> _openingHourSlots = new HashSet<OpeningHourSlot>();

	private void Awake()
	{
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool show)
		{
			if (!show)
			{
				buildingImage.sprite = null;
			}
		});
		startBusinessUI.OnBusinessStarted += BusinessHasStarted;
		KeyboardInputHelper.Configure(offerAmountInputField, SendOvertakeOffer);
		KeyboardInputHelper.Configure(buyBuildingAmountInputField, SendBuyBuildingOffer);
		KeyboardInputHelper.Configure(hamptonsPurchaseBoxUI.offerInputField, SendBuyBuildingOffer);
	}

	private void OnEnable()
	{
		LoadLeftSide();
		if (bizManBusiness.building.SpecialService != null)
		{
			SetAiOwned();
			return;
		}
		bool availableForRent = bizManBusiness.buildingRegistration.AvailableForRent;
		bool buildingOwnedByPlayer = bizManBusiness.buildingRegistration.BuildingOwnedByPlayer;
		bool rentedByPlayer = bizManBusiness.buildingRegistration.RentedByPlayer;
		if (!rentedByPlayer && IsHamptonsOwnableHouse(bizManBusiness.building))
		{
			SetActiveView("HamptonsOwnableHouseView");
		}
		else if (BuildingTypeHelper.GetData(bizManBusiness.building).HasTag(TagRef.Buildingtypetag.canbetakenover))
		{
			if (availableForRent || ((!rentedByPlayer & buildingOwnedByPlayer) && bizManBusiness.showTakeoverView) || (rentedByPlayer && bizManBusiness.buildingRegistration.businessTypeName == "ba:businesstype_empty"))
			{
				bizManBusiness.showTakeoverView = false;
				SetActiveView("RentView");
			}
			else if (rentedByPlayer)
			{
				SetActiveView(null);
			}
			else
			{
				SetAiOwned();
			}
		}
		else if (bizManBusiness.building.BuildingType == "ba:buildingtype_residential")
		{
			if (availableForRent | buildingOwnedByPlayer | rentedByPlayer)
			{
				SetActiveView("RentView");
			}
			else
			{
				SetActiveView(null);
			}
		}
		else
		{
			SetActiveView(null);
		}
	}

	private void OnDestroy()
	{
		startBusinessUI.OnBusinessStarted -= BusinessHasStarted;
	}

	private void BusinessHasStarted()
	{
		bizManBusiness.SetInitialTab();
		bizManBusiness.RefreshData();
	}

	public void SetActiveView(string view)
	{
		overtakeOfferPanel.SetActive(value: false);
		hamptonsPurchaseBoxUI.Hide();
		switch (view)
		{
		case "StartBusiness":
			startBusinessUI.Show(bizManBusiness.buildingRegistration);
			rentBuildingUI.Hide();
			businessSideAiOwned.gameObject.SetActive(value: false);
			sendOvertakeOfferPanel.SetActive(value: false);
			break;
		case "RentView":
			startBusinessUI.Hide();
			rentBuildingUI.Show(bizManBusiness.buildingRegistration);
			businessSideAiOwned.gameObject.SetActive(value: false);
			sendOvertakeOfferPanel.SetActive(value: false);
			break;
		case "Info":
			startBusinessUI.Hide();
			rentBuildingUI.Hide();
			businessSideAiOwned.gameObject.SetActive(bizManBusiness.building.BuildingType != "ba:buildingtype_warehouse");
			sendOvertakeOfferPanel.SetActive(bizManBusiness.building.SpecialService == null && bizManBusiness.building.BuildingType != "ba:buildingtype_warehouse");
			break;
		case "HamptonsOwnableHouseView":
			startBusinessUI.Hide();
			rentBuildingUI.Hide();
			businessSideAiOwned.gameObject.SetActive(value: false);
			sendOvertakeOfferPanel.SetActive(value: false);
			hamptonsPurchaseBoxUI.Show(bizManBusiness.buildingRegistration);
			break;
		default:
			startBusinessUI.Hide();
			rentBuildingUI.Hide();
			businessSideAiOwned.gameObject.SetActive(value: false);
			sendOvertakeOfferPanel.SetActive(value: false);
			break;
		}
	}

	public void LoadLeftSide()
	{
		Building building = BuildingHelper.GetBuilding(bizManBusiness.buildingRegistration.Address);
		bottomInfoBox.GetLanguageChangeEventByName("BuildingTypeLabel").SetData(LanguageChangeEventDataHolder.Create("bizman_building_buildingtype", new
		{
			type = building.BuildingType
		}));
		bottomInfoBox.GetLanguageChangeEventByName("AddressLabel").SetValue(bizManBusiness.buildingRegistration.Address.ToFormattedString(), clearKey: true);
		buyBuildingOfferBox.gameObject.SetActive(value: false);
		forSaleBox.gameObject.SetActive(value: false);
		notForSaleBox.gameObject.SetActive(value: false);
		UpdatePreviewButtonVisibility(bizManBusiness.buildingRegistration);
		if (!bizManBusiness.buildingRegistration.BuildingOwnedByPlayer)
		{
			BuildingForSale buildingForSale = SaveGameManager.Current.buildingsForSale.FirstOrDefault((BuildingForSale x) => x.address == bizManBusiness.buildingRegistration.Address);
			if (buildingForSale != null)
			{
				buyBuildingAmountInputField.text = buildingForSale.buildingPrice.ToString("F0");
				forSaleBox.GetLanguageChangeEventByName("BuildingDescription").Key = "bizman_building_description_" + building.BuildingType.GetIdWithoutType();
				TextLocalizationComponent languageChangeEventByName = forSaleBox.GetLanguageChangeEventByName("BuildingPriceLabel");
				if (building.IsHamptonsHouse())
				{
					languageChangeEventByName.gameObject.SetActive(value: false);
				}
				else
				{
					languageChangeEventByName.gameObject.SetActive(value: true);
					languageChangeEventByName.Arguments = new
					{
						price = buildingForSale.buildingPrice.ToShortCurrencyFormat()
					};
				}
				forSaleBox.GetLanguageChangeEventByName("RivalBuildingOwner").SetData(BusinessHelper.GetBuildingOwnerDescription(bizManBusiness.buildingRegistration));
				forSaleBox.gameObject.SetActive(value: true);
				forSaleOfferButton.gameObject.SetActive(!building.IsHamptonsHouse());
				notForSaleBox.gameObject.SetActive(value: false);
			}
			else if (bizManBusiness.building.SpecialService == null)
			{
				buyBuildingAmountInputField.text = "";
				notForSaleBox.GetLanguageChangeEventByName("RivalBuildingOwner").SetData(BusinessHelper.GetBuildingOwnerDescription(bizManBusiness.buildingRegistration));
				forSaleBox.gameObject.SetActive(value: false);
				notForSaleBox.gameObject.SetActive(value: true);
				notForSaleOfferButton.gameObject.SetActive(!building.IsHamptonsHouse());
			}
		}
		float marketValue = bizManBusiness.building.GetMarketValue();
		string text = ((marketValue < 0f) ? "-" : marketValue.ToShortCurrencyFormat());
		buildingSideInfoValuation.SetData("bizman_estimated_valuation".Localize(new
		{
			valuation = text
		}));
		topInfoBox.GetLabelByName("TrafficIndex").text = bizManBusiness.building.trafficIndex.ToString();
		topInfoBox.GetLabelByName("CustomerCapacity").text = ((building.GetCustomerCapacity == -1) ? "-" : $"{building.GetCustomerCapacity}");
		topInfoBox.GetLanguageChangeEventByName("Neighborhood").Key = bizManBusiness.building.Neighbourhood;
		BuildingHelper.SetBuildingAreaLocalization(bottomInfoBox.GetLanguageChangeEventByName("BuildingArea"), bizManBusiness.building);
		bottomInfoBox.GetLabelByName("MarketValue").text = text;
		bottomInfoBox.GetLabelByName("TotalSize").text = bizManBusiness.building.totalSqm.ToFormattedArea();
		CityBuildingController cbc = InstanceBehavior<CityManager>.Instance?.FindCityBuildingController(building.Address);
		if (!cbc)
		{
			return;
		}
		cbc.GenerateOutsideImage(delegate(ScreenshotCaptureController.CaptureCommand command)
		{
			if (building.Address == cbc.building.Address)
			{
				if (buildingImage.sprite != null)
				{
					UnityEngine.Object.Destroy(buildingImage.sprite.texture);
					UnityEngine.Object.Destroy(buildingImage.sprite);
				}
				if (_cachedBuildingImageTexture != null)
				{
					UnityEngine.Object.Destroy(_cachedBuildingImageTexture);
				}
				buildingImage.sprite = Sprite.Create(command.outputTexture, new Rect(0f, 0f, command.outputTexture.width, command.outputTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
				_cachedBuildingImageTexture = command.outputTexture;
			}
		});
	}

	private static bool IsHamptonsOwnableHouse(Building building)
	{
		if (!building.IsHamptonsHouse())
		{
			return false;
		}
		if (building.IsHamptonsAIVilla())
		{
			return false;
		}
		IReadOnlyCollection<SpecialRival> specialRivals = RivalsHelper.GetSpecialRivals();
		BuildingRegistration registration = building.GetRegistration();
		foreach (SpecialRival item in specialRivals)
		{
			if (item.rivalData.id == registration.buildingOwnerRivalId)
			{
				return false;
			}
		}
		return true;
	}

	public void SetAiOwned()
	{
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(bizManBusiness.buildingRegistration.BusinessName, LogoSize.SquareSign);
		Rect rect = new Rect(0f, 0f, businessLogoTexture.width, businessLogoTexture.height);
		if (businessSideInfoIcon.sprite != null)
		{
			UnityEngine.Object.Destroy(businessSideInfoIcon.sprite);
		}
		businessSideInfoIcon.sprite = Sprite.Create(businessLogoTexture, rect, new Vector2(0.5f, 0.5f));
		businessSideInfoBusinessType.Key = bizManBusiness.buildingRegistration.businessTypeName;
		businessSideInfoBusinessName.text = bizManBusiness.buildingRegistration.BusinessName;
		if (string.IsNullOrEmpty(bizManBusiness.buildingRegistration.BusinessDescription))
		{
			businessSideInfoBusinessDescription.TextContainer.text = "";
		}
		if (string.IsNullOrEmpty(bizManBusiness.buildingRegistration.businessOwnerRivalId))
		{
			businessSideInfoCorporation.gameObject.SetActive(value: false);
			businessSideInfoBusinessDescription.Key = bizManBusiness.buildingRegistration.BusinessDescription;
			businessSideInfoBusinessDescription.gameObject.SetActive(value: true);
		}
		else
		{
			businessSideInfoBusinessDescription.gameObject.SetActive(value: false);
			businessSideInfoCorporation.gameObject.SetActive(value: true);
			businessSideInfoCorporation.SetData(BusinessHelper.GetBusinessOwnerDescription(bizManBusiness.buildingRegistration));
		}
		if (bizManBusiness.building.SpecialService == null)
		{
			string valuation = CompetitionHelper.CalculateAiOwnedValuation(bizManBusiness.buildingRegistration).ToShortCurrencyFormat();
			LanguageChangeEventDataHolder data = "bizman_estimated_valuation".Localize(new { valuation });
			businessSideInfoValuation.SetData(data);
			businessSideOfferValuation.SetData(data);
			offerAmountInputField.text = "";
		}
		foreach (ScheduleDay scheduleDay in bizManBusiness.buildingRegistration.scheduleDays)
		{
			TextMeshProUGUI labelByName = businessSideInfoOpeningHours.Find(scheduleDay.day.ToStringFast()).GetLabelByName("Value");
			if (!bizManBusiness.buildingRegistration.temporarilyClosed && scheduleDay != null && scheduleDay.isOpen)
			{
				_openingHourSlots.Clear();
				foreach (OpeningHourSlot openingHourSlot in scheduleDay.openingHourSlots)
				{
					_openingHourSlots.Add(openingHourSlot);
				}
				labelByName.text = string.Join(", ", from x in _openingHourSlots
					orderby x.startingHour
					select x.startingHour.GetFormattedTime() + " - " + x.endingHour.GetFormattedTime());
			}
			else
			{
				labelByName.text = "common_closed".GetLocalization();
			}
		}
		storeInventory.gameObject.SetActive(CanShowStoreInventory(bizManBusiness.buildingRegistration));
		showEmployeesButton.SetActive(!string.IsNullOrEmpty(bizManBusiness.buildingRegistration.businessOwnerRivalId));
		SetActiveView("Info");
	}

	private static bool CanShowStoreInventory(BuildingRegistration buildingRegistration)
	{
		if (!(buildingRegistration.businessTypeName == "ba:businesstype_importexport") && buildingRegistration.GetListOfItemsForSale().Count <= 0)
		{
			return GetVehicleTypesForSale(buildingRegistration).Count > 0;
		}
		return true;
	}

	public void BrowseInventory()
	{
		InstanceBehavior<UIs>.Instance.itemsList.Clear();
		PrepareInventory(bizManBusiness.buildingRegistration);
		InstanceBehavior<UIs>.Instance.itemsList.SetTitle("bizman_store_inventory_title".Localize(new
		{
			businessName = bizManBusiness.buildingRegistration.BusinessName
		}).ToString());
		InstanceBehavior<UIs>.Instance.itemsList.Toggle(newState: true);
		GameAnalytics.TrackOpenBrowseInventory(bizManBusiness.buildingRegistration.Address.ToAnalyticsString());
	}

	private void PrepareInventory(BuildingRegistration registration)
	{
		if (registration.businessTypeName == "ba:businesstype_importexport")
		{
			PrepareImportExportInventory();
			return;
		}
		List<VehicleType> vehicleTypesForSale = GetVehicleTypesForSale(registration);
		if (vehicleTypesForSale.Count > 0)
		{
			PrepareVehicleInventory(vehicleTypesForSale);
		}
		else
		{
			PrepareDefaultInventory(registration.businessTypeName);
		}
	}

	private void PrepareImportExportInventory()
	{
		foreach (string item in ((ImportExportSettings)bizManBusiness.buildingRegistration.BuildingCached.SpecialService.settings).GetItemsAvailable())
		{
			ItemsListEntry itemsListEntry = UnityEngine.Object.Instantiate(storeEntryTemplate);
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item);
			LanguageChangeEventDataHolder itemLabel = LocalizationHelper.GetItemLabel(item, byName.boxSize);
			itemsListEntry.Init(itemLabel, ItemHelper.GetIconWithFallback(item));
			InstanceBehavior<UIs>.Instance.itemsList.AddEntry(itemsListEntry);
		}
	}

	private void PrepareVehicleInventory(List<VehicleType> vehicleTypesForSale)
	{
		foreach (VehicleType item in vehicleTypesForSale)
		{
			Sprite sprite = ItemHelper.GetIcon(item.vehicleTypeName + "showcase");
			if (sprite == null)
			{
				sprite = InstanceBehavior<GlobalReferences>.Instance.vehiclePOIIcon;
			}
			ItemsListEntry itemsListEntry = UnityEngine.Object.Instantiate(storeEntryTemplate);
			itemsListEntry.Init(new LanguageChangeEventDataHolder
			{
				Key = item.vehicleTypeName
			}, sprite, item.price);
			InstanceBehavior<UIs>.Instance.itemsList.AddEntry(itemsListEntry);
		}
	}

	private static List<VehicleType> GetVehicleTypesForSale(BuildingRegistration buildingRegistration)
	{
		List<VehicleType> list = new List<VehicleType>();
		if (string.IsNullOrEmpty(buildingRegistration.Layout))
		{
			return list;
		}
		BusinessLayoutSet orLoadBusinessLayoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(buildingRegistration.businessTypeName, new BuildingSizeInfo(buildingRegistration), buildingRegistration.Layout, warnIfNotFound: false);
		if (orLoadBusinessLayoutSet?.Items == null)
		{
			return list;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (BusinessLayoutSets.Item item in orLoadBusinessLayoutSet.Items)
		{
			PlayerItemPurchaserSettings playerItemPurchaserSettings = item.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled)
			{
				BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item.playerItemPurchaserSettings.itemName);
				if (!(byName == null) && !string.IsNullOrEmpty(byName.vehicleType))
				{
					hashSet.Add(byName.vehicleType);
				}
			}
		}
		foreach (string item2 in hashSet)
		{
			VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(item2);
			if ((bool)vehicleType)
			{
				list.Add(vehicleType);
			}
		}
		list.Sort(CompareByName);
		return list;
		static int CompareByName(VehicleType a, VehicleType b)
		{
			return string.CompareOrdinal(a.vehicleTypeName, b.vehicleTypeName);
		}
	}

	private void PrepareDefaultInventory(string type)
	{
		bool flag = type == "ba:businesstype_wholesalestore";
		string fallbackItemName = (flag ? "ba:itemname_closedcardboardbox" : "ba:itemname_paperbag");
		float num = (float)bizManBusiness.buildingRegistration.GetPriceIndex() / 100f;
		List<string> listOfItemsForSale = bizManBusiness.buildingRegistration.GetListOfItemsForSale();
		listOfItemsForSale.Sort();
		foreach (string item in listOfItemsForSale)
		{
			ItemsListEntry itemsListEntry = UnityEngine.Object.Instantiate(storeEntryTemplate);
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item);
			if (flag)
			{
				float num2 = byName.GetWholesalePrice() * num * ProductMarketHelper.GetProductMarketEventMultiplier(byName.itemName, bizManBusiness.buildingRegistration.Neighborhood);
				itemsListEntry.Init(LocalizationHelper.GetItemLabel(item, byName.boxSize), ItemHelper.GetIconWithFallback(item, fallbackItemName), num2 * (float)byName.boxSize);
			}
			else
			{
				itemsListEntry.Init(LocalizationHelper.GetItemLabel(item), ItemHelper.GetIconWithFallback(item, fallbackItemName), ItemHelper.GetPrice(byName.itemName, bizManBusiness.buildingRegistration) * num);
			}
			InstanceBehavior<UIs>.Instance.itemsList.AddEntry(itemsListEntry);
		}
	}

	public void RentBuilding()
	{
		if (!bizManBusiness.building.requiredDLC.DlcIsOwned())
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"dlc",
				bizManBusiness.building.requiredDLC.ToStringFast()
			} };
			Notifications.Show(NotificationType.Error, "common_requires_dlc", notificationData);
			return;
		}
		if (RivalsHelper.IsFeatureEnabled && !bizManBusiness.buildingRegistration.BuildingOwnedByPlayer)
		{
			SpecialRival specialRival = RivalsHelper.GetSpecialRival(bizManBusiness.buildingRegistration.buildingOwnerRivalId);
			if (specialRival != null && RivalsHelper.GetSpecialRivalState(specialRival.rivalData.id).isActive)
			{
				RivalsHelper.SendRentBuildingMessage(specialRival);
				Dictionary<string, string> notificationData2 = new Dictionary<string, string> { 
				{
					"name",
					specialRival.rivalData.rivalName
				} };
				Notifications.Show(NotificationType.Error, "notification_cannot_rent_building_owned_by_rival", notificationData2);
				return;
			}
		}
		int buildingDailyMarketRent = bizManBusiness.building.GetBuildingDailyMarketRent();
		int num = BuildingHelper.CalculateDeposit(bizManBusiness.buildingRegistration, buildingDailyMarketRent);
		int num2 = BuildingHelper.CalculateDefaultLayoutPrice(bizManBusiness.building.Address);
		bool flag = bizManBusiness.buildingRegistration.GetBuildingType() == "ba:buildingtype_residential";
		bool flag2 = false;
		if (!bizManBusiness.buildingRegistration.BuildingOwnedByPlayer)
		{
			flag2 = !TutorialHelper.HasCompletedObjective("tutorial_quest_get_some_sleep_objective_4") & flag;
		}
		if (!flag2 && SaveGameManager.Current.Money < (float)(num + num2 + buildingDailyMarketRent))
		{
			Notifications.ShowInsufficientMoney();
			return;
		}
		string text = bizManBusiness.buildingRegistration.Address.ToFormattedString();
		Dictionary<string, string> data = new Dictionary<string, string> { { "address", text } };
		bool flag3 = BusinessHelper.IsTaxDeductibleBusinessBuilding(bizManBusiness.buildingRegistration);
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deposit", data);
		if (flag3)
		{
			transactionInfo.SetTaxDeductibleName(text);
		}
		float amount = -(num + num2);
		Address address = bizManBusiness.buildingRegistration.Address;
		if (!GameManager.ChangeMoneySafe(amount, transactionInfo, null, address, force: false, showNotification: true))
		{
			return;
		}
		if (!bizManBusiness.buildingRegistration.BuildingOwnedByPlayer && !flag2)
		{
			TransactionInfo transactionInfo2 = new TransactionInfo("ba:transaction_rent", "ba:transactioncategory_rent", data);
			if (flag3)
			{
				transactionInfo2.SetTaxDeductibleName(text);
			}
			float amount2 = 0f - bizManBusiness.buildingRegistration.RentPerDay;
			address = bizManBusiness.buildingRegistration.Address;
			GameManager.ChangeMoneySafe(amount2, transactionInfo2, null, address);
		}
		BuildingHelper.RentBuilding(bizManBusiness.building, buildingDailyMarketRent, num);
		UpdatePreviewButtonVisibility(bizManBusiness.buildingRegistration);
		SetActiveView("RentView");
	}

	public void TerminateContract()
	{
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.Address == bizManBusiness.buildingRegistration.Address)
		{
			Notifications.Show(NotificationType.Error, "bizman_presentation_notification_cant_terminate_when_inside");
			return;
		}
		LanguageChangeEventDataHolder bodyData = "bizman_presentation_hud_confirm_terminate_contract".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			if (ItemHelper.AreThereSpecialGiftsInAddress(bizManBusiness.buildingRegistration.Address))
			{
				LanguageChangeEventDataHolder bodyData2 = LanguageChangeEventDataHolder.Create("bizman_presentation_hud_confirm_special_gift_inside");
				Action onConfirmAction = OnTerminateContractConfirm;
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData2, onConfirmAction);
			}
			else
			{
				OnTerminateContractConfirm();
			}
		});
	}

	private void OnTerminateContractConfirm()
	{
		bool flag = false;
		BuildingRegistration registration = bizManBusiness.buildingRegistration;
		List<ItemInstance> list = registration.itemInstances.Values.ToList();
		List<VehicleInstance> list2 = SaveGameManager.Current.VehicleInstances.Where((VehicleInstance x) => x.Address == bizManBusiness.buildingRegistration.Address).ToList();
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"address",
			bizManBusiness.buildingRegistration.Address.ToFormattedString()
		} };
		if (list.Count > 0 || list2.Count > 0)
		{
			float num = list.Sum((ItemInstance itemInstance) => itemInstance.GetSellingPrice());
			foreach (VehicleInstance item in list2)
			{
				num += item.GetSellingPrice();
			}
			num += registration.lastDeposit;
			if (Directory.Exists(LogoHelper.GetPlayerBusinessLogoPath(bizManBusiness.buildingRegistration.BusinessName)))
			{
				Directory.Delete(LogoHelper.GetPlayerBusinessLogoPath(bizManBusiness.buildingRegistration.BusinessName), recursive: true);
			}
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_depositreturnfurniture", data);
			float amount = num;
			Address address = bizManBusiness.buildingRegistration.Address;
			GameManager.ChangeMoneySafe(amount, transactionInfo, null, address);
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{
					"price",
					num.ToShortCurrencyFormat()
				},
				{
					"name",
					bizManBusiness.buildingRegistration.Address.ToFormattedString()
				}
			};
			Notifications.Show(NotificationType.Success, "bizman_presentation_notification_itemsold", notificationData);
			flag = true;
			foreach (ItemInstance item2 in list)
			{
				registration.RemoveItemInstanceFromBuilding(item2);
			}
			bool flag2 = registration.BuildingCached.IsHamptonsHouse();
			foreach (VehicleInstance item3 in list2)
			{
				VehicleController vehicleController = null;
				if (flag2 && item3.VehicleType.IsMotorVehicle)
				{
					vehicleController = VehicleHelper.GetVehicleController(item3);
				}
				item3.Delete(vehicleController);
			}
		}
		registration.BusinessName = null;
		registration.RentedByPlayer = false;
		registration.AvailableForRent = true;
		registration.takenOver = false;
		if (registration.GetBuildingType() == "ba:buildingtype_warehouse")
		{
			PurchasingAgentHelper.CancelPlansThatDeliverToAddress(registration.Address);
		}
		if (bizManBusiness.building.BuildingType != "ba:buildingtype_residential")
		{
			registration.businessTypeName = "ba:businesstype_empty";
		}
		(InstanceBehavior<CityManager>.Instance?.FindCityBuildingController(bizManBusiness.building.Address))?.UpdatePoi();
		InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		GuidersManager.UpdateGuidersWithAddress(registration.Address);
		RealEstateHelper.AddNoHomeModifierIfNeeded();
		if (!flag)
		{
			TransactionInfo transactionInfo2 = new TransactionInfo("ba:transaction_depositreturn", data);
			float lastDeposit = registration.lastDeposit;
			Address address = bizManBusiness.buildingRegistration.Address;
			GameManager.ChangeMoneySafe(lastDeposit, transactionInfo2, null, address);
		}
		IEnumerable<TodoTask> tasks = SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.address == registration.Address);
		SaveGameManager.Current.FurnitureDeliveryContracts.RemoveAll((FurnitureDeliveryContract x) => x.toAddress == registration.Address);
		FoodDeliveryHelper.RemoveContractsForAddress(registration.Address);
		InstanceBehavior<UIs>.Instance?.tasksUI.InstantlyCompleteListOfTasks(tasks);
		GameEvent.Invoke("ba:gameevent_rentedbuilding");
		UpdatePreviewButtonVisibility(registration);
		OnEnable();
	}

	private void UpdatePreviewButtonVisibility(BuildingRegistration registration)
	{
		bool active = registration.AvailableForRent || (registration.BuildingCached.IsHamptonsHouse() && registration.IsOnSale());
		previewButton.SetActive(active);
	}

	public void SendOvertakeOffer()
	{
		Dictionary<string, string> notificationData;
		if (!bizManBusiness.building.requiredDLC.DlcIsOwned())
		{
			notificationData = new Dictionary<string, string> { 
			{
				"dlc",
				bizManBusiness.building.requiredDLC.ToStringFast()
			} };
			Notifications.Show(NotificationType.Error, "common_requires_dlc", notificationData);
			return;
		}
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.Address == bizManBusiness.address)
		{
			Notifications.Show(NotificationType.Error, "notification_cant_takeover_inside");
			return;
		}
		if (!EducationHelper.HasCompletedDiploma(DiplomaName.Headquarters))
		{
			notificationData = new Dictionary<string, string> { 
			{
				"name",
				DiplomaName.Headquarters.GetLocalizeKey().Localize().ToString()
			} };
			Notifications.Show(NotificationType.Error, "notification_missing_diploma_overtake_business", notificationData);
			return;
		}
		if (bizManBusiness.buildingRegistration.GetBuildingType() == "ba:buildingtype_office" && !EducationHelper.HasCompletedDiploma(DiplomaName.OfficeBusinesses))
		{
			notificationData = new Dictionary<string, string> { 
			{
				"name",
				DiplomaName.OfficeBusinesses.GetLocalization()
			} };
			Notifications.Show(NotificationType.Error, "notification_missing_diploma_overtake_business_office", notificationData);
			return;
		}
		if (RivalsHelper.IsFeatureEnabled)
		{
			SpecialRival specialRival = RivalsHelper.GetSpecialRival(bizManBusiness.buildingRegistration.buildingOwnerRivalId);
			if (specialRival != null && RivalsHelper.GetSpecialRivalState(specialRival.rivalData.id).isActive)
			{
				RivalsHelper.SendRentBuildingMessage(specialRival);
				notificationData = new Dictionary<string, string> { 
				{
					"name",
					specialRival.rivalData.rivalName
				} };
				Notifications.Show(NotificationType.Error, "notification_cannot_rent_building_owned_by_rival", notificationData);
				return;
			}
		}
		if (!float.TryParse(offerAmountInputField.text, out var result) || result <= 0f)
		{
			Notifications.Show(NotificationType.Error, "common_notification_invalid_amount");
			return;
		}
		if (SaveGameManager.Current.Money < result)
		{
			Notifications.ShowInsufficientMoney();
			return;
		}
		float num = CompetitionHelper.CalculateAiOwnedValuation(bizManBusiness.buildingRegistration) * RivalsHelper.GetOvertakeBusinessAcceptRate(bizManBusiness.buildingRegistration.businessOwnerRivalId, bizManBusiness.buildingRegistration.Address);
		if (result < num)
		{
			notificationData = new Dictionary<string, string> { 
			{
				"price",
				result.ToShortCurrencyFormat()
			} };
			Notifications.Show(NotificationType.Error, "bizman_presentation_notification_offer_rejected", notificationData);
			return;
		}
		string text = bizManBusiness.buildingRegistration.Address.ToFormattedString();
		Dictionary<string, string> data = new Dictionary<string, string> { { "address", text } };
		bool num2 = BusinessHelper.IsTaxDeductibleBusinessBuilding(bizManBusiness.buildingRegistration);
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deposit", data);
		if (num2)
		{
			transactionInfo.SetTaxDeductibleName(text);
		}
		float amount = 0f - result;
		Address address = bizManBusiness.buildingRegistration.Address;
		GameManager.ChangeMoneySafe(amount, transactionInfo, null, address);
		notificationData = new Dictionary<string, string> { 
		{
			"name",
			bizManBusiness.buildingRegistration.BusinessName
		} };
		Notifications.Show(NotificationType.Success, "bizman_presentation_notification_offer_accepted", notificationData);
		OvertakeBusiness(bizManBusiness.buildingRegistration, bizManBusiness);
	}

	public static void OvertakeBusiness(BuildingRegistration buildingRegistration, BizManBusiness bizManBusiness = null)
	{
		List<ScheduleDay> scheduleDays = buildingRegistration.scheduleDays;
		buildingRegistration.scheduleDays = new List<ScheduleDay>();
		foreach (ScheduleDay item in scheduleDays)
		{
			ScheduleDay scheduleDay = new ScheduleDay
			{
				day = item.day,
				isOpen = item.isOpen
			};
			foreach (OpeningHourSlot openingHourSlot in item.openingHourSlots)
			{
				scheduleDay.openingHourSlots.Add(openingHourSlot.Clone());
			}
			if (scheduleDay.openingHourSlots.Count == 0)
			{
				scheduleDay.openingHourSlots.Add(new OpeningHourSlot(8, 16));
			}
			buildingRegistration.scheduleDays.Add(scheduleDay);
		}
		buildingRegistration.AddToPlayer();
		buildingRegistration.GenerateEmployees();
		buildingRegistration.dirtSpots = BuildingCleanlinessHelper.GetDirtSpotsForBuilding(buildingRegistration.BuildingCached);
		bool flag = SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => x.BusinessName == buildingRegistration.BusinessName && x.RentedByPlayer && x != buildingRegistration);
		if (!flag)
		{
			BusinessLogoGenerator.Create(buildingRegistration.BusinessName, buildingRegistration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(buildingRegistration.BusinessName), isPlayerBusiness: true, delegate
			{
				GlobalEvents.onBuildingRegistrationChange?.Invoke(buildingRegistration.Address);
				if (bizManBusiness != null)
				{
					bizManBusiness.SetInitialTab();
					bizManBusiness.RefreshData();
				}
			});
		}
		LicensingFeesHelper.PayLicensingFees(buildingRegistration, noCharge: true);
		BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
		BusinessHelper.GenerateMissingTodoTasksForBusiness(buildingRegistration);
		CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
		BusinessHelper.UpdatePromotion(buildingRegistration);
		GameEvent.Invoke("ba:gameevent_newbusiness");
		CustomerDemandHelper.ReloadCachedFulfilled(buildingRegistration);
		PersonalGoalsUI.UpdatePersonalGoals("ba:gameevent_newbusiness");
		DeliveryJobStartController.OnBusinessChange(buildingRegistration.Address);
		if (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
		{
			foreach (ItemInstance item2 in buildingRegistration.itemInstances.Values.Where((ItemInstance x) => ItemsGetter.GetByName(x.itemName).HasTag(TagRef.Itemtag.issecuritypanel)))
			{
				item2.isSecured = true;
			}
			BusinessSecurityHelper.UpdateCamerasCoverage(buildingRegistration.Address);
			buildingRegistration.UpdateSecurityLevel();
		}
		BizManSchedule.AutoFillSchedule(bizManBusiness);
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			InstanceBehavior<CityManager>.Instance.FindCityBuildingController(buildingRegistration.BuildingCached.Address).UpdatePoi();
			InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		}
		RivalsHelper.CheckRivalTimeline(buildingRegistration.Neighborhood);
		foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
		{
			NeighborhoodDemand neighborhoodDemand = SaveGameManager.Current.productMarketEntries.FirstOrDefault((ProductMarketEntry x) => x.itemName == cachedAvailableProduct)?.demandValues.FirstOrDefault((NeighborhoodDemand x) => x.neighborhood == buildingRegistration.Neighborhood);
			if (neighborhoodDemand != null)
			{
				neighborhoodDemand.providers--;
				neighborhoodDemand.RecalculateIfPlayerHasMonopoly(cachedAvailableProduct);
			}
		}
		if (flag)
		{
			buildingRegistration.BusinessName = "";
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(buildingRegistration.Address, "Settings");
		}
		else
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(buildingRegistration.Address);
		}
	}

	public void SendBuyBuildingOffer()
	{
		Dictionary<string, string> notificationData;
		if (!bizManBusiness.building.requiredDLC.DlcIsOwned())
		{
			notificationData = new Dictionary<string, string> { 
			{
				"dlc",
				bizManBusiness.building.requiredDLC.ToStringFast()
			} };
			Notifications.Show(NotificationType.Error, "common_requires_dlc", notificationData);
			return;
		}
		if (!double.TryParse((bizManBusiness.building.IsHamptonsHouse() ? hamptonsPurchaseBoxUI.offerInputField : buyBuildingAmountInputField).text, out var result) || result <= 0.0)
		{
			Notifications.Show(NotificationType.Error, "common_notification_invalid_amount");
			return;
		}
		if ((double)SaveGameManager.Current.Money < result)
		{
			Notifications.ShowInsufficientMoney();
			return;
		}
		BuildingForSale buildingForSale = SaveGameManager.Current.buildingsForSale.FirstOrDefault((BuildingForSale x) => x.address == bizManBusiness.building.Address);
		double num = ((buildingForSale == null) ? ((double)(bizManBusiness.building.GetMarketValue() * (1f + RivalsHelper.GetBuyBuildingAcceptRate(bizManBusiness.buildingRegistration.buildingOwnerRivalId) / 100f))) : ((double)(buildingForSale.buildingPrice * buildingForSale.acceptOfferRate)));
		if (result < num)
		{
			notificationData = new Dictionary<string, string> { 
			{
				"price",
				result.ToShortCurrencyFormat()
			} };
			Notifications.Show(NotificationType.Error, "bizman_presentation_notification_building_offer_rejected", notificationData);
			return;
		}
		string text = bizManBusiness.buildingRegistration.Address.ToFormattedString();
		Dictionary<string, string> data = new Dictionary<string, string> { { "address", text } };
		bool num2 = BusinessHelper.IsTaxDeductibleBusinessBuilding(bizManBusiness.buildingRegistration);
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_buildingbought", data);
		if (num2)
		{
			transactionInfo.SetTaxDeductibleName(text);
		}
		float amount = 0f - (float)result;
		Address address = bizManBusiness.buildingRegistration.Address;
		GameManager.ChangeMoneySafe(amount, transactionInfo, null, address);
		notificationData = new Dictionary<string, string> { { "address", text } };
		Notifications.Show(NotificationType.Success, "bizman_presentation_notification_building_offer_accepted", notificationData);
		SaveGameManager.Current.realEstate.Add(new RealEstate
		{
			address = bizManBusiness.buildingRegistration.Address,
			purchasePrice = result,
			purchaseDay = SaveGameManager.Current.Day,
			totalSqm = BuildingHelper.GetBuilding(bizManBusiness.buildingRegistration.Address).totalSqm,
			occupancy = UnityEngine.Random.Range(30, 70),
			pricePerSqm = bizManBusiness.building.GetBuildingDailyMarketRentPerSqm()
		});
		if (buildingForSale != null)
		{
			SaveGameManager.Current.buildingsForSale.Remove(buildingForSale);
		}
		if (bizManBusiness.buildingRegistration.RentedByPlayer)
		{
			bizManBusiness.buildingRegistration.RentPerDay = 0f;
		}
		bizManBusiness.buildingRegistration.buildingOwnerRivalId = string.Empty;
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(bizManBusiness.building.Address, "RealEstate");
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			InstanceBehavior<CityManager>.Instance.FindCityBuildingController(bizManBusiness.building.Address).UpdatePoi();
			InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		}
		if (bizManBusiness.buildingRegistration.BuildingCached.IsHamptonsHouse())
		{
			BuildingHelper.RentBuilding(bizManBusiness.building, 0f, 0f);
		}
		GameEvent.Invoke("ba:gameevent_purchasedbuilding");
	}

	public void SetStartBusiness()
	{
		if (SaveGameManager.Current.BuildingRegistrations.Count(delegate(BuildingRegistration x)
		{
			Building building = BuildingHelper.GetBuilding(x.Address);
			return x.RentedByPlayer && x.businessTypeName != "ba:businesstype_empty" && BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.canbetakenover);
		}) > 0 && !EducationHelper.HasCompletedDiploma(DiplomaName.MarketDemands))
		{
			Notifications.ShowError("bizman_presentation_notification_fundamentalbusinessadministration_course_required", "bizman_presentation_notification_fundamentalbusinessadministration_course_required");
		}
		else
		{
			SetActiveView("StartBusiness");
		}
	}

	public void PreviewBuilding()
	{
		if (bizManBusiness.buildingRegistration != null)
		{
			InstanceBehavior<UIs>.Instance.buildingPreview.PreviewBuilding(bizManBusiness.buildingRegistration.BuildingCached);
		}
	}

	private void OnDisable()
	{
		if ((bool)buildingImage.sprite)
		{
			UnityEngine.Object.Destroy(buildingImage.sprite);
		}
	}

	public void ShowBusinessEmployeesList()
	{
		InstanceBehavior<UIs>.Instance.rivalEmployeesUi.Show(bizManBusiness.buildingRegistration);
	}

	[ConsoleMethod("OvertakeRandomBusinesses", "Overtake a number of random businesses", new string[] { })]
	public static void OvertakeRandomBusinesses(int amount)
	{
		IList<BuildingRegistration> list = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && !x.BuildingOwnedByPlayer && BuildingTypeHelper.GetData(x).HasTag(TagRef.Buildingtypetag.canbetakenover) && x.BuildingCached.SpecialService == null && x.businessTypeName != "ba:businesstype_empty").ToList().Shuffle();
		for (int num = 0; num < amount; num++)
		{
			if (num >= list.Count)
			{
				Debug.Log("Not enough businesses to overtake");
				break;
			}
			OvertakeBusiness(list[num]);
		}
	}
}
