using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Buildings.BuildingTypes.Shared.Dirtiness;
using DG.Tweening;
using Dialogs;
using Entities;
using Enums;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Player.FoodDeliveryJob;
using PlayerActivity;
using Streets;
using TMPro;
using UI.Guiders;
using UI.InteriorDesigner;
using UI.MainMenu;
using UI.Notification;
using UI.Smartphone;
using UI.Smartphone.Apps.Persona;
using UnityEngine;
using UnityEngine.UI;
using Vehicles.DeliveryDriverJob;

namespace UI.Tasks;

public class TasksUI : MonoBehaviour
{
	private static readonly List<Transform> UnsortedTaskUIElements = new List<Transform>();

	public Transform taskEntryTemplate;

	public Transform nestedTaskEntryTemplate;

	public Color inactiveTaskColor;

	public Color highlightTextColor;

	[SerializeField]
	private GameObject container;

	[SerializeField]
	private RectTransform expandButton;

	[SerializeField]
	private float bounceFactor = 0.1f;

	[SerializeField]
	private float bounceAnimationDuration = 0.2f;

	[SerializeField]
	private float collapseAnimationDuration = 0.3f;

	[SerializeField]
	private Transform groupTemplate;

	[SerializeField]
	private float maxObjectivesHeight;

	[SerializeField]
	private PreferredSizeFitter preferredSizeFitter;

	private CanvasGroup _canvasGroup;

	private Transform _tasksGroup;

	private bool _scheduledUpdateObjectivesHeightRunning;

	[NonSerialized]
	public bool forceCheckForCompletedTodoTasks;

	[NonSerialized]
	public readonly WorkoutPlanUI workoutPlanUI = new WorkoutPlanUI();

	[NonSerialized]
	public readonly DeliveryJobUI deliveryJobUI = new DeliveryJobUI();

	[NonSerialized]
	public readonly FoodDeliveryJobUI foodDeliveryJobUI = new FoodDeliveryJobUI();

	private readonly HashSet<string> _gameEventsLastFrame = new HashSet<string>();

	private readonly List<string> _gameEventsTemp = new List<string>();

	private bool _scheduledUpdateCurrentBusinessTodoTasks;

	private Coroutine _switchOutAnimationCoroutine;

	private Action _onExpand;

	public bool IsCollapsed { get; private set; }

	private void Awake()
	{
		container.gameObject.SetActive(value: true);
		groupTemplate.gameObject.SetActive(value: false);
		taskEntryTemplate.gameObject.SetActive(value: false);
		nestedTaskEntryTemplate.gameObject.SetActive(value: false);
		_canvasGroup = GetComponent<CanvasGroup>();
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool show)
		{
			ChangeVisibility(!show);
		});
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnd));
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Combine(GlobalEvents.onItemDropped, (Action<ItemController>)delegate
		{
			ScheduleUpdateCurrentBusinessTodoTasks();
		});
		GlobalEvents.onItemDiscarded = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemDiscarded, (Action<ItemInstance>)delegate
		{
			ScheduleUpdateCurrentBusinessTodoTasks();
		});
		GlobalEvents.onItemGrabbed = (Action<ItemInstance>)Delegate.Combine(GlobalEvents.onItemGrabbed, (Action<ItemInstance>)delegate(ItemInstance itemInstance)
		{
			InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.FindAll((TodoTask x) => x.itemInstanceId == itemInstance.id));
			ScheduleUpdateCurrentBusinessTodoTasks();
		});
		InteriorDesignerUI.OnInteriorDesignerToggle.AddListener(OnInteriorDesignerToggled);
		TutorialHelper.onQuestLoaded = (Action)Delegate.Combine(TutorialHelper.onQuestLoaded, new Action(ScheduleUpdateObjectivesHeight));
	}

	private void OnGameEventTriggered(string gameEvent)
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.isRunning || gameEvent == "ba:gameevent_newday")
		{
			_gameEventsLastFrame.Add(gameEvent);
		}
	}

	private void LateUpdate()
	{
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning || ItemHelper.ItemsToUpdateToDoTasks.Count > 0)
		{
			return;
		}
		if (_gameEventsLastFrame.Count <= 0 && forceCheckForCompletedTodoTasks)
		{
			CheckForCompletedTodoTasks();
		}
		else
		{
			if (_gameEventsLastFrame.Count <= 0)
			{
				return;
			}
			_gameEventsTemp.Clear();
			foreach (string item in _gameEventsLastFrame)
			{
				_gameEventsTemp.Add(item);
			}
			_gameEventsLastFrame.Clear();
			foreach (string item2 in _gameEventsTemp)
			{
				PersonalGoalsUI.UpdatePersonalGoals(item2);
			}
			_gameEventsTemp.Clear();
			CheckForCompletedTodoTasks();
		}
	}

	private void Start()
	{
		_tasksGroup = SetUpTasksGroup("bizman_alerts");
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.dontgeneratetodotasks))
			{
				BusinessHelper.GenerateMissingTodoTasksForBusiness(buildingRegistration);
			}
		}
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Combine(InteriorDesignerUI.onCloseInteriorDesigner, new Action(CheckInteriorDesignerTasks));
		foreach (TodoTask todoTask in SaveGameManager.Current.TodoTasks)
		{
			AddTodoTaskToUI(todoTask, updateAll: false);
		}
		workoutPlanUI.Init();
		deliveryJobUI.Init();
		foodDeliveryJobUI.Init();
		ScheduleUpdateObjectivesHeight();
		GameEvent.Invoke(string.Empty);
	}

	public void ScheduleUpdateCurrentBusinessTodoTasks()
	{
		if (!_scheduledUpdateCurrentBusinessTodoTasks)
		{
			_scheduledUpdateCurrentBusinessTodoTasks = true;
			CoroutineUtility.RunAfterOneFrame(UpdateCurrentBusinessTodoTasks);
		}
	}

	private void UpdateCurrentBusinessTodoTasks()
	{
		_scheduledUpdateCurrentBusinessTodoTasks = false;
		if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration != null)
		{
			BusinessHelper.GenerateMissingTodoTasksForBusiness(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		}
	}

	public void ChangeVisibility(bool show)
	{
		show = show && !GameManager.IsAnyMiniGameActive();
		_canvasGroup.alpha = (show ? 1 : 0);
		_canvasGroup.interactable = show;
		_canvasGroup.blocksRaycasts = show;
	}

	public void ScheduleUpdateObjectivesHeight()
	{
		if (!_scheduledUpdateObjectivesHeightRunning)
		{
			_scheduledUpdateObjectivesHeightRunning = true;
			if (!InstanceBehavior<UIs>.Instance.timeMachine.isRunning && !InteriorDesignerUI.IsOpen)
			{
				UpdateObjectivesHeight();
			}
		}
	}

	private void OnTimeMachineEnd()
	{
		if (_scheduledUpdateObjectivesHeightRunning)
		{
			UpdateObjectivesHeight();
		}
		forceCheckForCompletedTodoTasks = true;
	}

	private void OnInteriorDesignerToggled(bool open)
	{
		if (!open && _scheduledUpdateObjectivesHeightRunning)
		{
			UpdateObjectivesHeight();
		}
	}

	private void UpdateObjectivesHeight()
	{
		_scheduledUpdateObjectivesHeightRunning = false;
		preferredSizeFitter.ForceUpdate();
		SortTodoTasks();
	}

	private void AddTodoTaskToUI(TodoTask task, bool updateAll = true)
	{
		if (!_tasksGroup || _tasksGroup.Find(task.id) != null)
		{
			return;
		}
		Transform transform = UnityEngine.Object.Instantiate(taskEntryTemplate, _tasksGroup);
		transform.name = task.id;
		TextLocalizationComponent languageChangeEventByName = transform.GetLanguageChangeEventByName("Label");
		languageChangeEventByName.SetData(GetTodoDescription(task));
		TMP_Text textContainer = languageChangeEventByName.TextContainer;
		TMP_Text tMP_Text = textContainer;
		tMP_Text.color = task.priority switch
		{
			Priority.Medium => InstanceBehavior<GlobalReferences>.Instance.colors.yellow, 
			Priority.High => InstanceBehavior<GlobalReferences>.Instance.colors.red, 
			_ => textContainer.color, 
		};
		transform.Find("Checkmark").GetComponent<Toggle>().isOn = false;
		Button component = transform.GetComponent<Button>();
		component.onClick.AddListener(delegate
		{
			ClickTask(task);
		});
		component.interactable = true;
		Button buttonByName = transform.GetButtonByName("DestinationButton");
		Address destinationAddress = GetDestination(task);
		if (destinationAddress == null)
		{
			buttonByName.gameObject.SetActive(value: false);
		}
		else
		{
			buttonByName.gameObject.SetActive(value: true);
			buttonByName.onClick.AddListener(delegate
			{
				SaveGameManager.Current.customDestination = destinationAddress;
				Dictionary<string, string> notificationData = new Dictionary<string, string> { 
				{
					"address",
					destinationAddress.ToFormattedString()
				} };
				Notifications.Show(NotificationType.Success, "notification_destination_set", notificationData);
				GuidersManager.SetGuiderTarget(destinationAddress, DirectionGuiderType.Destination);
			});
		}
		transform.gameObject.SetActive(value: true);
		if (updateAll)
		{
			ScheduleUpdateObjectivesHeight();
		}
	}

	public Transform SetUpTasksGroup(string headerKey)
	{
		Transform obj = UnityEngine.Object.Instantiate(groupTemplate, groupTemplate.parent);
		obj.GetLanguageChangeEventByName("Headline/Label").Key = headerKey;
		obj.gameObject.SetActive(value: true);
		return obj;
	}

	public TodoTask CreateTodoTask(TodoTaskType type, Address address = null, string itemName = null, string itemInstanceId = null, string employeeId = null, Priority priority = Priority.Low, int priorityOffset = 0, int remainingDays = 0, string businessRequirementName = null, bool skipExistsCheck = false)
	{
		TodoTask todoTask = AddNewTodoTask(type, address, itemName, itemInstanceId, employeeId, priority, priorityOffset, remainingDays, businessRequirementName, skipExistsCheck);
		if (todoTask != null)
		{
			AddTodoTaskToUI(todoTask);
		}
		return todoTask;
	}

	public static TodoTask AddNewTodoTask(TodoTaskType type, Address address = null, string itemName = null, string itemInstanceId = null, string employeeId = null, Priority priority = Priority.Low, int priorityOffset = 0, int remainingDays = 0, string businessRequirementName = null, bool skipExistsCheck = false)
	{
		if (!skipExistsCheck && ExistsAlready(type, address, itemName, itemInstanceId, employeeId, SaveGameManager.Current.TodoTasks))
		{
			return null;
		}
		TodoTask todoTask = new TodoTask
		{
			id = UuidHelper.GenerateBase64Uuid(),
			type = type,
			address = address,
			employeeId = employeeId,
			itemName = itemName,
			itemInstanceId = itemInstanceId,
			priority = priority,
			priorityOffset = priorityOffset,
			remainingDays = remainingDays,
			businessRequirement = businessRequirementName
		};
		SaveGameManager.Current.TodoTasks.Add(todoTask);
		UpdateBusinessAlerts(todoTask.address);
		return todoTask;
	}

	public static bool ExistsAlready(TodoTaskType type, Address address, string itemName, string itemInstanceId, string employeeId, List<TodoTask> tasks)
	{
		bool result = false;
		foreach (TodoTask task in tasks)
		{
			if (task.type == type && !(task.address != address) && !(task.itemName != itemName) && !(task.itemInstanceId != itemInstanceId) && !(task.employeeId != employeeId))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void UpdateTodoTask(TodoTask task, Priority newPriority)
	{
		TextLocalizationComponent languageChangeEventByName = _tasksGroup.Find(task.id).GetLanguageChangeEventByName("Label");
		languageChangeEventByName.SetData(GetTodoDescription(task));
		if (task.priority != newPriority)
		{
			task.priority = newPriority;
			TMP_Text textContainer = languageChangeEventByName.TextContainer;
			TMP_Text tMP_Text = textContainer;
			tMP_Text.color = task.priority switch
			{
				Priority.Low => InstanceBehavior<GlobalReferences>.Instance.colors.white, 
				Priority.Medium => InstanceBehavior<GlobalReferences>.Instance.colors.yellow, 
				Priority.High => InstanceBehavior<GlobalReferences>.Instance.colors.red, 
				_ => textContainer.color, 
			};
			UpdateBusinessAlerts(task.address);
			ScheduleUpdateObjectivesHeight();
		}
	}

	private void SortTodoTasks()
	{
		if (!_tasksGroup)
		{
			return;
		}
		UnsortedTaskUIElements.Clear();
		foreach (Transform item in _tasksGroup)
		{
			UnsortedTaskUIElements.Add(item);
		}
		UnsortedTaskUIElements.Sort(delegate(Transform a, Transform b)
		{
			TodoTask todoTask = SaveGameManager.Current.TodoTasks.Find((TodoTask t) => t.id == a.name);
			TodoTask todoTask2 = SaveGameManager.Current.TodoTasks.Find((TodoTask t) => t.id == b.name);
			if (todoTask == null || todoTask2 == null)
			{
				return 0;
			}
			int value = (int)todoTask.priority * 100 + todoTask.priorityOffset;
			int num = ((int)todoTask2.priority * 100 + todoTask2.priorityOffset).CompareTo(value);
			return (num == 0) ? Comparer<Address>.Default.Compare(todoTask.address, todoTask2.address) : num;
		});
		foreach (Transform unsortedTaskUIElement in UnsortedTaskUIElements)
		{
			unsortedTaskUIElement.SetAsLastSibling();
		}
	}

	public void CompleteTodoTask(TodoTask task)
	{
		if (task != null)
		{
			CoroutineUtility.Run(CompleteTodoTaskRoutine(task));
			UpdateBusinessAlerts(task.address);
		}
	}

	private static void UpdateBusinessAlerts(Address taskAddress)
	{
		if (FullMenu.IsOpen && InstanceBehavior<UIs>.Instance.fullMenu.bizMan.gameObject.activeSelf && InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address == taskAddress)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.ScheduleLoadAlerts();
		}
	}

	private IEnumerator CompleteTodoTaskRoutine(TodoTask task)
	{
		Transform entry = _tasksGroup.Find(task.id);
		if ((bool)entry)
		{
			CanvasGroup cg = entry.GetComponent<CanvasGroup>();
			TextMeshProUGUI labelByName = entry.GetLabelByName("Label");
			labelByName.fontStyle = FontStyles.Strikethrough;
			labelByName.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;
			SaveGameManager.Current.TodoTasks.RemoveAll((TodoTask x) => x.id == task.id);
			yield return new WaitForSecondsRealtime(4f);
			cg.DOFade(0f, 2f).SetLink(entry.gameObject).SetUpdate(isIndependentUpdate: true)
				.OnComplete(delegate
				{
					UnityEngine.Object.Destroy(entry.gameObject);
					ScheduleUpdateObjectivesHeight();
				});
		}
	}

	public void InstantlyCompleteListOfTasks(IEnumerable<TodoTask> tasks)
	{
		if (!_tasksGroup)
		{
			return;
		}
		List<TodoTask> list = new List<TodoTask>();
		foreach (TodoTask task in tasks)
		{
			Transform transform = _tasksGroup.Find(task.id);
			if ((bool)transform)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
				list.Add(task);
			}
		}
		foreach (TodoTask item in list)
		{
			SaveGameManager.Current.TodoTasks.Remove(item);
		}
		ScheduleUpdateObjectivesHeight();
	}

	public void InstantlyCompleteTodoTask(TodoTask task)
	{
		Transform transform = _tasksGroup.Find(task.id);
		if ((bool)transform)
		{
			UnityEngine.Object.Destroy(transform.gameObject);
			ScheduleUpdateObjectivesHeight();
			SaveGameManager.Current.TodoTasks.RemoveAll((TodoTask x) => x.id == task.id);
		}
	}

	public static void UpdateTasksFromBusiness(BuildingRegistration registration)
	{
		InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteListOfTasks(SaveGameManager.Current.TodoTasks.FindAll((TodoTask x) => x.address == registration.Address));
		BusinessHelper.GenerateMissingTodoTasksForBusiness(registration);
	}

	private void CheckForCompletedTodoTasks()
	{
		forceCheckForCompletedTodoTasks = false;
		List<TodoTask> list = new List<TodoTask>(SaveGameManager.Current.TodoTasks);
		list.Sort((TodoTask left, TodoTask right) => Comparer<Address>.Default.Compare(left.address, right.address));
		foreach (TodoTask item in list)
		{
			BuildingRegistration buildingRegistration = ((!item.address.IsUndefined() && item.address != null) ? BuildingHelper.GetBuildingRegistration(item.address) : null);
			switch (item.type)
			{
			case TodoTaskType.LowStock:
			{
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
					break;
				}
				if (!buildingRegistration.itemInstances.TryGetValue(item.itemInstanceId, out var value2))
				{
					CompleteTodoTask(item);
					break;
				}
				CargoInstance stockInstance = value2.GetStockInstance();
				if (string.IsNullOrEmpty(stockInstance.itemName))
				{
					CompleteTodoTask(item);
				}
				else if (stockInstance.ItemCached.HasTag(TagRef.Itemtag.isbag) && !BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags))
				{
					CompleteTodoTask(item);
				}
				else if ((float)(stockInstance.amount + BuildingHelper.CountTotalResourcesInStock(buildingRegistration, stockInstance.itemName, includeProducers: false, includePallets: false)) > (float)stockInstance.GetMaxStockCapacity(value2) * 0.25f)
				{
					CompleteTodoTask(item);
				}
				break;
			}
			case TodoTaskType.EmptyStock:
			{
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
					break;
				}
				if (!buildingRegistration.itemInstances.TryGetValue(item.itemInstanceId, out var value))
				{
					CompleteTodoTask(item);
					break;
				}
				if (value.ItemCached.HasTag(TagRef.Itemtag.isbag) && !BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags))
				{
					CompleteTodoTask(item);
					break;
				}
				CargoInstance stockInstance = value.GetStockInstance();
				if (stockInstance.amount > 0 || BuildingHelper.HasResourcesInStock(buildingRegistration.Address, stockInstance.itemName, includeProducers: false, includePallets: false))
				{
					CompleteTodoTask(item);
				}
				break;
			}
			case TodoTaskType.MissingRequiredItem:
			case TodoTaskType.MissingRequiredItemCount:
				if (BusinessHelper.IsRequirementMet(buildingRegistration, item.businessRequirement))
				{
					CompleteTodoTask(item);
				}
				break;
			case TodoTaskType.MissingSchedule:
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
				}
				else if (buildingRegistration.scheduleDays.Exists((ScheduleDay x) => x.isOpen))
				{
					CompleteTodoTask(item);
				}
				break;
			case TodoTaskType.NoProducers:
				if (BusinessHelper.IsThereAtLeastOnePrimaryProduct(buildingRegistration))
				{
					CompleteTodoTask(item);
				}
				break;
			case TodoTaskType.DirtyFloors:
			{
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
					break;
				}
				float cleanliness = buildingRegistration.GetCleanliness();
				if (Mathf.CeilToInt(cleanliness) >= 90)
				{
					CompleteTodoTask(item);
					break;
				}
				Priority newPriority = ((!(cleanliness > BuildingCleanlinessHelper.FloorTileCleanlinessStates[0])) ? ((cleanliness > BuildingCleanlinessHelper.FloorTileCleanlinessStates[1]) ? Priority.Medium : Priority.High) : Priority.Low);
				UpdateTodoTask(item, newPriority);
				break;
			}
			case TodoTaskType.EmployeeUnassigned:
			{
				EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(item.employeeId);
				if (employeeById == null || employeeById.IsAssignedToAnyBusiness() || employeeById.trainingSession != null)
				{
					CompleteTodoTask(item);
				}
				break;
			}
			case TodoTaskType.EmployeeIdle:
			{
				EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(item.employeeId);
				if (employeeById == null || employeeById.trainingSession != null || !employeeById.IsAssignedToAnyBusiness() || employeeById.IsAssignedToAnyWorkShift())
				{
					CompleteTodoTask(item);
				}
				break;
			}
			case TodoTaskType.BusinessTemporarilyClosed:
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
				}
				else if (!buildingRegistration.temporarilyClosed)
				{
					CompleteTodoTask(item);
				}
				break;
			case TodoTaskType.UnpaidLicensingFees:
				if (buildingRegistration == null)
				{
					CompleteTodoTask(item);
				}
				else if (SaveGameManager.Current.paidLicensingFeesToday.Exists(((Address, string) pair) => pair.Item1 == buildingRegistration.Address))
				{
					CompleteTodoTask(item);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case TodoTaskType.PayTaxes:
			case TodoTaskType.LowHunger:
			case TodoTaskType.LowHealth:
			case TodoTaskType.UnhappyEmployee:
			case TodoTaskType.BrokenFurniture:
			case TodoTaskType.VehicleNeedsRepair:
			case TodoTaskType.VehicleNeedsFuel:
				break;
			}
		}
	}

	public static LanguageChangeEventDataHolder GetTodoDescription(TodoTask task, bool showBusinessName = true)
	{
		LanguageChangeEventDataHolder result = default(LanguageChangeEventDataHolder);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(task.address);
		ItemInstance value = null;
		if (buildingRegistration != null && !string.IsNullOrEmpty(task.itemInstanceId))
		{
			buildingRegistration.itemInstances.TryGetValue(task.itemInstanceId, out value);
		}
		string text = ((buildingRegistration == null) ? null : (string.IsNullOrEmpty(buildingRegistration.BusinessName) ? buildingRegistration.Address.ToFormattedString() : buildingRegistration.BusinessName));
		if (showBusinessName && text != null)
		{
			result.Prefix = text + ": ";
		}
		switch (task.type)
		{
		case TodoTaskType.LowStock:
			result.Key = "alert_low_stock";
			if (value == null)
			{
				Debug.Log("No item instance found in " + task.address?.ToFormattedString() + " with id '" + task.itemInstanceId + "'");
				return result;
			}
			result.Arguments = new
			{
				itemname = task.itemName,
				producer_itemname = value.itemName
			};
			break;
		case TodoTaskType.EmptyStock:
			result.Key = "alert_empty_stock";
			if (value == null)
			{
				Debug.Log("No item instance found in " + task.address?.ToFormattedString() + " with id '" + task.itemInstanceId + "'");
				return result;
			}
			result.Arguments = new
			{
				itemname = task.itemName,
				producer_itemname = value.itemName
			};
			break;
		case TodoTaskType.MissingSchedule:
			result.Key = "alert_missing_schedule";
			break;
		case TodoTaskType.NoProducers:
			result.Key = "alert_no_producers";
			break;
		case TodoTaskType.DirtyFloors:
			result.Key = "alert_dirty_floors";
			break;
		case TodoTaskType.EmployeeUnassigned:
			result.Key = "tutorial_todotask_employeeunassigned";
			result.Arguments = new
			{
				employeename = EmployeeHelper.GetEmployeeById(task.employeeId)?.characterData.name
			};
			break;
		case TodoTaskType.EmployeeIdle:
			result.Key = "tutorial_todotask_employeeidle";
			result.Arguments = new
			{
				employeename = EmployeeHelper.GetEmployeeById(task.employeeId)?.characterData.name
			};
			break;
		case TodoTaskType.PayTaxes:
			result.Key = "tutorial_todotask_paytaxes";
			result.Arguments = new { task.remainingDays };
			break;
		case TodoTaskType.BusinessTemporarilyClosed:
			result.Key = "alert_business_temporarily_closed";
			break;
		case TodoTaskType.MissingRequiredItem:
		case TodoTaskType.MissingRequiredItemCount:
		{
			BusinessRequirement businessRequirement = BusinessTypeHelper.GetData(buildingRegistration)?.businessRequirements.Find((BusinessRequirement x) => x.businessRequirementName == task.businessRequirement);
			if (businessRequirement == null)
			{
				return result;
			}
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
				result.Key = "alert_missing_required_item_count";
				result.Arguments = new
				{
					itemname = businessRequirement.GetLocalizeKey(),
					count = num
				};
			}
			else
			{
				result.Key = "alert_missing_required_item";
				result.Arguments = new
				{
					itemname = businessRequirement.GetLocalizeKey()
				};
			}
			break;
		}
		case TodoTaskType.UnpaidLicensingFees:
			result.Key = "alert_unpaid_licensing_fees";
			break;
		case TodoTaskType.VehicleNeedsRepair:
			result.Key = "alert_vehicle_needs_repair";
			break;
		case TodoTaskType.VehicleNeedsFuel:
			result.Key = "alert_vehicle_needs_fuel";
			break;
		}
		return result;
	}

	public void ClickTask(TodoTask task)
	{
		switch (task.type)
		{
		case TodoTaskType.LowStock:
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(task.address, "InventoryPricing");
			break;
		case TodoTaskType.DirtyFloors:
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(task.address);
			break;
		case TodoTaskType.PayTaxes:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(SaveGameManager.Current.Contacts.Find((Contact x) => x.id == "internal_revenue_service"));
			break;
		case TodoTaskType.EmployeeUnassigned:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.ChangeTab("Employees");
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(EmployeeHelper.GetEmployeeById(task.employeeId));
			break;
		case TodoTaskType.LowHunger:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Persona);
			break;
		case TodoTaskType.LowHealth:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Persona);
			break;
		case TodoTaskType.EmptyStock:
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(task.address, "InventoryPricing");
			break;
		case TodoTaskType.EmployeeIdle:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.ChangeTab("Employees");
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(EmployeeHelper.GetEmployeeById(task.employeeId));
			break;
		case TodoTaskType.MissingSchedule:
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(task.address, "Schedule");
			break;
		case TodoTaskType.MissingRequiredItem:
		case TodoTaskType.NoProducers:
		case TodoTaskType.BusinessTemporarilyClosed:
		case TodoTaskType.MissingRequiredItemCount:
		case TodoTaskType.UnpaidLicensingFees:
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(task.address);
			break;
		case TodoTaskType.VehicleNeedsRepair:
		case TodoTaskType.VehicleNeedsFuel:
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(AutoTowServiceHelper.GetAutoTowContact());
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case TodoTaskType.UnhappyEmployee:
		case TodoTaskType.BrokenFurniture:
			break;
		}
	}

	public static Address GetDestination(TodoTask task)
	{
		switch (task.type)
		{
		case TodoTaskType.LowStock:
		case TodoTaskType.EmptyStock:
		case TodoTaskType.DirtyFloors:
		case TodoTaskType.MissingRequiredItem:
		case TodoTaskType.MissingSchedule:
		case TodoTaskType.NoProducers:
		case TodoTaskType.BusinessTemporarilyClosed:
		case TodoTaskType.MissingRequiredItemCount:
			return task.address;
		case TodoTaskType.PayTaxes:
			return TaxHelper.GetIRSAddress();
		default:
			return null;
		}
	}

	public void SetCollapsedState(bool collapsed)
	{
		SetCollapsedState(collapsed, bounce: false);
	}

	private void SetCollapsedState(bool collapsed, bool bounce)
	{
		IsCollapsed = collapsed;
		RectTransform component = container.GetComponent<RectTransform>();
		Sequence s = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		if (bounce & collapsed)
		{
			s.Append(component.DOPivot(new Vector2(0f - bounceFactor, 1f), bounceAnimationDuration));
		}
		s.Append(component.DOPivot(new Vector2(collapsed ? 1 : 0, 1f), collapseAnimationDuration));
		s.Join(expandButton.DOPivot(new Vector2((!collapsed) ? 1 : 0, 1f), collapseAnimationDuration));
		if (!collapsed)
		{
			_onExpand?.Invoke();
			_onExpand = null;
		}
	}

	public void PerformSwitchOutAnimation(Action onReopen, float reopenDelay)
	{
		if (_switchOutAnimationCoroutine != null)
		{
			StopCoroutine(_switchOutAnimationCoroutine);
		}
		_switchOutAnimationCoroutine = StartCoroutine(SwitchOutAnimation(onReopen, reopenDelay));
	}

	private IEnumerator SwitchOutAnimation(Action onReopen, float reopenDelay)
	{
		_onExpand?.Invoke();
		_onExpand = onReopen;
		if (!IsCollapsed)
		{
			SetCollapsedState(collapsed: true, bounce: true);
		}
		yield return new WaitForSecondsRealtime(reopenDelay);
		if (IsCollapsed && _onExpand != null)
		{
			SetCollapsedState(collapsed: false);
		}
	}

	private void OnDestroy()
	{
		deliveryJobUI.Dispose();
		foodDeliveryJobUI.Dispose();
		InteriorDesignerUI.OnInteriorDesignerToggle.RemoveListener(OnInteriorDesignerToggled);
		InteriorDesignerUI.onCloseInteriorDesigner = (Action)Delegate.Remove(InteriorDesignerUI.onCloseInteriorDesigner, new Action(CheckInteriorDesignerTasks));
		TutorialHelper.onQuestLoaded = (Action)Delegate.Remove(TutorialHelper.onQuestLoaded, new Action(ScheduleUpdateObjectivesHeight));
		if (_switchOutAnimationCoroutine != null)
		{
			StopCoroutine(_switchOutAnimationCoroutine);
		}
	}

	private void CheckInteriorDesignerTasks()
	{
		ScheduleUpdateCurrentBusinessTodoTasks();
		GameEvent.Invoke("ba:gameevent_interiorelementschanged");
	}
}
