using System;
using System.Collections.Generic;
using System.Linq;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.Characters.Skills;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Buildings;
using EnhancedUI.EnhancedScroller;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Enums;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Elements;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MyEmployees;

public class MyEmployees : MonoBehaviour
{
	public MyEmployeesMassActionsUI massActionsUI;

	public EmployeeScrollerController employeeScrollerController;

	[Header("Candidates")]
	public CandidateScrollerController candidateScrollerController;

	[SerializeField]
	private Button employeesTabButton;

	[SerializeField]
	private Button candidatesTabButton;

	[SerializeField]
	private Badge candidatesBadge;

	[Header("Filters")]
	[SerializeField]
	private TogglePanel employeeFilterPanel;

	[SerializeField]
	private TogglePanel candidateFilterPanel;

	[SerializeField]
	private Badge filterBadge;

	[Header("EmployeeInfo")]
	[SerializeField]
	private Transform employeeInfoBox;

	[SerializeField]
	private TMP_Text employeeNameLabel;

	[SerializeField]
	private TextLocalizationComponent ageLabel;

	[SerializeField]
	private TextLocalizationComponent hourlyWageLabel;

	[SerializeField]
	private TextLocalizationComponent genderLabel;

	[SerializeField]
	private TextLocalizationComponent timeInfoLabel;

	[SerializeField]
	private TextLocalizationComponent calledInSickWarning;

	[SerializeField]
	private GameObject satisfactionPanel;

	[SerializeField]
	private MyEmployeeCandidateSection candidateSection;

	[SerializeField]
	private ProgressBar satisfactionProgressBar;

	[SerializeField]
	private GameObject payBonusPanel;

	[SerializeField]
	private Button payBonusButton;

	[SerializeField]
	private TextLocalizationComponent payBonusLabel;

	[SerializeField]
	private TextLocalizationComponent payBonusCooldownLabel;

	[SerializeField]
	private Transform skillTemplate;

	[SerializeField]
	private Transform demandTemplate;

	[SerializeField]
	private UI.Elements.Dropdown assignedAddressDropdown;

	[SerializeField]
	private Transform trainingOverlay;

	[SerializeField]
	private TextLocalizationComponent positiveActionButtonLabel;

	[SerializeField]
	private TextLocalizationComponent negativeActionButtonLabel;

	[SerializeField]
	private TextLocalizationComponent additionalActionButtonLabel;

	[SerializeField]
	private GameObject additionalActionButton;

	[NonSerialized]
	public string initialTab;

	[NonSerialized]
	public int lastSelectedEmployeeIndex;

	private List<BuildingRegistration> _businessRegistrations;

	private EmployeeInstance _selectedEmployeeInstance;

	public EmployeeInstance SelectedEmployeeInstance => _selectedEmployeeInstance;

	public string CurrentTab { get; private set; }

	private int ActiveFilterCountForCurrentTab
	{
		get
		{
			string currentTab = CurrentTab;
			if (!(currentTab == "Employees"))
			{
				if (currentTab == "Candidates")
				{
					return candidateScrollerController.ActiveFilterCount;
				}
				return 0;
			}
			return employeeScrollerController.ActiveFilterCount;
		}
	}

	private void Awake()
	{
		trainingOverlay.gameObject.SetActive(value: false);
		massActionsUI.Initialize();
	}

	private void Start()
	{
		skillTemplate.gameObject.SetActive(value: false);
		demandTemplate.gameObject.SetActive(value: false);
		DeselectEmployee();
		employeeScrollerController.onRowSelected.AddListener(delegate(EmployeeModel model)
		{
			ShowEmployee(model.employeeInstance);
		});
		candidateScrollerController.onRowSelected.AddListener(delegate(CandidateModel model)
		{
			ShowEmployee(model.employeeInstance);
		});
		assignedAddressDropdown.onOptionSelected.AddListener(AssignSelectedEmployeeToBusiness);
	}

	private void UpdateBusinessDropdown()
	{
		if (_selectedEmployeeInstance == null)
		{
			return;
		}
		_businessRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter);
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		foreach (BuildingRegistration businessRegistration in _businessRegistrations)
		{
			list.Add(businessRegistration.GetDisplayName());
		}
		assignedAddressDropdown.SetOptions(list, localize: false);
	}

	private void AssignSelectedEmployeeToBusiness(int index)
	{
		Address address = ((index >= 1) ? _businessRegistrations[index - 1].Address : null);
		if (!(_selectedEmployeeInstance.assignedAddress == address))
		{
			Address assignedAddress = _selectedEmployeeInstance.assignedAddress;
			if (!_selectedEmployeeInstance.IsCandidate)
			{
				BizManSchedule.AbortAutoFillForBusiness(BuildingHelper.GetBuildingRegistration(assignedAddress));
				EmployeeHelper.UnassignEmployeeFromAllWorkshifts(_selectedEmployeeInstance);
				CustomerDemandHelper.ReloadCachedFulfilled(_selectedEmployeeInstance.assignedAddress);
				CustomerDemandHelper.ReloadCachedFulfilled(assignedAddress);
				_selectedEmployeeInstance.AddTodoTask((!_selectedEmployeeInstance.IsAssignedToAnyBusiness()) ? TodoTaskType.EmployeeUnassigned : TodoTaskType.EmployeeIdle);
			}
			_selectedEmployeeInstance.assignedAddress = address;
			if (_selectedEmployeeInstance.IsCandidate)
			{
				candidateScrollerController.scroller.RefreshActiveCellViews();
			}
			else
			{
				RefreshList();
			}
			ShowEmployee(_selectedEmployeeInstance);
			GlobalEvents.onBuildingRegistrationChange?.Invoke(assignedAddress ?? address);
			SaveGameManager.MarkChange();
		}
	}

	private bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		BuildingTypeData data = BuildingTypeHelper.GetData(buildingRegistration);
		if (_selectedEmployeeInstance.HasSkill("ba:skill_deliverydriver") && data.requiredBuildingSkills.Contains("ba:skill_deliverydriver"))
		{
			return true;
		}
		if (buildingRegistration.Address == _selectedEmployeeInstance.assignedAddress)
		{
			return true;
		}
		if (data.NeedsCleaning && _selectedEmployeeInstance.HasSkill("ba:skill_cleaning"))
		{
			return true;
		}
		BusinessType data2 = BusinessTypeHelper.GetData(buildingRegistration);
		if (data2.HasTag(TagRef.Businesstag.allowtheft) && _selectedEmployeeInstance.HasSkill("ba:skill_securityguard"))
		{
			return true;
		}
		string[] employeePrimarySkills = data2.employeePrimarySkills;
		foreach (string skill in employeePrimarySkills)
		{
			if (_selectedEmployeeInstance.HasSkill(skill))
			{
				return true;
			}
		}
		return false;
	}

	private void OnEnable()
	{
		CandidateCellView.ClearBusinessOptionsCache();
		EmployeeScrollerController obj = employeeScrollerController;
		obj.onDataChanged = (Action)Delegate.Combine(obj.onDataChanged, new Action(UpdateFilterBadge));
		CandidateScrollerController obj2 = candidateScrollerController;
		obj2.onDataChanged = (Action)Delegate.Combine(obj2.onDataChanged, new Action(UpdateFilterBadge));
		if (SaveGameManager.Current != null)
		{
			MyEmployeesMassActionsUI.massActionSelectedEmployees = new List<EmployeeInstance>();
			massActionsUI.massActionAllToggle.SetIsOnWithoutNotify(value: false);
			_selectedEmployeeInstance = null;
			employeeScrollerController.SelectRow(null, null);
			candidateScrollerController.SelectRow(null, null);
			UpdateBadge();
			ChangeTab(initialTab ?? "Employees");
			initialTab = null;
		}
	}

	private void OnDisable()
	{
		CandidateCellView.ClearBusinessOptionsCache();
		employeeFilterPanel.Close();
		employeeScrollerController.ClearFilters();
		EmployeeScrollerController obj = employeeScrollerController;
		obj.onDataChanged = (Action)Delegate.Remove(obj.onDataChanged, new Action(UpdateFilterBadge));
		candidateFilterPanel.Close();
		candidateScrollerController.ClearFilters();
		CandidateScrollerController obj2 = candidateScrollerController;
		obj2.onDataChanged = (Action)Delegate.Remove(obj2.onDataChanged, new Action(UpdateFilterBadge));
		DeselectEmployee();
		employeeScrollerController.SelectRow(null, null);
		candidateScrollerController.SelectRow(null, null);
	}

	public void DelayShowEmployee(EmployeeInstance employeeInstance)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			ShowEmployee(employeeInstance);
		});
	}

	public void UpdateCurrentEmployeeBox()
	{
		if (_selectedEmployeeInstance == null)
		{
			return;
		}
		string currentTab = CurrentTab;
		if (!(currentTab == "Employees"))
		{
			if (currentTab == "Candidates" && !SaveGameManager.Current.CandidateEmployeeInstances.Contains(_selectedEmployeeInstance))
			{
				DeselectEmployee();
				candidateScrollerController.SelectRow(null, null);
				return;
			}
		}
		else if (!EmployeeHelper.GetEmployeeInstances().Contains(_selectedEmployeeInstance))
		{
			DeselectEmployee();
			employeeScrollerController.SelectRow(null, null);
			return;
		}
		ShowEmployee(_selectedEmployeeInstance);
	}

	public void ShowEmployee(EmployeeInstance employeeInstance)
	{
		_selectedEmployeeInstance = employeeInstance;
		employeeNameLabel.text = employeeInstance.characterData.name;
		ageLabel.SetData("common_age".Localize(new
		{
			age = TimeHelper.GetYearsByDays(employeeInstance.characterData.ageInDays)
		}));
		genderLabel.SetData("common_gender".Localize(new
		{
			gender = employeeInstance.characterData.gender.GetLocalizeKey()
		}));
		hourlyWageLabel.SetData("common_hourly_wage".Localize(new
		{
			hourlyWage = employeeInstance.hourlyWage.ToCurrencyFormat()
		}));
		if (employeeInstance.IsCandidate)
		{
			UpdateCandidateUI(employeeInstance);
		}
		else
		{
			UpdateEmployeeUI(employeeInstance);
		}
		UpdateBusinessDropdown();
		if (employeeInstance.IsAssignedToAnyBusiness())
		{
			assignedAddressDropdown.ResetSelectedOption(_businessRegistrations.FindIndex((BuildingRegistration x) => x.Address == employeeInstance.assignedAddress) + 1);
		}
		else
		{
			assignedAddressDropdown.ResetSelectedOption(0);
		}
		skillTemplate.ResetTemplate();
		employeeInfoBox.gameObject.SetActive(value: true);
		SetTrainingOverlay(employeeInstance);
		foreach (Skill skill in employeeInstance.characterData.skills)
		{
			Transform obj = UnityEngine.Object.Instantiate(skillTemplate, skillTemplate.parent);
			obj.GetLanguageChangeEventByName("SkillName").Key = skill.name;
			obj.GetLabelByName("SkillLevel").text = skill.GetRoundedValue() + "%";
			Button buttonByName = obj.GetButtonByName("TrainButton");
			if (employeeInstance.IsCandidate)
			{
				buttonByName.gameObject.SetActive(value: false);
			}
			else
			{
				buttonByName.gameObject.SetActive(employeeInstance.CanTrainSkill(skill));
				if (employeeInstance.CanTrainSkill(skill))
				{
					buttonByName.onClick.AddListener(delegate
					{
						if (!_selectedEmployeeInstance.assignedAddress.IsUndefined() && _selectedEmployeeInstance.assignedAddress != null)
						{
							Notifications.ShowError("myemployees_unassign_for_training");
						}
						else
						{
							int skillIncrease = Mathf.Min(Mathf.CeilToInt(100f - skill.value), 10);
							float price = EmployeeHelper.GetTrainingCost(employeeInstance, skill.name, skillIncrease);
							LanguageChangeEventDataHolder bodyData = "myemployees_hud_confirm_start_training".Localize(new
							{
								value = Mathf.Min(skill.GetRoundedValue() + 10, 100),
								price = price.ToShortCurrencyFormat(),
								skill = skill.name
							});
							HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
							{
								Dictionary<string, string> data = new Dictionary<string, string>
								{
									{
										"employee",
										employeeInstance.characterData.name
									},
									{ "skillName", skill.name }
								};
								TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_employeetraining", data);
								transactionInfo.SetTaxDeductibleName("ba:transaction_employeetraining_label");
								if (GameManager.ChangeMoneySafe(0f - price, transactionInfo, null, null, force: false, showNotification: true))
								{
									employeeInstance.trainingSession = new EmployeeInstance.TrainingInstance
									{
										skill = skill.name,
										startDay = SaveGameManager.Current.Day
									};
									ShowEmployee(employeeInstance);
									GameEvent.Invoke(string.Empty);
									employeeScrollerController.RefreshEmployeeModel(employeeInstance);
									employeeScrollerController.RefreshFilters();
								}
							});
						}
					});
				}
			}
			obj.gameObject.SetActive(value: true);
		}
		demandTemplate.ResetTemplate();
		foreach (string demand in employeeInstance.demands)
		{
			Transform transform = UnityEngine.Object.Instantiate(demandTemplate, demandTemplate.parent);
			transform.GetLanguageChangeEventByName("MainInfo/Name").Key = demand;
			BasicTooltip componentInChildren = transform.GetComponentInChildren<BasicTooltip>();
			componentInChildren.titleKey = demand;
			componentInChildren.descriptionKey = demand + "_description";
			JobDemand byName = JobDemandHelper.GetByName(demand);
			transform.GetLanguageChangeEventByName("MainInfo/Priority").Key = byName.PriorityLocalizeKey;
			TextMeshProUGUI labelByName = transform.GetLabelByName("MainInfo/Priority");
			switch (byName.priority)
			{
			case Priority.Low:
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
				break;
			case Priority.Medium:
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
				break;
			case Priority.High:
				labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
				break;
			}
			Toggle componentInChildren2 = transform.GetComponentInChildren<Toggle>();
			TextLocalizationComponent languageChangeEventByName = transform.GetLanguageChangeEventByName("ExtraInfo/ExtraInfoLabel");
			if (employeeInstance.IsCandidate)
			{
				componentInChildren2.gameObject.SetActive(value: false);
				languageChangeEventByName.gameObject.SetActive(value: false);
			}
			else
			{
				componentInChildren2.isOn = byName.Fulfilled(_selectedEmployeeInstance, null);
				if (byName is HoursWorkingPerWeek)
				{
					languageChangeEventByName.SetData("myemployees_employee_hours".Localize(new { employeeInstance.assignedWeeklyHours, employeeInstance.workedHoursThisWeek }));
				}
				else if (byName is DaysWorkingPerWeek)
				{
					languageChangeEventByName.SetData("myemployees_employee_days".Localize(new
					{
						assignedWeeklyDays = employeeInstance.assignedWeeklyDays.Count,
						workedDays = employeeInstance.workedDays
					}));
				}
				else
				{
					languageChangeEventByName.gameObject.SetActive(value: false);
				}
			}
			transform.gameObject.SetActive(value: true);
		}
	}

	private void UpdateCandidateUI(EmployeeInstance employeeInstance)
	{
		candidateSection.UpdateUI(employeeInstance);
		satisfactionPanel.SetActive(value: false);
		payBonusPanel.SetActive(value: false);
		calledInSickWarning.gameObject.SetActive(value: false);
		positiveActionButtonLabel.Key = "myemployees_hirecandidate_button";
		negativeActionButtonLabel.Key = "myemployees_discardcandidate_button";
		additionalActionButtonLabel.Key = "dialog_negotiate_button";
		additionalActionButton.SetActive(value: true);
	}

	private void UpdateEmployeeUI(EmployeeInstance employeeInstance)
	{
		timeInfoLabel.SetData("myemployees_days_hired".Localize(new
		{
			daysHired = SaveGameManager.Current.Day - employeeInstance.dayHired
		}));
		candidateSection.SetActive(active: false);
		satisfactionPanel.SetActive(value: true);
		satisfactionProgressBar.SetValue(employeeInstance.satisfaction);
		payBonusPanel.SetActive(value: true);
		bool flag = employeeInstance.CanGiveBonus();
		payBonusButton.interactable = flag;
		payBonusLabel.SetData("myemployees_configure_pay_bonus".Localize(new
		{
			bonusAmount = (flag ? employeeInstance.GetBonusAmount().ToShortCurrencyFormat() : 0f.ToShortCurrencyFormat())
		}));
		int daysUntilBonusAvailability = employeeInstance.GetDaysUntilBonusAvailability();
		payBonusCooldownLabel.gameObject.SetActive(daysUntilBonusAvailability > 0);
		if (daysUntilBonusAvailability > 0)
		{
			payBonusCooldownLabel.SetData("myemployees_pay_bonus_cooldown".Localize(new
			{
				days = daysUntilBonusAvailability
			}));
		}
		calledInSickWarning.SetData("myemployees_called_in_sick".Localize(new
		{
			employeeName = employeeInstance.characterData.name
		}));
		calledInSickWarning.gameObject.SetActive(_selectedEmployeeInstance.isAbsent);
		positiveActionButtonLabel.Key = "myemployees_manage_schedule";
		negativeActionButtonLabel.Key = "myemployees_fire";
		additionalActionButton.SetActive(value: false);
	}

	private void SetTrainingOverlay(EmployeeInstance employeeInstance)
	{
		bool isTraining = employeeInstance.IsTraining;
		trainingOverlay.gameObject.SetActive(isTraining);
		assignedAddressDropdown.gameObject.SetActive(!isTraining);
		if (isTraining)
		{
			trainingOverlay.GetLanguageChangeEventByName("SkillName").Key = employeeInstance.trainingSession.skill;
			Timestamp timestamp = TimeHelper.Now();
			timestamp.Day = employeeInstance.trainingSession.startDay + 1;
			timestamp.Hour = 17;
			int hours = Mathf.FloorToInt(timestamp.GetTotalMinutes() - TimeHelper.NowInMinutes()) / 60;
			trainingOverlay.GetLanguageChangeEventByName("HoursLeft").Arguments = new { hours };
		}
	}

	public void OnPayBonusButtonClick()
	{
		if (_selectedEmployeeInstance.GiveBonus())
		{
			satisfactionProgressBar.SetValue(_selectedEmployeeInstance.satisfaction, showAnimation: true);
			payBonusButton.interactable = false;
			payBonusLabel.SetData("myemployees_configure_pay_bonus".Localize(new
			{
				bonusAmount = 0f.ToShortCurrencyFormat()
			}));
			payBonusCooldownLabel.gameObject.SetActive(value: true);
			payBonusCooldownLabel.SetData("myemployees_pay_bonus_cooldown".Localize(new
			{
				days = 30
			}));
			EmployeeModel employeeModel = employeeScrollerController.data.FirstOrDefault((EmployeeModel x) => x.employeeInstance == _selectedEmployeeInstance);
			if (employeeModel != null)
			{
				employeeModel.satisfaction = _selectedEmployeeInstance.satisfaction;
			}
			employeeScrollerController.scroller.RefreshActiveCellViews();
			ReorderCurrentScroller();
		}
	}

	public void OnPositiveActionButtonClick()
	{
		if (_selectedEmployeeInstance.IsCandidate)
		{
			MyEmployeesMassActionsUI.massActionSelectedEmployees.Remove(_selectedEmployeeInstance);
			EmployeeHelper.HireCandidate(_selectedEmployeeInstance);
			candidateScrollerController.RemoveCandidate(_selectedEmployeeInstance);
			DeselectEmployee();
		}
		else
		{
			ManageSchedule(_selectedEmployeeInstance);
		}
	}

	public void OnNegativeActionButtonClick()
	{
		MyEmployeesMassActionsUI.massActionSelectedEmployees.Remove(_selectedEmployeeInstance);
		if (_selectedEmployeeInstance.IsCandidate)
		{
			EmployeeHelper.DiscardCandidate(_selectedEmployeeInstance);
			candidateScrollerController.RemoveCandidate(_selectedEmployeeInstance);
			DeselectEmployee();
		}
		else
		{
			FireEmployee();
		}
	}

	public void OnAdditionalActionButtonClick()
	{
		NegotiateWithCandidate(_selectedEmployeeInstance);
	}

	public void NegotiateWithCandidate(EmployeeInstance employeeInstance, bool isRival = false, bool isPoached = false)
	{
		if (SaveGameManager.Current.candidateSalaryNegotiations.Any((CandidateSalaryNegotiation x) => !x.completed && x.employeeInstance.id == employeeInstance.id))
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(employeeInstance.GetContact());
		}
		else
		{
			CandidateSalaryNegotiation candidateSalaryNegotiation = new CandidateSalaryNegotiation(employeeInstance, isRival, isPoached);
			SaveGameManager.Current.candidateSalaryNegotiations.Add(candidateSalaryNegotiation);
			candidateSalaryNegotiation.SendOffer();
		}
	}

	public static void ManageSchedule(EmployeeInstance employeeInstance)
	{
		if (!employeeInstance.IsAssignedToAnyBusiness())
		{
			Notifications.ShowError("myemployees_no_business_assigned_notification", "myemployees_no_business_assigned_notification");
			return;
		}
		string tab = (employeeInstance.HasSkill("ba:skill_deliverydriver") ? "Drivers" : "Schedule");
		Address assignedAddress = employeeInstance.assignedAddress;
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(assignedAddress, tab);
	}

	private void FireEmployee()
	{
		HudConfirm.Show(null, "myemployees_hud_confirm_fire_employee", delegate
		{
			_selectedEmployeeInstance.RemoveEmployee();
			employeeScrollerController.RemoveEmployee(_selectedEmployeeInstance);
			DeselectEmployee();
			GameEvent.Invoke(string.Empty);
		});
	}

	public void RefreshList()
	{
		ChangeTab(CurrentTab, keepSortOrder: true);
	}

	public void ChangeTab(string newTab)
	{
		ChangeTab(newTab, keepSortOrder: false);
	}

	private void ChangeTab(string newTab, bool keepSortOrder)
	{
		if (CurrentTab != newTab)
		{
			employeeFilterPanel.Close();
			candidateFilterPanel.Close();
			MyEmployeesMassActionsUI.massActionSelectedEmployees = new List<EmployeeInstance>();
			massActionsUI.massActionAllToggle.SetIsOnWithoutNotify(value: false);
			DeselectEmployee();
		}
		CurrentTab = newTab;
		UpdateFilterBadge();
		employeeScrollerController.gameObject.SetActive(newTab == "Employees");
		candidateScrollerController.gameObject.SetActive(newTab == "Candidates");
		employeesTabButton.interactable = newTab != "Employees";
		candidatesTabButton.interactable = newTab != "Candidates";
		employeesTabButton.transform.GetLabelByName("Label").color = ((newTab == "Employees") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		candidatesTabButton.transform.GetLabelByName("Label").color = ((newTab == "Candidates") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		if (!(newTab == "Employees"))
		{
			if (newTab == "Candidates")
			{
				massActionsUI.UpdateMassActionDropdown("Candidates");
				float currentScrollPosition = candidateScrollerController.scroller.ScrollPosition;
				CoroutineUtility.RunAfterOneFrame(delegate
				{
					candidateScrollerController.LoadList();
					candidateScrollerController.ChangeSortOrder(null, flipOrder: false);
					if (keepSortOrder)
					{
						candidateScrollerController.scroller.SetScrollPositionImmediately(currentScrollPosition);
					}
					else
					{
						lastSelectedEmployeeIndex = -1;
					}
				});
				employeeScrollerController.SelectRow(null, null);
			}
			else
			{
				Debug.LogError("Unhandled tab: " + CurrentTab);
			}
			return;
		}
		massActionsUI.UpdateMassActionDropdown("Employees");
		float currentScrollPosition2 = employeeScrollerController.scroller.ScrollPosition;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			employeeScrollerController.LoadList();
			employeeScrollerController.ChangeSortOrder(null, flipOrder: false);
			if (keepSortOrder)
			{
				employeeScrollerController.scroller.SetScrollPositionImmediately(currentScrollPosition2);
			}
			else
			{
				lastSelectedEmployeeIndex = -1;
			}
		});
		candidateScrollerController.SelectRow(null, null);
	}

	public void ChangeToEmployeesTab()
	{
		ChangeTab("Employees");
	}

	public void ChangeToCandidatesTab()
	{
		ChangeTab("Candidates");
	}

	public void ToggleFilterPanel()
	{
		string currentTab = CurrentTab;
		if (!(currentTab == "Employees"))
		{
			if (currentTab == "Candidates")
			{
				candidateFilterPanel.Toggle();
			}
			else
			{
				Debug.LogError("Unhandled tab: " + CurrentTab);
			}
		}
		else
		{
			employeeFilterPanel.Toggle();
		}
	}

	public void DeselectEmployee()
	{
		_selectedEmployeeInstance = null;
		employeeInfoBox.gameObject.SetActive(value: false);
	}

	public void UpdateBadge()
	{
		candidatesBadge.UpdateBadge(SaveGameManager.Current.CandidateEmployeeInstances.Count);
	}

	private void UpdateFilterBadge()
	{
		filterBadge.UpdateBadge(ActiveFilterCountForCurrentTab);
	}

	public EnhancedScroller GetCurrentScroller()
	{
		string currentTab = CurrentTab;
		if (!(currentTab == "Employees"))
		{
			if (currentTab == "Candidates")
			{
				return candidateScrollerController.scroller;
			}
			Debug.LogError("Unhandled tab: " + CurrentTab);
			return employeeScrollerController.scroller;
		}
		return employeeScrollerController.scroller;
	}

	public void ReorderCurrentScroller()
	{
		string currentTab = CurrentTab;
		if (!(currentTab == "Employees"))
		{
			if (currentTab == "Candidates")
			{
				candidateScrollerController.ChangeSortOrder(null, flipOrder: false);
				float scrollPosition = candidateScrollerController.scroller.ScrollPosition;
				candidateScrollerController.scroller.SetScrollPositionImmediately(scrollPosition);
			}
			else
			{
				Debug.LogError("Unhandled tab: " + CurrentTab);
			}
		}
		else
		{
			employeeScrollerController.ChangeSortOrder(null, flipOrder: false);
			float scrollPosition2 = employeeScrollerController.scroller.ScrollPosition;
			employeeScrollerController.scroller.SetScrollPositionImmediately(scrollPosition2);
		}
	}
}
