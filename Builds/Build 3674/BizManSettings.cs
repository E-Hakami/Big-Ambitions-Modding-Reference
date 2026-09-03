using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Steamworks;
using TMPro;
using UI;
using UI.Elements;
using UI.Guiders;
using UI.Notification;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BizManSettings : MonoBehaviour
{
	[SerializeField]
	private Button saveChangesButton;

	[SerializeField]
	private TMP_InputField businessName;

	[SerializeField]
	private UI.Elements.Dropdown businessTypeDropdown;

	[SerializeField]
	private LogoCustomizer logoCustomizer;

	[SerializeField]
	private SetUpUniformsWindow setUpUniformsWindow;

	private BizManBusiness _bizManBusiness;

	private List<string> _playerBusinessTypes;

	private string _selectedBusinessTypeName;

	private bool _usingThisTextInput;

	public bool HasUnsavedChanges
	{
		get
		{
			if (base.gameObject.activeSelf)
			{
				if (!saveChangesButton.interactable && !logoCustomizer.HasUnsavedChanges)
				{
					return string.IsNullOrEmpty(_bizManBusiness.buildingRegistration.BusinessName);
				}
				return true;
			}
			return false;
		}
	}

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
		if (Singleton<SteamAPI>.Instance.steamApiEnabled && SteamUtils.IsSteamInBigPictureMode)
		{
			businessName.onSelect.AddListener(delegate
			{
				_usingThisTextInput = true;
				SteamUtils.ShowGamepadTextInput(GamepadTextInputMode.Normal, GamepadTextInputLineMode.SingleLine, "common_name".GetLocalization(), (businessName.characterLimit == 0) ? 100 : businessName.characterLimit, businessName.text);
			});
			SteamUtils.OnGamepadTextInputDismissed += delegate(bool submitted)
			{
				if (_usingThisTextInput & submitted)
				{
					businessName.text = SteamUtils.GetEnteredGamepadText();
				}
				_usingThisTextInput = false;
				saveChangesButton.interactable = true;
			};
		}
		businessName.onValueChanged.AddListener(delegate
		{
			UpdateSaveButton();
		});
	}

	public void RefreshData()
	{
		setUpUniformsWindow.Init(_bizManBusiness.buildingRegistration);
		saveChangesButton.interactable = false;
		businessName.text = _bizManBusiness.buildingRegistration.BusinessName;
		_selectedBusinessTypeName = _bizManBusiness.buildingRegistration.businessTypeName;
		logoCustomizer.gameObject.SetActive(!BuildingTypeHelper.GetData(_bizManBusiness.buildingRegistration).HasTag(TagRef.Buildingtypetag.hasnologo));
		_playerBusinessTypes = (from x in BusinessTypeHelper.GetAllPlayerAvailableBusinesses()
			where x.suitableBuildingType == _bizManBusiness.building.BuildingType
			select x.businessTypeName).ToList();
		businessTypeDropdown.SetOptions(_playerBusinessTypes.ToList());
		businessTypeDropdown.ResetSelectedOption(_playerBusinessTypes.IndexOf(_bizManBusiness.buildingRegistration.businessTypeName));
		businessTypeDropdown.onOptionSelected.RemoveAllListeners();
		businessTypeDropdown.onOptionSelected.AddListener(delegate(int businessTypeIndex)
		{
			_selectedBusinessTypeName = _playerBusinessTypes[businessTypeIndex];
			UpdateSaveButton();
		});
		UpdateSaveButton();
	}

	public void SetBusinessNameToEmpty()
	{
		businessName.text = "";
	}

	public void RandomizeBusinessName()
	{
		businessName.text = InstanceBehavior<GlobalReferences>.Instance.businessNameGenerator.GenerateName(BusinessTypeHelper.GetData(_selectedBusinessTypeName), _bizManBusiness.buildingRegistration.Neighborhood);
	}

	private void UpdateSaveButton()
	{
		bool interactable = _selectedBusinessTypeName != _bizManBusiness.buildingRegistration.businessTypeName || businessName.text != _bizManBusiness.buildingRegistration.BusinessName;
		saveChangesButton.interactable = interactable;
	}

	public void SaveBusinessInformation()
	{
		DiplomaName courseRequired = BusinessTypeHelper.GetData(_selectedBusinessTypeName).courseRequired;
		if (courseRequired != DiplomaName.Undefined && !EducationHelper.HasCompletedDiploma(courseRequired))
		{
			EducationHelper.ShowCourseRequiredNotification(_selectedBusinessTypeName, courseRequired);
			return;
		}
		if (string.IsNullOrWhiteSpace(businessName.text))
		{
			Notifications.ShowError("bizman_settings_notification_name_empty");
			return;
		}
		businessName.text = businessName.text.Trim();
		if (businessName.text != _bizManBusiness.buildingRegistration.BusinessName)
		{
			if (businessName.text.StartsWith("."))
			{
				Notifications.ShowError("bizman_notification_name_cannot_start_dot", "bizman_notification_name_cannot_start_dot", trackOnSaveGame: false);
				return;
			}
			if (businessName.text.Any((char x) => Path.GetInvalidFileNameChars().Contains(x)))
			{
				Notifications.ShowError("bizman_notification_invalid_business_name_characters", "name_invalid_characters", trackOnSaveGame: false);
				return;
			}
			if (SaveGameManager.Current.BuildingRegistrations.Exists((BuildingRegistration x) => x.BusinessName == businessName.text))
			{
				Notifications.ShowError("bizman_settings_notification_name_duplicated");
				return;
			}
			RenameLogoSpriteFolder(_bizManBusiness.buildingRegistration.BusinessName, businessName.text);
			_bizManBusiness.buildingRegistration.BusinessName = businessName.text;
		}
		bool flag = _bizManBusiness.building.BuildingType != "ba:buildingtype_warehouse";
		string businessTypeName = _bizManBusiness.buildingRegistration.businessTypeName;
		if (businessTypeName != _selectedBusinessTypeName)
		{
			ChangeBusinessType(businessTypeName);
		}
		BusinessHelper.UpdateCustomerCapacity(_bizManBusiness.buildingRegistration);
		CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(_bizManBusiness.buildingRegistration, TimeHelper.GetDayOfWeek());
		TasksUI.UpdateTasksFromBusiness(_bizManBusiness.buildingRegistration);
		_bizManBusiness.RefreshData();
		if (flag)
		{
			logoCustomizer.Refresh();
		}
		Notifications.Show(NotificationType.Success, "bizman_settings_notification_save_successfull");
		GameEvent.Invoke("ba:gameevent_newbusiness");
		GuidersManager.UpdateGuidersWithAddress(_bizManBusiness.buildingRegistration.Address);
		InstanceBehavior<CityManager>.Instance.FindCityBuildingController(_bizManBusiness.buildingRegistration.Address)?.UpdatePoi();
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.buildingRegistration == _bizManBusiness.buildingRegistration)
		{
			InstanceBehavior<BuildingManager>.Instance.businessType = BusinessTypeHelper.GetData(_bizManBusiness);
		}
		GenerateNewLogo(flag);
	}

	private void ChangeBusinessType(string previousBusinessTypeName)
	{
		BizManSchedule.AbortAutoFillForBusiness(_bizManBusiness.buildingRegistration);
		BusinessType data = BusinessTypeHelper.GetData(_selectedBusinessTypeName);
		List<string> list = data.employeePrimarySkills.ToList();
		list.AddRange(BuildingTypeHelper.GetData(_bizManBusiness.building).requiredBuildingSkills);
		if (!data.HasTag(TagRef.Businesstag.allowtheft))
		{
			list.Remove("ba:skill_securityguard");
		}
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = _bizManBusiness.buildingRegistration.Address,
			withoutSkills = list.ToArray()
		}))
		{
			EmployeeHelper.UnassignEmployeeFromAllWorkshifts(employeeInstance);
		}
		if (previousBusinessTypeName == "ba:businesstype_headquarters")
		{
			BusinessHelper.DeleteHQPlans(_bizManBusiness.buildingRegistration);
		}
		LogisticsManagerHelper.CancelAllDeliveriesForAddress(_bizManBusiness.buildingRegistration.Address);
		if (_selectedBusinessTypeName == "ba:businesstype_headquarters")
		{
			HappinessHelper.AddModifier("ba:happinessmodifier_started_a_headquarters");
			_bizManBusiness.buildingRegistration.marketingCampaigns.Clear();
			_bizManBusiness.buildingRegistration.TemporarilyClose(closed: false);
			BusinessHelper.UpdatePromotion(_bizManBusiness.buildingRegistration);
			_bizManBusiness.buildingRegistration.scheduleDays = new List<ScheduleDay>
			{
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Monday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = true
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Tuesday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = true
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Wednesday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = true
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Thursday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = true
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Friday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = true
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Saturday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = false
				},
				new ScheduleDay
				{
					day = DayOfWeekOrdered.Sunday,
					openingHourSlots = new List<OpeningHourSlot>
					{
						new OpeningHourSlot(8, 16)
					},
					isOpen = false
				}
			};
		}
		_bizManBusiness.buildingRegistration.businessTypeName = _selectedBusinessTypeName;
		_bizManBusiness.buildingRegistration.OnBusinessTypeChanged();
		UpdateSaveButton();
		bool flag = data.HasTag(TagRef.Businesstag.customersneedpaperbags);
		bool flag2 = data.HasTag(TagRef.Businesstag.customersneedpaperbags);
		if (flag == flag2)
		{
			return;
		}
		foreach (ItemInstance item in _bizManBusiness.buildingRegistration.itemInstances.Values.Where((ItemInstance x) => (x.ItemCached.type & ItemType.PointOfSale) != 0).ToList())
		{
			CargoInstance stockInstance = item.GetStockInstance();
			if (flag2)
			{
				stockInstance.itemName = "ba:itemname_paperbag";
				stockInstance.ResetItemCached();
				item.FillUpShowcaseShelfOrPointOfSale();
			}
			else
			{
				if (string.IsNullOrEmpty(stockInstance.itemName))
				{
					continue;
				}
				if (stockInstance.amount > 0)
				{
					CargoInstance cargoInstance = new CargoInstance(stockInstance.itemName, stockInstance.amount, stockInstance.pricePerUnit);
					if (cargoInstance.ReturnToAShelf(item.AddressCached))
					{
						stockInstance.amount = 0;
					}
					else
					{
						stockInstance.amount = cargoInstance.amount;
						stockInstance.itemName = cargoInstance.itemName;
						stockInstance.ResetItemCached();
						Debug.LogWarning("Couldn't return all PaperBags to a " + item.itemName + " when changing to a business type that don't require PaperBags. Keeping them into the CashRegister");
					}
				}
				stockInstance.itemName = null;
			}
		}
	}

	public static void RenameLogoSpriteFolder(string oldName, string newName)
	{
		if (Directory.Exists(LogoHelper.GetPlayerBusinessLogoPath(oldName)))
		{
			string playerBusinessLogoPath = LogoHelper.GetPlayerBusinessLogoPath(oldName);
			string playerBusinessLogoPath2 = LogoHelper.GetPlayerBusinessLogoPath(newName);
			string text = Path.Combine(SaveGamePathHelper.CurrentVersionFolderPath(), "TempSpriteFolder");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			Directory.Move(playerBusinessLogoPath, text);
			Directory.Move(text, playerBusinessLogoPath2);
		}
	}

	private void GenerateNewLogo(bool isNotWarehouse)
	{
		if (isNotWarehouse)
		{
			BusinessLogoGenerator.Create(_bizManBusiness.buildingRegistration.BusinessName, _bizManBusiness.buildingRegistration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(_bizManBusiness.buildingRegistration.BusinessName), _bizManBusiness.buildingRegistration.RentedByPlayer, delegate
			{
				GlobalEvents.onBuildingRegistrationChange?.Invoke(_bizManBusiness.buildingRegistration.Address);
				InstanceBehavior<CityManager>.Instance.UpdateBillboardsFromBusiness(_bizManBusiness.buildingRegistration.BusinessName);
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.LoadBusinessLogo();
			});
		}
		else
		{
			BusinessLogoGenerator.Instance.GenerateWarehouseLogo(_bizManBusiness.buildingRegistration.BusinessName, BusinessTypeHelper.GetData(_bizManBusiness), LogoHelper.GetPlayerBusinessLogoPath(_bizManBusiness.buildingRegistration.BusinessName), _bizManBusiness.buildingRegistration.RentedByPlayer);
			GlobalEvents.onBuildingRegistrationChange?.Invoke(_bizManBusiness.buildingRegistration.Address);
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.LoadBusinessLogo();
		}
	}

	public void ShutdownBusiness()
	{
		LanguageChangeEventDataHolder bodyData = "notification_confirm_shut_down_business".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			saveChangesButton.interactable = false;
			if (Directory.Exists(LogoHelper.GetPlayerBusinessLogoPath(_bizManBusiness.buildingRegistration.BusinessName)))
			{
				Directory.Delete(LogoHelper.GetPlayerBusinessLogoPath(_bizManBusiness.buildingRegistration.BusinessName), recursive: true);
			}
			BusinessHelper.ShutdownBusiness(_bizManBusiness.buildingRegistration);
			_bizManBusiness.SetInitialTab();
			_bizManBusiness.RefreshData();
			GuidersManager.UpdateGuidersWithAddress(_bizManBusiness.buildingRegistration.Address);
			InstanceBehavior<CityManager>.Instance.FindCityBuildingController(_bizManBusiness.building.Address)?.UpdatePoi();
			InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
			GlobalEvents.onBuildingRegistrationChange?.Invoke(_bizManBusiness.address);
		});
	}
}
