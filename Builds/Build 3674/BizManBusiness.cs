using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Entities;
using Enums;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI;
using UI.Elements;
using UI.Guiders;
using UI.Notification;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.BizMan.HRManagers;
using UI.Smartphone.Apps.BizMan.LogisticsManagers;
using UI.Smartphone.Apps.BizMan.PricingManagers;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BizManBusiness : MonoBehaviour
{
	[SerializeField]
	private SplitterIndicator splitterIndicator;

	[SerializeField]
	private Transform menu;

	[SerializeField]
	private Transform containers;

	[SerializeField]
	private Transform businessInfo;

	[SerializeField]
	private BizManBuildingInfo buildingInfo;

	[SerializeField]
	private UI.Elements.Dropdown businessNameDropdown;

	[SerializeField]
	private Image businessLogo;

	[SerializeField]
	private TextLocalizationComponent businessTypeLabel;

	[SerializeField]
	private Transform requirementTemplate;

	[SerializeField]
	private Transform alertTemplate;

	[SerializeField]
	private Toggle temporarilyClosedToggle;

	[SerializeField]
	private GameObject securityInfo;

	[SerializeField]
	private TextLocalizationComponent securityLevelLabel;

	[NonSerialized]
	public Address address;

	[NonSerialized]
	public Building building;

	[NonSerialized]
	public BuildingRegistration buildingRegistration;

	[NonSerialized]
	public bool showTakeoverView;

	public BizManInsight bizManInsight;

	public BizManInventoryPricing bizManInventoryPricing;

	public BizManSettings bizManSettings;

	[FormerlySerializedAs("newBizManSchedule")]
	public BizManSchedule bizManSchedule;

	public BizManMarketing bizManMarketing;

	public PricingManagersPlanList pricingManagersPlanList;

	public LogisticsManagersPlanList logisticsLogisticsManagersPlanList;

	public PurchasingAgentsPlanList purchasingAgentsPlanList;

	public HrManagerPlanUI hrHrManagerPlanUI;

	public HrManagersPlanList hrManagersPlanList;

	private Color32 _defaultTabColor;

	private string _selectedTab = "Presentation";

	private Transform _currentContainer;

	private TextMeshProUGUI _currentTabText;

	private List<BuildingRegistration> _businesses;

	private List<string> _tabs;

	private bool _scheduleLoadBizManAlerts;

	private void Awake()
	{
		_defaultTabColor = menu.GetChild(0).GetComponent<TMP_Text>().color;
		foreach (Transform tab in menu.transform)
		{
			tab.GetComponent<Button>().onClick.AddListener(delegate
			{
				if (bizManSettings.HasUnsavedChanges)
				{
					if (string.IsNullOrEmpty(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.buildingRegistration.BusinessName))
					{
						Notifications.ShowError("bizman_settings_notification_name_empty");
					}
					else
					{
						HudConfirm.Show(null, "change_character_clothes_unsaved_changes_warning", delegate
						{
							SetTab(tab.name);
						});
					}
				}
				else
				{
					SetTab(tab.name);
				}
			});
		}
		temporarilyClosedToggle.onValueChanged.AddListener(delegate(bool closed)
		{
			if (buildingRegistration != null)
			{
				buildingRegistration.TemporarilyClose(closed);
				UpdateSecurityInfo();
				ReloadCurrentFactoryTab();
				ScheduleLoadAlerts();
			}
		});
		businessNameDropdown.onOptionSelected.AddListener(delegate(int index)
		{
			if (string.IsNullOrEmpty(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.buildingRegistration.BusinessName))
			{
				Notifications.ShowError("bizman_settings_notification_name_empty");
				businessNameDropdown.ResetSelectedOption();
			}
			else
			{
				SetAddress(_businesses[index].Address);
				SetUpTabs();
				if (!_tabs.Contains(_selectedTab))
				{
					SetInitialTab(_tabs[1]);
				}
				RefreshData();
			}
		});
	}

	private void OnEnable()
	{
		RefreshData();
	}

	public void Open()
	{
		if (base.gameObject.activeSelf)
		{
			RefreshData();
		}
		else
		{
			base.gameObject.SetActive(value: true);
		}
	}

	public void SetAddress(Address newAddress)
	{
		if (!(address == newAddress))
		{
			address = newAddress;
			building = BuildingHelper.GetBuilding(address);
			buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
			menu.Find("Presentation").GetComponent<TextLocalizationComponent>().SetValue(address.ToFormattedString(), clearKey: true);
		}
	}

	private void SetUpTabs()
	{
		_tabs = new List<string> { "Presentation" };
		if (buildingRegistration.RentedByPlayer)
		{
			if (buildingRegistration.businessTypeName == "ba:businesstype_warehouse")
			{
				_tabs.Add("Drivers");
				_tabs.Add("Inventory");
				_tabs.Add("Settings");
			}
			else if (buildingRegistration.businessTypeName == "ba:businesstype_factory")
			{
				_tabs.Add("Factory");
				_tabs.Add("Schedule");
				_tabs.Add("Drivers");
				_tabs.Add("Inventory");
				_tabs.Add("Settings");
			}
			else if (buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				_tabs.Add("Schedule");
				_tabs.Add("PricingManagers");
				_tabs.Add("LogisticsManagers");
				_tabs.Add("PurchasingAgents");
				_tabs.Add("HRManagers");
				_tabs.Add("Headhunters");
				_tabs.Add("Settings");
			}
			else if (buildingRegistration.businessTypeName != "ba:businesstype_empty" && !BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.containsnobusiness))
			{
				_tabs.Add("Insight");
				_tabs.Add("InventoryPricing");
				_tabs.Add("Schedule");
				if (BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.acceptswholesaledeliveries))
				{
					_tabs.Add("Deliveries");
				}
				_tabs.Add("Marketing");
				_tabs.Add("Settings");
			}
		}
		if (SaveGameManager.Current.realEstate.Exists((RealEstate x) => x.address == address))
		{
			_tabs.Add("RealEstate");
		}
		foreach (Transform item in menu)
		{
			item.gameObject.SetActive(_tabs.Contains(item.name));
		}
	}

	public void SetInitialTab(string tab = null)
	{
		if (tab == null)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName == "ba:businesstype_empty")
			{
				_selectedTab = "Presentation";
			}
			else if (buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				_selectedTab = "PricingManagers";
			}
			else if (buildingRegistration.businessTypeName == "ba:businesstype_factory")
			{
				_selectedTab = "Factory";
			}
			else if (building.BuildingType == "ba:buildingtype_warehouse")
			{
				_selectedTab = "Drivers";
			}
			else if (building.BuildingType == "ba:buildingtype_residential")
			{
				_selectedTab = (SaveGameManager.Current.realEstate.Exists((RealEstate x) => x.address == address) ? "RealEstate" : "Presentation");
			}
			else
			{
				_selectedTab = "Insight";
			}
		}
		else
		{
			_selectedTab = tab;
		}
		if (_selectedTab == "Schedule" && buildingRegistration.businessTypeName == "ba:businesstype_warehouse")
		{
			_selectedTab = "Drivers";
		}
	}

	private void SetTab(string tabName)
	{
		Transform transform = containers.Find(tabName);
		if (transform == null)
		{
			return;
		}
		if (_currentContainer != null)
		{
			_currentContainer.gameObject.SetActive(value: false);
			_currentTabText.color = _defaultTabColor;
		}
		_selectedTab = tabName;
		_currentContainer = transform;
		_currentContainer.gameObject.SetActive(value: true);
		_currentTabText = menu.Find(tabName).GetComponent<TextMeshProUGUI>();
		_currentTabText.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		splitterIndicator.Set(_currentTabText.GetComponent<RectTransform>());
		if (tabName == "RealEstate")
		{
			businessInfo.gameObject.SetActive(value: false);
			if (address != null)
			{
				buildingInfo.ShowInfo();
			}
		}
		else
		{
			buildingInfo.gameObject.SetActive(value: false);
			businessInfo.gameObject.SetActive(tabName != "Presentation");
		}
		switch (tabName)
		{
		case "Insight":
			bizManInsight.RefreshData(buildingRegistration);
			break;
		case "InventoryPricing":
			bizManInventoryPricing.RefreshData();
			break;
		case "Schedule":
			bizManSchedule.LoadScheduler();
			ScheduleLoadAlerts();
			break;
		case "Settings":
			bizManSettings.RefreshData();
			break;
		case "Marketing":
			bizManMarketing.RefreshData();
			break;
		}
	}

	public void RefreshData()
	{
		SetUpTabs();
		SetTab(_selectedTab);
		_businesses = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		int selectedOption = _businesses.IndexOf(buildingRegistration);
		businessNameDropdown.SetOptions(_businesses.Select((BuildingRegistration x) => x.BusinessName).ToList(), localize: false, selectedOption);
		businessTypeLabel.Key = buildingRegistration.businessTypeName;
		temporarilyClosedToggle.transform.parent.gameObject.SetActive(ShouldShowTemporarilyClosedToggle());
		temporarilyClosedToggle.SetIsOnWithoutNotify(buildingRegistration.temporarilyClosed);
		LoadBusinessLogo();
		UpdateSecurityInfo();
		LoadRequirements();
		ScheduleLoadAlerts();
	}

	private static bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		return buildingRegistration.HasEstablishedBusiness;
	}

	private bool ShouldShowTemporarilyClosedToggle()
	{
		if (!BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.generatesrevenue))
		{
			return buildingRegistration.businessTypeName == "ba:businesstype_factory";
		}
		return true;
	}

	private void ReloadCurrentFactoryTab()
	{
		if (!(_selectedTab != "Factory") && !(_currentContainer == null))
		{
			SetTab(_selectedTab);
		}
	}

	public void LoadBusinessLogo()
	{
		businessLogo.color = Color.white;
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(buildingRegistration.BusinessName, LogoSize.SquareSign, buildingRegistration.RentedByPlayer);
		if (businessLogoTexture == null)
		{
			BusinessLogoGenerator.Create(buildingRegistration.BusinessName, buildingRegistration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(buildingRegistration.BusinessName), buildingRegistration.RentedByPlayer, delegate
			{
				Texture2D businessLogoTexture2 = LogoHelper.GetBusinessLogoTexture(buildingRegistration.BusinessName, LogoSize.SquareSign, playerBusiness: true);
				if (businessLogoTexture2 != null)
				{
					SetBusinessLogo(businessLogoTexture2);
				}
			});
		}
		else
		{
			SetBusinessLogo(businessLogoTexture);
		}
	}

	private void SetBusinessLogo(Texture2D texture)
	{
		if (businessLogo.sprite != null)
		{
			UnityEngine.Object.Destroy(businessLogo.sprite);
		}
		Rect rect = new Rect(0f, 0f, texture.width, texture.height);
		businessLogo.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
	}

	public void UpdateSecurityInfo()
	{
		if (!BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
		{
			securityInfo.SetActive(value: false);
			return;
		}
		if (BusinessHelper.IsBusinessOpen(buildingRegistration))
		{
			(string, Color32) tuple = GetSecurityLevelLabel();
			securityLevelLabel.Key = "bizman_security_level";
			securityLevelLabel.Arguments = new
			{
				securityLevel = tuple.Item1,
				securityPercentage = Mathf.FloorToInt(buildingRegistration.securityLevelPercentage)
			};
			securityLevelLabel.TextContainer.color = tuple.Item2;
		}
		else
		{
			securityLevelLabel.Key = "bizman_security_business_closed";
			securityLevelLabel.TextContainer.color = Color.black;
		}
		securityInfo.SetActive(value: true);
	}

	private (string levelKey, Color32 color) GetSecurityLevelLabel()
	{
		float securityLevelPercentage = buildingRegistration.securityLevelPercentage;
		if (securityLevelPercentage < 50f)
		{
			if (securityLevelPercentage < 30f)
			{
				return (levelKey: "bizman_security_level_poor", color: InstanceBehavior<GlobalReferences>.Instance.colors.red);
			}
			return (levelKey: "bizman_security_level_average", color: InstanceBehavior<GlobalReferences>.Instance.colors.orange);
		}
		if (securityLevelPercentage < 90f)
		{
			return (levelKey: "bizman_security_level_good", color: InstanceBehavior<GlobalReferences>.Instance.colors.green);
		}
		return (levelKey: "bizman_security_level_perfect", color: InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen);
	}

	private void LoadRequirements()
	{
		requirementTemplate.ResetTemplate();
		BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
		if (data == null)
		{
			return;
		}
		foreach (BusinessRequirement businessRequirement in data.businessRequirements)
		{
			if (!(businessRequirement is PaidLicensingFees))
			{
				LanguageChangeEventDataHolder data2 = "alert_missing_required_item".Localize(new
				{
					itemname = businessRequirement.GetLocalizeKey()
				});
				int num = 1;
				if (businessRequirement is SpecificItemsInBuildingBySqm specificItemsInBuildingBySqm)
				{
					num = specificItemsInBuildingBySqm.GetRequiredItemCount(buildingRegistration);
				}
				else if (businessRequirement is ItemsOfTypeInBuildingBySqm itemsOfTypeInBuildingBySqm)
				{
					num = itemsOfTypeInBuildingBySqm.GetRequiredItemCount(buildingRegistration);
				}
				if (num > 1)
				{
					data2 = "alert_missing_required_item_count".Localize(new
					{
						itemname = businessRequirement.GetLocalizeKey(),
						count = num
					});
				}
				Transform obj = UnityEngine.Object.Instantiate(requirementTemplate, requirementTemplate.parent);
				obj.GetComponent<TextLocalizationComponent>().SetData(data2);
				obj.GetComponentInChildren<Toggle>().isOn = BusinessHelper.IsRequirementMet(buildingRegistration, businessRequirement);
				HelpIconButton componentInChildren = obj.GetComponentInChildren<HelpIconButton>();
				string helpLink = businessRequirement.GetHelpLink(buildingRegistration);
				if (!string.IsNullOrEmpty(helpLink))
				{
					componentInChildren.linkSlug = helpLink;
					componentInChildren.gameObject.SetActive(value: true);
				}
				else
				{
					componentInChildren.gameObject.SetActive(value: false);
				}
				obj.gameObject.SetActive(value: true);
			}
		}
	}

	public void ScheduleLoadAlerts()
	{
		if (!_scheduleLoadBizManAlerts)
		{
			_scheduleLoadBizManAlerts = true;
			CoroutineUtility.RunAfterOneFrame(LoadAlerts);
		}
	}

	private void LoadAlerts()
	{
		alertTemplate.ResetTemplate();
		foreach (TodoTask item in SaveGameManager.Current.TodoTasks.Where((TodoTask x) => x.address == buildingRegistration.Address))
		{
			Transform obj = UnityEngine.Object.Instantiate(alertTemplate, alertTemplate.parent);
			obj.GetComponent<TextLocalizationComponent>().SetData(TasksUI.GetTodoDescription(item, showBusinessName: false));
			Color32 color = ((item.priority != Priority.High) ? InstanceBehavior<GlobalReferences>.Instance.colors.yellow : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			Color32 color2 = color;
			obj.GetImageByName("Icon").color = color2;
			obj.gameObject.SetActive(value: true);
		}
		_scheduleLoadBizManAlerts = false;
	}

	public void SetDestination()
	{
		if (buildingRegistration != null)
		{
			SetDestination(buildingRegistration.Address);
		}
		else
		{
			Debug.LogError("Can't use SetDestination button due to no registration set up");
		}
	}

	public void SetDestination(Address destinationAddress)
	{
		SaveGameManager.Current.customDestination = destinationAddress;
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"address",
			destinationAddress.ToFormattedString()
		} };
		Notifications.Show(NotificationType.Success, "notification_destination_set", notificationData);
		GuidersManager.SetGuiderTarget(destinationAddress, DirectionGuiderType.Destination);
		InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
	}

	public void OpenInEconoView()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.EconoView);
		InstanceBehavior<UIs>.Instance.fullMenu.econoView.SetBusiness(buildingRegistration);
	}

	private void OnDisable()
	{
		if (_currentContainer != null)
		{
			_currentContainer.gameObject.SetActive(value: false);
			_currentTabText.color = _defaultTabColor;
		}
		_currentContainer = null;
		_currentTabText = null;
		GlobalEvents.onBuildingRegistrationChange?.Invoke(address);
		GameEvent.Invoke("ba:gameevent_changedbuildingregistration");
		address = null;
	}
}
