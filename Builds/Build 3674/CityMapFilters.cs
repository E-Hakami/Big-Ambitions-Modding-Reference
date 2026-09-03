using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.Neighborhoods;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Buildings;
using City.CityMap;
using Entities;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using Player.FoodDeliveryJob;
using Streets;
using TMPro;
using UI.Elements;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.UI;
using Vehicles.DeliveryDriverJob;

public class CityMapFilters : MonoBehaviour
{
	[SerializeField]
	private Transform filterEntry;

	[SerializeField]
	private CityMapFilterCategory lineEntry;

	[SerializeField]
	private TextLocalizationComponent closeLabel;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform topControls;

	[SerializeField]
	private UI.Elements.Dropdown searchDropdown;

	[SerializeField]
	private Color toggleAllTextColor = Color.white;

	[SerializeField]
	private TextAlignmentOptions toggleAllTextAlignment;

	public Sprite playerIcon;

	public Color playerFilterColor;

	public Sprite subwayStationIcon;

	public Sprite deliveryJobIcon;

	public Sprite customerServiceJobIcon;

	public Color subwayStationFilterColor;

	public Sprite forSaleIcon;

	public Color forSaleFilterColor;

	public Sprite undergroundParkingIcon;

	public Color undergroundParkingFilterColor;

	public Sprite boatIcon;

	public Color boatFilterColor;

	public Sprite openNowIcon;

	public Sprite playerOwnedIcon;

	public Sprite golfActivityIcon;

	public Sprite tennisActivityIcon;

	public Sprite partyPierActivityIcon;

	private readonly Dictionary<string, CityMapFilter> _filterEntries = new Dictionary<string, CityMapFilter>();

	private readonly Dictionary<string, Toggle> _filterToggles = new Dictionary<string, Toggle>();

	private readonly List<CityMapFilterCategory> _filterCategories = new List<CityMapFilterCategory>();

	private readonly HashSet<string> _visibleNeighborhoods = new HashSet<string>();

	private readonly HashSet<string> _visibleBuildingTypes = new HashSet<string>();

	private readonly HashSet<string> _visibleBusinesses = new HashSet<string>();

	private readonly HashSet<Address> _visibleAddresses = new HashSet<Address>();

	private readonly List<CityBuildingController> _searchableBuildings = new List<CityBuildingController>();

	private readonly List<PointOfInterest> _permanentPoisHidden = new List<PointOfInterest>();

	private readonly HashSet<string> _visibleRivalIds = new HashSet<string>();

	private string _filterSearchText;

	private Toggle _toggleAll;

	private bool _initialized;

	private void Start()
	{
		_initialized = true;
		InitializeFilters();
		SetUpKeysLabels();
		searchDropdown.onOptionSelected.AddListener(OnDropdownValueChanged);
		searchDropdown.onSearchTextChanged.AddListener(OnSearchTextChanged);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		GlobalEvents.onScreenshotModeToggled = (Action<bool>)Delegate.Combine(GlobalEvents.onScreenshotModeToggled, (Action<bool>)delegate(bool visible)
		{
			canvasGroup.alpha = (visible ? 1 : 0);
		});
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(PopulateSearchDropdown));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool visible)
		{
			if (visible)
			{
				LoadFilters(SaveGameManager.Current.SelectedCitymapFilters.Copy());
			}
			else
			{
				ShowHiddenPermanentPois();
			}
		});
		base.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		if (_initialized && _searchableBuildings.Count == 0)
		{
			PopulateSearchDropdown();
		}
	}

	private void OnDestroy()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(PopulateSearchDropdown));
	}

	private void SetUpKeysLabels()
	{
		closeLabel.Suffix = PlayerAction.Cancel.AsSuffix();
	}

	public void CloseCityMap()
	{
		if (CityMap.IsOpen)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
		}
	}

	public void ResetSearchText()
	{
		searchDropdown.ResetSearch();
	}

	public CityMapFilterCategory GetCategory(string categoryName)
	{
		return _filterCategories.Find((CityMapFilterCategory x) => x.name == categoryName);
	}

	public void ToggleFilter(string filterName, bool isOn)
	{
		if (!_filterToggles.TryGetValue(filterName, out var value))
		{
			Debug.LogWarning("Filter \"" + filterName + "\" not found");
		}
		else
		{
			value.isOn = isOn;
		}
	}

	private void InitializeFilters()
	{
		_filterEntries.Clear();
		_filterToggles.Clear();
		filterEntry.ClearClones();
		filterEntry.gameObject.SetActive(value: false);
		lineEntry.transform.ClearClones();
		lineEntry.gameObject.SetActive(value: false);
		CreateToggleAll();
		CreateStatusCategory();
		CreateJobsCategory();
		CreateBuildingTypesCategory();
		CreateSpecialBuildingsCategory();
		CreateRealEstateCategory();
		CreateActivitiesCategory();
		CreateNeighborhoodsCategory();
		CreateActiveBusinessesCategory();
	}

	private void LoadFilters(List<string> filters)
	{
		foreach (string filter in filters)
		{
			if (_filterToggles.TryGetValue(filter, out var value))
			{
				value.SetIsOnWithoutNotify(value: true);
				value.onValueChanged.Invoke(arg0: true);
			}
		}
	}

	private void CreateStatusCategory()
	{
		CityMapFilterCategory category = CreateLine("bizman_status");
		CreateFilter("buildingresume_rented_by_you", playerOwnedIcon, null, category);
		CreateSpecialRivalFilters(category);
		CreateFilter("citymap_filter_hide_closed", openNowIcon, null, category).ApplyAlternativeToggleSprite();
	}

	private void CreateSpecialRivalFilters(CityMapFilterCategory category)
	{
		foreach (RivalData allRivalDatum in RivalsHelper.GetAllRivalData())
		{
			string rivalId = allRivalDatum.id;
			if (!rivalId.IsSpecialRival())
			{
				continue;
			}
			CreateFilter("rival_" + rivalId, playerOwnedIcon, new CityMapFilterData
			{
				rivalId = rivalId,
				isAvailable = delegate
				{
					if (!RivalsHelper.IsFeatureEnabled)
					{
						return false;
					}
					SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(rivalId);
					return specialRivalState != null && specialRivalState.isActive && !specialRivalState.isDefeated;
				}
			}, category, new LanguageChangeEventDataHolder
			{
				Key = "citymap_filter_rival_rented",
				Arguments = new { allRivalDatum.rivalName }
			});
		}
	}

	private void CreateJobsCategory()
	{
		CityMapFilterCategory category = CreateLine("citymap_category_jobs");
		CreateFilter("ba:skill_customerservice", customerServiceJobIcon, null, category);
		CreateFilter("ba:skill_deliverydriver", deliveryJobIcon, null, category);
		CreateFilter("food_delivery_job_title", InstanceBehavior<GlobalReferences>.Instance.foodDeliveryJobConfig.MapIcon, null, category);
	}

	private void CreateBuildingTypesCategory()
	{
		CityMapFilterCategory category = CreateLine("citymap_category_rentable");
		foreach (KeyValuePair<string, BuildingTypeData> buildingType in BuildingTypeHelper.BuildingTypes)
		{
			if (buildingType.Value.hasCityMapFilter)
			{
				CreateFilter(buildingType.Key.ToLower(), buildingType.Value.poiIcon, new CityMapFilterData
				{
					buildingType = buildingType.Key
				}, category);
			}
		}
	}

	private void CreateSpecialBuildingsCategory()
	{
		CityMapFilterCategory category = CreateLine("citymap_category_special_buildings");
		CreateFilter("common_subwaystation", subwayStationIcon, null, category);
		foreach (BusinessType specialBusiness in BusinessTypeHelper.GetSpecialBusinesses())
		{
			CreateFilter(specialBusiness.businessTypeName.ToLower(), specialBusiness.icon, new CityMapFilterData
			{
				businessTypeName = specialBusiness.businessTypeName
			}, category);
		}
	}

	private void CreateActivitiesCategory()
	{
		CityMapFilterCategory category = CreateLine("citymap_category_activities");
		CreateAddressFilter("citymap_activity_golf", "1 tur", golfActivityIcon, category);
		CreateAddressFilter("citymap_activity_tennis", "2 tur", tennisActivityIcon, category);
		CreateAddressFilter("citymap_activity_party_pier", "5 pier", partyPierActivityIcon, category);
	}

	private void CreateAddressFilter(string labelKey, string address, Sprite icon, CityMapFilterCategory category)
	{
		Address parsedAddress = BuildingHelper.ParseAddressString(address);
		CityMapFilterData filterData = new CityMapFilterData
		{
			address = parsedAddress
		};
		CreateFilter(labelKey, icon, filterData, category, default(LanguageChangeEventDataHolder), null, delegate
		{
			Transform addressEntranceTransform = BuildingHelper.GetAddressEntranceTransform(parsedAddress);
			return (!addressEntranceTransform) ? ((Vector3?)null) : new Vector3?(addressEntranceTransform.position);
		});
	}

	private void CreateRealEstateCategory()
	{
		CityMapFilterCategory category = CreateLine("common_real_estate");
		CreateFilter("common_buildings_for_sale", forSaleIcon, null, category);
	}

	private void CreateNeighborhoodsCategory()
	{
		CityMapFilterCategory category = CreateLine("common_neighborhoods");
		NeighborhoodData[] neighborhoodsData = NeighborhoodHelper.NeighborhoodsData;
		foreach (NeighborhoodData neighborhoodData in neighborhoodsData)
		{
			if (neighborhoodData.hasCityMapFilter)
			{
				Vector3? neighborhoodFocusPoint = GetNeighborhoodFocusPoint(neighborhoodData.neighbourhood);
				string neighbourhood = neighborhoodData.neighbourhood;
				Sprite cityMapIcon = neighborhoodData.cityMapIcon;
				CityMapFilterData filterData = new CityMapFilterData
				{
					neighbourhood = neighborhoodData.neighbourhood
				};
				Vector3? focusPoint = neighborhoodFocusPoint;
				CreateFilter(neighbourhood, cityMapIcon, filterData, category, default(LanguageChangeEventDataHolder), focusPoint);
			}
		}
	}

	private void CreateActiveBusinessesCategory()
	{
		CityMapFilterCategory category = CreateLine("citymap_category_active_businesses");
		foreach (BusinessType item in from x in BusinessTypeHelper.GetAllPlayerAvailableBusinesses()
			where x.HasTag(TagRef.Businesstag.generatesrevenue)
			select x)
		{
			CreateFilter(item.businessTypeName.ToLower(), item.icon, new CityMapFilterData
			{
				businessTypeName = item.businessTypeName
			}, category);
		}
	}

	private CityMapFilterCategory CreateLine(string key)
	{
		CityMapFilterCategory cityMapFilterCategory = UnityEngine.Object.Instantiate(lineEntry, lineEntry.transform.parent);
		cityMapFilterCategory.name = key;
		cityMapFilterCategory.SetUp(key);
		cityMapFilterCategory.gameObject.SetActive(value: true);
		_filterCategories.Add(cityMapFilterCategory);
		return cityMapFilterCategory;
	}

	private void CreateToggleAll()
	{
		Transform obj = UnityEngine.Object.Instantiate(filterEntry, topControls);
		CityMapFilter component = obj.GetComponent<CityMapFilter>();
		component.SetUp("citymap_map_filters_toggle_all", ToggleAllFilters, null, null, excludeFromSelection: true);
		component.SetLabelColor(toggleAllTextColor);
		component.SetLabelAlignment(toggleAllTextAlignment);
		_toggleAll = component.Toggle;
		obj.gameObject.SetActive(value: true);
	}

	private CityMapFilter CreateFilter(string filterName, Sprite filterIcon, CityMapFilterData filterData, CityMapFilterCategory category, LanguageChangeEventDataHolder languageData = default(LanguageChangeEventDataHolder), Vector3? focusPoint = null, Func<Vector3?> focusPointResolver = null)
	{
		Transform obj = UnityEngine.Object.Instantiate(filterEntry, filterEntry.parent);
		obj.name = filterName;
		CityMapFilter component = obj.GetComponent<CityMapFilter>();
		component.SetUp(filterName, OnFilterToggled, filterIcon, filterData, excludeFromSelection: false, languageData, focusPoint, focusPointResolver);
		_filterEntries.Add(filterName, component);
		_filterToggles.Add(filterName, component.Toggle);
		category.AddFilter(component);
		return component;
	}

	private static Vector3? GetNeighborhoodFocusPoint(string neighborhood)
	{
		CityMapNeighborhoodZones.CityMapNeighborhoodZone neighbourhoodZone = CityMapNeighborhoodZones.GetNeighbourhoodZone(neighborhood);
		if (neighbourhoodZone?.zoneRenderer == null)
		{
			return null;
		}
		return neighbourhoodZone.zoneRenderer.bounds.center;
	}

	private bool IsFilterAvailable(string filterName)
	{
		if (_filterEntries.TryGetValue(filterName, out var value))
		{
			return value.IsAvailable();
		}
		return false;
	}

	private void OnFilterToggled(bool isOn)
	{
		ApplyFilters();
		if ((bool)_toggleAll)
		{
			bool isOnWithoutNotify = _filterToggles.Values.Any((Toggle x) => x.isOn);
			_toggleAll.SetIsOnWithoutNotify(isOnWithoutNotify);
		}
	}

	public void ApplyFilters()
	{
		if (!CityMap.IsOpen || FullMenu.IsOpen)
		{
			return;
		}
		UpdateFilterVisibility();
		_visibleRivalIds.Clear();
		foreach (KeyValuePair<string, CityMapFilter> filterEntry in _filterEntries)
		{
			if (IsFilterSelected(filterEntry.Key) && filterEntry.Value.Data?.rivalId != null)
			{
				_visibleRivalIds.Add(filterEntry.Value.Data.rivalId);
			}
		}
		InstanceBehavior<CityManager>.Instance.cityMap.ToggleSubwayStations(IsFilterSelected("common_subwaystation"));
		DeliveryJobStartController.UpdateMapPois();
		UpdateVisibleNeighborhoods();
		CityMapNeighborhoodZones.CityMapNeighborhoodZone[] zones = CityMapNeighborhoodZones.Zones;
		foreach (CityMapNeighborhoodZones.CityMapNeighborhoodZone cityMapNeighborhoodZone in zones)
		{
			if (_visibleNeighborhoods.Contains(cityMapNeighborhoodZone.neighbourhood))
			{
				cityMapNeighborhoodZone.Show();
			}
			else
			{
				cityMapNeighborhoodZone.Hide();
			}
		}
		_permanentPoisHidden.Clear();
		UpdateVisibleBuildingTypes();
		UpdateVisibleBusinesses();
		UpdateVisibleAddresses();
		bool flag = IsFilterSelected("common_buildings_for_sale");
		bool flag2 = IsFilterSelected("buildingresume_rented_by_you");
		bool flag3 = IsFilterSelected("citymap_filter_hide_closed");
		bool flag4 = IsFilterSelected("ba:skill_customerservice");
		bool flag5 = IsFilterSelected("food_delivery_job_title");
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			bool flag6 = IsBuildingForSale(cityBuildingController);
			bool flag7 = cityBuildingController.buildingRegistration.RentedByPlayer || cityBuildingController.buildingRegistration.BuildingOwnedByPlayer;
			bool flag8 = flag5 && FoodDeliveryJobHelper.HasActiveOffer(cityBuildingController.building.Address);
			bool flag9 = ((flag2 & flag7) || (cityBuildingController.buildingRegistration.AvailableForRent && _visibleBuildingTypes.Contains(cityBuildingController.building.BuildingType)) || _visibleBusinesses.Contains(cityBuildingController.buildingRegistration.businessTypeName) || _visibleAddresses.Contains(cityBuildingController.building.Address) || _visibleRivalIds.Contains(cityBuildingController.buildingRegistration.businessOwnerRivalId) || (flag & flag6)) | flag8;
			if ((flag9 & flag3) && !flag6 && !cityBuildingController.buildingRegistration.AvailableForRent && cityBuildingController.building.BuildingType != "ba:buildingtype_residential")
			{
				flag9 = BusinessHelper.IsBusinessOpen(cityBuildingController.buildingRegistration);
			}
			if (flag4)
			{
				Job job = cityBuildingController.building.SpecialService?.playerJob;
				if (job != null && job.scheduleDays.Count > 0)
				{
					flag9 = true;
				}
			}
			if (flag9)
			{
				BuildingTypeData data = BuildingTypeHelper.GetData(cityBuildingController.building);
				Color color = data.mapFilterColor;
				Sprite icon = data.poiIcon;
				if (cityBuildingController.buildingRegistration.businessTypeName != "ba:businesstype_empty")
				{
					BusinessType data2 = BusinessTypeHelper.GetData(cityBuildingController.buildingRegistration);
					color = data2.cityMapFilterColor;
					icon = ((!(cityBuildingController.building.SpecialService != null) || !(cityBuildingController.building.SpecialService.overridePoiIcon != null)) ? data2.icon : cityBuildingController.building.SpecialService.overridePoiIcon);
				}
				else if (flag6 & flag)
				{
					color = forSaleFilterColor;
					icon = forSaleIcon;
				}
				if (cityBuildingController.poi == null)
				{
					cityBuildingController.CreatePOI();
				}
				else if (cityBuildingController.poi.Permanent)
				{
					cityBuildingController.poi.SetHidden(hide: false);
				}
				cityBuildingController.SetHighlight(isOn: true, color);
				cityBuildingController.poi.SetIcon(icon, color);
			}
			else
			{
				cityBuildingController.SetHighlight(isOn: false);
				if (cityBuildingController.poi != null && cityBuildingController.poi.Permanent && (!cityBuildingController.building.IsHamptonsHouse() || InstanceBehavior<BuildingManager>.Instance.building != cityBuildingController.building))
				{
					cityBuildingController.poi.SetHidden(hide: true);
					_permanentPoisHidden.Add(cityBuildingController.poi);
				}
			}
			if (cityBuildingController.poi != null)
			{
				cityBuildingController.poi.SetFoodDeliveryStatus(flag8);
			}
		}
	}

	private void ShowHiddenPermanentPois()
	{
		foreach (PointOfInterest item in _permanentPoisHidden)
		{
			item.SetHidden(hide: false);
		}
	}

	public static void HideAllOutlines()
	{
		InstanceBehavior<CityManager>.Instance.cityMap.ToggleSubwayStations(isOn: false);
		CityMapNeighborhoodZones.HideAllZones();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		for (int i = 0; i < cityBuildingControllers.Length; i++)
		{
			cityBuildingControllers[i].ClearHighlight();
		}
	}

	private bool IsFilterSelected(string filterName)
	{
		if (_filterToggles.TryGetValue(filterName, out var value) && value.isOn)
		{
			return IsFilterAvailable(filterName);
		}
		return false;
	}

	private void OnSearchTextChanged(string searchText)
	{
		_filterSearchText = searchText;
		UpdateFilterVisibility();
	}

	private void UpdateFilterVisibility()
	{
		foreach (CityMapFilterCategory filterCategory in _filterCategories)
		{
			filterCategory.ApplySearch(_filterSearchText);
		}
	}

	private void UpdateVisibleNeighborhoods()
	{
		_visibleNeighborhoods.Clear();
		foreach (KeyValuePair<string, Toggle> filterToggle in _filterToggles)
		{
			if (IsFilterSelected(filterToggle.Key))
			{
				CityMapFilterData data = _filterEntries[filterToggle.Key].Data;
				if (data != null && data.neighbourhood != null)
				{
					_visibleNeighborhoods.Add(data.neighbourhood);
				}
			}
		}
	}

	private void UpdateVisibleBuildingTypes()
	{
		_visibleBuildingTypes.Clear();
		foreach (KeyValuePair<string, Toggle> filterToggle in _filterToggles)
		{
			if (IsFilterSelected(filterToggle.Key))
			{
				CityMapFilterData data = _filterEntries[filterToggle.Key].Data;
				if (data != null && data.buildingType != null)
				{
					_visibleBuildingTypes.Add(data.buildingType);
				}
			}
		}
	}

	private void UpdateVisibleBusinesses()
	{
		_visibleBusinesses.Clear();
		foreach (KeyValuePair<string, Toggle> filterToggle in _filterToggles)
		{
			if (IsFilterSelected(filterToggle.Key))
			{
				CityMapFilterData data = _filterEntries[filterToggle.Key].Data;
				if (data != null && data.businessTypeName != null)
				{
					_visibleBusinesses.Add(data.businessTypeName);
				}
			}
		}
	}

	private void UpdateVisibleAddresses()
	{
		_visibleAddresses.Clear();
		foreach (KeyValuePair<string, Toggle> filterToggle in _filterToggles)
		{
			if (IsFilterSelected(filterToggle.Key))
			{
				CityMapFilterData data = _filterEntries[filterToggle.Key].Data;
				if (data?.address != null)
				{
					_visibleAddresses.Add(data.address);
				}
			}
		}
	}

	private static bool IsBuildingForSale(CityBuildingController building)
	{
		if (SaveGameManager.Current.buildingsForSale.Exists((BuildingForSale x) => x.address == building.building.Address))
		{
			return !SaveGameManager.Current.realEstate.Exists((RealEstate x) => x.address == building.building.Address);
		}
		return false;
	}

	private void PopulateSearchDropdown(Address _ = null)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		_searchableBuildings.Clear();
		_searchableBuildings.Capacity = InstanceBehavior<CityManager>.Instance.cityBuildingControllers.Length;
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			string businessName = cityBuildingController.buildingRegistration.BusinessName;
			if (!string.IsNullOrWhiteSpace(businessName))
			{
				list.Add("<b><noparse>" + businessName + "</noparse></b><br><noparse>" + cityBuildingController.building.Address.ToFormattedString() + "</noparse>");
				list2.Add(businessName);
				_searchableBuildings.Add(cityBuildingController);
			}
		}
		searchDropdown.SetPlaceholder("citymap_map_search_placeholder");
		searchDropdown.SetOptions(list, localize: false, -1, list2);
	}

	private void OnDropdownValueChanged(int index)
	{
		if (index >= 0)
		{
			if (index < _searchableBuildings.Count && (bool)_searchableBuildings[index])
			{
				InstanceBehavior<CityManager>.Instance.cityMap.FocusOnBuilding(_searchableBuildings[index]);
			}
			OnSearchTextChanged(string.Empty);
			searchDropdown.SelectOption(-1);
		}
	}

	private void ToggleAllFilters(bool isOn)
	{
		SaveGameManager.Current.SelectedCitymapFilters.Clear();
		foreach (KeyValuePair<string, Toggle> filterToggle in _filterToggles)
		{
			filterToggle.Value.SetIsOnWithoutNotify(isOn);
			if (isOn)
			{
				SaveGameManager.Current.SelectedCitymapFilters.Add(filterToggle.Key);
			}
		}
		foreach (CityMapFilterCategory filterCategory in _filterCategories)
		{
			filterCategory.ToggleAll.SetIsOnWithoutNotify(isOn);
		}
		ApplyFilters();
	}

	public void Toggle(bool show)
	{
		base.gameObject.SetActive(show);
	}
}
