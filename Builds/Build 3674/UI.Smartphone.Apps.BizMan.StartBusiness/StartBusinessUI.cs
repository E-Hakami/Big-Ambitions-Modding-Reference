using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Steamworks;
using TMPro;
using UI.Components;
using UI.Notification;
using UI.Smartphone.Apps.Persona;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.StartBusiness;

public class StartBusinessUI : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField businessNameField;

	[SerializeField]
	private StartBusinessTypeUI businessTypeTemplate;

	[SerializeField]
	private Transform businessesContainer;

	[SerializeField]
	private Scrollbar scrollbar;

	[SerializeField]
	private GameObject[] retailOnlyElements;

	private readonly Dictionary<string, StartBusinessTypeUI> _businessTypes = new Dictionary<string, StartBusinessTypeUI>();

	private string _selectedType;

	private bool _usingThisTextInput;

	private BuildingRegistration _buildingRegistration;

	public event Action OnBusinessStarted;

	private void Awake()
	{
		KeyboardInputHelper.Configure(businessNameField, SetUpBusiness);
		if (Singleton<SteamAPI>.Instance.steamApiEnabled && SteamUtils.IsSteamInBigPictureMode)
		{
			SetupSteamInput();
		}
	}

	private void SetupSteamInput()
	{
		businessNameField.onSelect.AddListener(delegate
		{
			_usingThisTextInput = true;
			SteamUtils.ShowGamepadTextInput(GamepadTextInputMode.Normal, GamepadTextInputLineMode.SingleLine, "common_name".GetLocalization(), (businessNameField.characterLimit == 0) ? 100 : businessNameField.characterLimit, businessNameField.text);
		});
		SteamUtils.OnGamepadTextInputDismissed += delegate(bool submitted)
		{
			if (_usingThisTextInput & submitted)
			{
				businessNameField.text = SteamUtils.GetEnteredGamepadText();
			}
			_usingThisTextInput = false;
		};
	}

	public void Show(BuildingRegistration buildingRegistration)
	{
		_buildingRegistration = buildingRegistration;
		_selectedType = "ba:businesstype_empty";
		base.gameObject.SetActive(value: true);
		Load();
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
		_buildingRegistration = null;
	}

	private void Load()
	{
		businessNameField.text = "";
		businessesContainer.DestroyAllChildren();
		ResetBusinessTypes();
		UpdateRetailOnlyVisuals();
		string[] availableBusinessTypes = BuildingTypeHelper.GetData(_buildingRegistration).availableBusinessTypes;
		foreach (string businessTypeName in availableBusinessTypes)
		{
			AddType(businessTypeName);
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			scrollbar.value = 0f;
		});
	}

	private void ResetBusinessTypes()
	{
		foreach (StartBusinessTypeUI value in _businessTypes.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		_businessTypes.Clear();
	}

	private void UpdateRetailOnlyVisuals()
	{
		bool active = SupportsRetailBusinesses();
		GameObject[] array = retailOnlyElements;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(active);
		}
	}

	private bool SupportsRetailBusinesses()
	{
		string[] availableBusinessTypes = BuildingTypeHelper.GetData(_buildingRegistration).availableBusinessTypes;
		for (int i = 0; i < availableBusinessTypes.Length; i++)
		{
			BusinessType data = BusinessTypeHelper.GetData(availableBusinessTypes[i]);
			if (data.suitableBuildingType != "ba:buildingtype_warehouse" && data.suitableBuildingType != "ba:buildingtype_office")
			{
				return true;
			}
		}
		return false;
	}

	private void AddType(string businessTypeName)
	{
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		StartBusinessTypeUI startBusinessTypeUI = UnityEngine.Object.Instantiate(businessTypeTemplate, businessesContainer);
		int competitors = GetCompetitors(data);
		startBusinessTypeUI.Initialize(data, competitors);
		startBusinessTypeUI.OnTypeSelected += NewBusinessTypeSelected;
		_businessTypes.Add(businessTypeName, startBusinessTypeUI);
	}

	private void NewBusinessTypeSelected(string selectedBusinessType)
	{
		if (!(selectedBusinessType == _selectedType))
		{
			if (_businessTypes.TryGetValue(_selectedType, out var value))
			{
				value.ChangeSelectedState(isSelected: false);
			}
			_selectedType = selectedBusinessType;
			_businessTypes[_selectedType].ChangeSelectedState(isSelected: true);
		}
	}

	private int GetCompetitors(BusinessType businessType)
	{
		int num = 0;
		if (!businessType.IsHeadquarters)
		{
			foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
			{
				if (buildingRegistration.BuildingCached.Neighbourhood == _buildingRegistration.Neighborhood && buildingRegistration.businessTypeName == businessType.businessTypeName)
				{
					num++;
				}
			}
		}
		return num;
	}

	public void RandomizeName()
	{
		businessNameField.text = InstanceBehavior<GlobalReferences>.Instance.businessNameGenerator.GenerateName(BusinessTypeHelper.GetData(_selectedType), _buildingRegistration.BuildingCached.Neighbourhood);
	}

	public void SetUpBusiness()
	{
		DiplomaName courseRequired = BusinessTypeHelper.GetData(_selectedType).courseRequired;
		if (courseRequired != DiplomaName.Undefined && !EducationHelper.HasCompletedDiploma(courseRequired))
		{
			EducationHelper.ShowCourseRequiredNotification(_selectedType, courseRequired);
			return;
		}
		if (string.IsNullOrWhiteSpace(businessNameField.text))
		{
			Notifications.ShowError("bizman_presentation_notification_no_business_name_entered");
			return;
		}
		businessNameField.text = businessNameField.text.Trim();
		if (businessNameField.text.StartsWith("."))
		{
			Notifications.ShowError("bizman_notification_name_cannot_start_dot", "bizman_notification_name_cannot_start_dot", trackOnSaveGame: false);
			return;
		}
		if (businessNameField.text.Any((char x) => Path.GetInvalidFileNameChars().Contains(x)))
		{
			Notifications.ShowError("bizman_notification_invalid_business_name_characters", "name_invalid_characters", trackOnSaveGame: false);
			return;
		}
		if (SaveGameManager.Current.BuildingRegistrations.Exists((BuildingRegistration x) => x.BusinessName == businessNameField.text))
		{
			Notifications.ShowError("bizman_settings_notification_name_duplicated");
			return;
		}
		if (_selectedType == "ba:businesstype_empty")
		{
			Notifications.ShowError("bizman_presentation_notification_no_business_type_entered");
			return;
		}
		_buildingRegistration.BusinessName = businessNameField.text;
		_buildingRegistration.businessTypeName = _selectedType;
		if (_buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
		{
			_buildingRegistration.TemporarilyClose(closed: false);
			HappinessHelper.AddModifier("ba:happinessmodifier_started_a_headquarters");
			_buildingRegistration.scheduleDays = new List<ScheduleDay>
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
		else
		{
			_buildingRegistration.TemporarilyClose(_buildingRegistration.businessTypeName != "ba:businesstype_warehouse");
			if (_buildingRegistration.businessTypeName == "ba:businesstype_factory")
			{
				_buildingRegistration.scheduleDays = new List<ScheduleDay>
				{
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Monday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Tuesday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Wednesday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Thursday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Friday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Saturday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					},
					new ScheduleDay
					{
						day = DayOfWeekOrdered.Sunday,
						openingHourSlots = new List<OpeningHourSlot>
						{
							new OpeningHourSlot(0, 24)
						},
						isOpen = true
					}
				};
			}
			else
			{
				_buildingRegistration.scheduleDays = new List<ScheduleDay>
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
						isOpen = true
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
		}
		bool num = _buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse";
		if (!num)
		{
			BusinessHelper.UpdateCustomerCapacity(_buildingRegistration);
		}
		_buildingRegistration.creationDay = SaveGameManager.Current.Day;
		if (BusinessTypeHelper.GetData(_buildingRegistration).HasTag(TagRef.Businesstag.generatesrevenue))
		{
			CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(_buildingRegistration, TimeHelper.GetDayOfWeek());
			BusinessHelper.UpdatePromotion(_buildingRegistration);
			_buildingRegistration.UpdateSecurityLevel();
		}
		BusinessHelper.GenerateMissingTodoTasksForBusiness(_buildingRegistration);
		GameEvent.Invoke("ba:gameevent_newbusiness");
		PersonalGoalsUI.UpdatePersonalGoals("ba:gameevent_newbusiness");
		InstanceBehavior<CityManager>.Instance.FindCityBuildingController(_buildingRegistration.Address)?.UpdatePoi();
		InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration == _buildingRegistration)
		{
			InstanceBehavior<BuildingManager>.Instance.businessType = BusinessTypeHelper.GetData(_buildingRegistration);
		}
		if (!num)
		{
			_buildingRegistration.logoSettings = LogoHelper.GenerateLogoSetting(_selectedType);
			BusinessLogoGenerator.Create(_buildingRegistration.BusinessName, _buildingRegistration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(_buildingRegistration.BusinessName), isPlayerBusiness: true, delegate
			{
				GlobalEvents.onBuildingRegistrationChange?.Invoke(_buildingRegistration.Address);
				OnBusinessStarted?.Invoke();
			});
		}
		else
		{
			BusinessLogoGenerator.Instance.GenerateWarehouseLogo(_buildingRegistration.BusinessName, BusinessTypeHelper.GetData(_buildingRegistration), LogoHelper.GetPlayerBusinessLogoPath(_buildingRegistration.BusinessName), isPlayerBusiness: true);
			GlobalEvents.onBuildingRegistrationChange?.Invoke(_buildingRegistration.Address);
			OnBusinessStarted?.Invoke();
		}
		RivalsHelper.CheckRivalTimeline(_buildingRegistration.Neighborhood);
	}
}
