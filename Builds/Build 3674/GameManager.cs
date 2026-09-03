using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BehaviorDesigner.Runtime;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Rivals;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using BlueprintsUI;
using Buildings;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Buildings.BuildingTypes.Special.FurnitureStore;
using Buildings.Indoors;
using Buildings.Indoors.InteriorDesign;
using Buildings.Office.Headquarters;
using BusinessLayoutSets;
using CameraControllers;
using Character.Customization;
using Cinemachine;
using Controllers;
using DG.Tweening;
using Dialogs;
using Entities;
using Extensions;
using GamePrompt.Runtime.Scripts;
using GleyTrafficSystem;
using Helpers;
using IngameDebugConsole;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Localizor;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Player.FoodDeliveryJob;
using Player.HUD.ItemInfoOverlays;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;
using PlayerActivity;
using PlayerActivity.Tennis;
using Scenes;
using Scenes.MainMenu;
using Seasons;
using Settings;
using Streets;
using TMPro;
using Tutorial;
using UI;
using UI.Components;
using UI.CustomUI;
using UI.Elements;
using UI.Guiders;
using UI.InteriorDesigner;
using UI.Load;
using UI.Notification;
using UI.Overlays;
using UI.Purchase;
using UI.PurchaseVehicle;
using UI.Smartphone;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Vehicles;

public class GameManager : InstanceBehavior<GameManager>
{
	public const int HourToDoDeliveries = 2;

	private const int MidnightSaveHour = 23;

	private const int MaxMidnightBankBalances = 7;

	public const float BaseMinutesValue = 1f;

	public const float CasinoMinutesValue = 0.5f;

	private const string ShadowCasterDebugViewShaderName = "ShadowCasterDebug";

	public static bool IsInFocus = true;

	private static Camera _mainCamera;

	public static Address hospitalAddress = new Address("ba:street_seventhstreet", 2);

	public static bool isCitySceneBeingUnloaded;

	private static float MinutesMultiplier;

	public static bool preventAutoSave;

	private static bool PendingMidnightAutoSave;

	[BoxGroup("Debug")]
	public GameScenes scenesToLoadOnStart;

	[BoxGroup("Debug")]
	public bool breakOnNewHour;

	[BoxGroup("Debug")]
	public bool breakOnNewDay;

	[BoxGroup("Debug")]
	public bool setInvincibilityOnStart;

	[BoxGroup("Debug")]
	[SerializeField]
	private CustomPassVolume passVolume;

	public static bool isShadowCasterDebugViewEnabled;

	public static bool hideStaticShadowCastersMeshRenderers;

	public PlayerController playerController;

	[HideInInspector]
	public VehicleController selectedVehicle;

	public Transform itemsContainer;

	public CinemachineVirtualCameraBase pedestrianCamera;

	public CinemachineVirtualCameraBase vehicleCamera;

	public CinemachineVirtualCameraBase vehicleCameraReverse;

	public CinemachineVirtualCameraBase indoorVehicleCamera;

	public CinemachineVirtualCameraBase indoorVehicleCameraReverse;

	public CinemachineVirtualCameraBase citymapCamera;

	public CinemachineVirtualCameraBase subwayCamera;

	public CinemachineVirtualCameraBase indoorCamera;

	public CinemachineVirtualCameraBase dummyCamera;

	public CinemachineVirtualCameraBase freeLookCamera;

	public CinemachineVirtualCameraBase indoorPlacementCamera;

	public CinemachineVirtualCameraBase buildingPreviewCamera;

	public CinemachineVirtualCameraBase boatCinemacticCamera;

	public CinemachineVirtualCameraBase vehiclePreviewCamera;

	public ScreenshotCaptureController buildingOutdoorsCameraCaptureController;

	public ScreenshotCaptureController saveGameCameraCaptureController;

	public MouseSettings mouseSettings;

	public float signEmissionIntensity = 2f;

	public List<GenericPersonalGoal> personalGoals;

	public CoroutineManager coroutineManager;

	public EmployeeUniformPreview employeeUniformPreview;

	public TimeOfDayController timeOfDayController;

	public bool useSaveGameTypeJson = true;

	public RadioPlayer radioPlayer;

	public RainAudio rainAudio;

	public bool isTrailerMode;

	public ParticleSystem navigationParticleSystem;

	public bool IsUIDevScene;

	public bool ForceLOD0;

	private float _nextAutoSave;

	private int _customSecondsBetweenAutoSaves;

	private bool _pendingUpdateSecurityLevel;

	private AsyncOperationHandle<IList<GenericPersonalGoal>> _personalGoalsHandle;

	[NonSerialized]
	public bool spawnTraffic = true;

	[NonSerialized]
	public readonly Queue<BuildingRegistration> pendingRetailPriceRecalculations = new Queue<BuildingRegistration>();

	[NonSerialized]
	public readonly Queue<BuildingRegistration> pendingDailyValuationUpdates = new Queue<BuildingRegistration>();

	[NonSerialized]
	public bool shouldUpdateAfterDeliveries;

	private static CustomPass _shadowCasterDebugPass;

	private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

	private static readonly TransactionInfo CheatTransactionInfo = new TransactionInfo("ba:transaction_cheat");

	public const string WaypointPlayerPrefsKey = "tpwWaypoints";

	[BoxGroup("BuildingHider")]
	public float Height;

	[BoxGroup("BuildingHider")]
	public float HeightOffset;

	[BoxGroup("BuildingHider")]
	public float NoiseScale = 20f;

	private static readonly int PoHeightID = Shader.PropertyToID("_PO_Height");

	private static readonly int PoHeightOffsetID = Shader.PropertyToID("_PO_Height_Offset");

	private static readonly int ScaleID = Shader.PropertyToID("_NoiseScale");

	public static bool IsPlayerWalking => InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.isActiveAndEnabled;

	public static bool IsDevMode => false;

	public static bool IsCustomGame => SaveGameManager.Current.gameVariables.difficulty == Difficulty.Custom;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsInFocus = true;
		_mainCamera = null;
		isCitySceneBeingUnloaded = false;
		MinutesMultiplier = 1f;
		preventAutoSave = false;
		PendingMidnightAutoSave = false;
	}

	protected override void Awake()
	{
		base.Awake();
		if (!base.IsMainInstance)
		{
			return;
		}
		Resources.UnloadUnusedAssets();
		TimeHelper.use12h = PlayerPrefSettings.use12h;
		UnitHelper.useImperial = PlayerPrefSettings.useImperial;
		CultureHelper.UpdateStoredCultureInfo();
		LocalizorManager.showNonCriticalWarnings = !IsDevMode;
		SetMinutesMultiplier(PlayerPrefSettings.GameSpeed);
		SetBuildingCutout();
		DebugLogConsole.AddCustomParameterType(typeof(Timestamp), TimeHelper.DebugConsole_ParseTimestamp);
		CommandHelper.AddCommand<string>("SetLanguage", "Sets New Language", LocalizorManager.SetUsedLanguage);
		CommandHelper.AddCommand<int>("SetLocalizationMode", "Sets New Language", LocalizorManager.SetLocalizationModeThroughCommand);
		CommandHelper.AddCommand("LoadTemp", "Loads Strings from temp.json", delegate
		{
			LocalizorManager.LoadTemp(invokeLanguageChangeEvent: true);
		});
		CommandHelper.AddCommand<int, Month>("OverrideDate", "Overrides system date.", DateHelper.OverrideSystemDate);
		CommandHelper.AddCommand<bool>("OverrideDate", "Overrides system date. Toggle uses date set previously.", DateHelper.OverrideSystemDate);
		CommandHelper.AddCommand<SeasonName>("OverrideDate", "Overrides system date. Set date to the first day of the selected season", DateHelper.OverrideSystemDate);
		if (SaveGameManager.Current == null)
		{
			if (!SaveGameManager.Load(null, loadScene: false) || SaveGameManager.Current == null)
			{
				return;
			}
			if (scenesToLoadOnStart != 0)
			{
				LoadScene.LoadScenes(scenesToLoadOnStart, skipFadeOut: true);
			}
		}
		else
		{
			ItemHelper.Init();
		}
		MouseController.Init(mouseSettings);
		DOTween.Init();
		InteriorElementsHelper.Init();
		WallsVisibilityHelper.ToggleWalls(SaveGameManager.Current.wallsVisibility);
		CitizenHelper.Init();
		ProductMarketHelper.Init();
		EmployeeHelper.Init();
		VehicleHelper.Init(this);
		LoadingAsyncTaskManager.AddTask(BusinessLayoutSetHelper.Init());
		CustomerEntriesHelper.Init();
		ParkingSimulator.ResetPool();
		_personalGoalsHandle = Addressables.LoadAssetsAsync<GenericPersonalGoal>("PersonalGoals", null);
		personalGoals = _personalGoalsHandle.WaitForCompletion().ToList();
		EnergyHelper.Init();
		RivalsHelper.FillData(SaveGameManager.Current.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		FurnitureDeliveryHelper.Init();
		FoodDeliveryHelper.Init();
		AutoTowServiceHelper.Init();
		FoodDeliveryJobHelper.Init();
		BusinessSimulatorHelper.Init();
		SpecialNpcHelper.Init();
		TutorialPointersManager.Init();
		CameraHelper.Init(new List<CinemachineVirtualCameraBase>
		{
			InstanceBehavior<GameManager>.Instance.citymapCamera,
			InstanceBehavior<GameManager>.Instance.pedestrianCamera,
			InstanceBehavior<GameManager>.Instance.vehicleCamera,
			InstanceBehavior<GameManager>.Instance.vehicleCameraReverse,
			InstanceBehavior<GameManager>.Instance.indoorVehicleCamera,
			InstanceBehavior<GameManager>.Instance.indoorVehicleCameraReverse,
			InstanceBehavior<GameManager>.Instance.subwayCamera,
			InstanceBehavior<GameManager>.Instance.indoorCamera,
			InstanceBehavior<GameManager>.Instance.dummyCamera,
			InstanceBehavior<GameManager>.Instance.freeLookCamera,
			InstanceBehavior<GameManager>.Instance.indoorPlacementCamera,
			InstanceBehavior<GameManager>.Instance.buildingPreviewCamera,
			InstanceBehavior<GameManager>.Instance.boatCinemacticCamera,
			InstanceBehavior<GameManager>.Instance.vehiclePreviewCamera
		});
		PlacementHelper.Init(InstanceBehavior<GameManager>.Instance.indoorPlacementCamera.GetComponentInParent<PlacementCam>(), InstanceBehavior<GameManager>.Instance.indoorCamera.GetComponent<PedestrianCam>(), InstanceBehavior<GameManager>.Instance.pedestrianCamera.GetComponent<PedestrianCam>());
		ResetNextAutoSave();
		if (Debug.isDebugBuild)
		{
			LocalizorManager.SetLocalizationMode(LocalizorSettings.LocalizationMode.HalfTranslated);
		}
		if (SteamAPI.StatsRecieved)
		{
			ForceUpdateAchievementsOnSteam();
		}
		else
		{
			Singleton<SteamAPI>.Instance.onSteamUserStatsReceived.AddListener(ForceUpdateAchievementsOnSteam);
		}
		if (ForceLOD0)
		{
			GlobalEvents.RegisterOnGameLoadedCallback(delegate
			{
				LODGroup[] array = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					array[i].ForceLOD(0);
				}
			});
		}
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GamePromptManager.StartCollecting();
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				InteriorDesignerHelper.Init(timeOfDayController, indoorPlacementCamera.GetComponentInParent<PlacementCam>(), blueprintCreatorMode: false);
			});
		});
		GlobalEvents.RegisterOnGameLoadedLateCallback(TutorialHelper.Init);
	}

	private void Start()
	{
		GlobalEvents.RegisterOnGameLoadedLateCallback(delegate
		{
			InstanceBehavior<UIs>.Instance.options.RefreshSongs();
			FolderWatcherHelper.StartWatching(RadioPlayer.GetRadioPath(), InstanceBehavior<UIs>.Instance.options.RefreshSongs);
			EnsureAvailableCinemaTheater.ApplyFix();
		});
		DebugLogConsole.onCommandExecuted = (Action)Delegate.Combine(DebugLogConsole.onCommandExecuted, new Action(InputActionHelper.ResetAllActions));
		AntiAliasingHelper.SetAntiAliasingSetting(AntiAliasingHelper.GetPlayerAntiAliasingSetting());
		if (PlayerPrefs.HasKey(PlayerPref.vSyncAndFPSLimitV2))
		{
			Options.SetFPSSetting(PlayerPrefSettings.vSyncAndFPSLimitV2);
		}
		RebindActionUI.BindingChanged.AddListener(delegate
		{
			GlobalEvents.onBindingsChanged();
		});
		InputHelper.SetupPlayerInput();
		GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(coroutineManager.StopRunningCoroutines));
		GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, new Action(PrefabHelper.OnGameUnloaded));
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		ScheduleHelper.OnWorkShiftChanged.AddListener(OnWorkShiftChanged);
		ScheduleHelper.OnOpeningHourChanged.AddListener(OnOpeningHourChanged);
		GlobalEvents.onSaveGame = (Action)Delegate.Combine(GlobalEvents.onSaveGame, (Action)delegate
		{
			BusinessSimulatorHelper.Work.ForceCompleteAllWork();
			LogisticsManagerHelper.FactoryDeliveriesWork.ForceCompleteAllWork();
			LogisticsManagerHelper.WarehouseDeliveriesWork.ForceCompleteAllWork();
			EmployeeInstance.UpdateSatisfactionWork.ForceCompleteAllWork();
		});
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, (Action<bool>)delegate(bool paused)
		{
			if (paused)
			{
				BusinessSimulatorHelper.Work.ForceCompleteAllWork();
			}
		});
	}

	private void Update()
	{
		Shader.SetGlobalFloat(UnscaledTimeId, Time.unscaledTime);
		if (LoadScene.isLoading || PlayerHelper.playerDead)
		{
			return;
		}
		ParkingSimulator.parkingQueueWorker.Process();
		GlobalKeyEvents();
		if ((bool)InstanceBehavior<UIs>.Instance.timeMachine && InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			EmployeeInstance.UpdateSatisfactionWork.ForceCompleteAllWork();
		}
		else if (CasinoBoatManager.IsOnCasinoBoat)
		{
			RunMainGameTick(Time.deltaTime * 0.5f);
		}
		else
		{
			RunMainGameTick(Time.deltaTime * MinutesMultiplier);
		}
		InstanceBehavior<UIs>.Instance.gameSpeed?.UpdateVisuals();
		CompetitionHelper.ProcessPendingRetailPriceRecalculations();
		CompetitionHelper.ProcessPendingDailyValuationUpdates();
		Contact.ShowAddedContactNotifications();
		UpdateFaintVignette();
		CityMapHider.Work.ProgressWork();
		BusinessSimulatorHelper.ProgressWork();
		if (!BusinessSimulatorHelper.Work.HasPendingWork)
		{
			LogisticsManagerHelper.FactoryDeliveriesWork.ProgressWork();
			if (!LogisticsManagerHelper.FactoryDeliveriesWork.HasPendingWork)
			{
				LogisticsManagerHelper.WarehouseDeliveriesWork.ProgressWork();
			}
			if (!LogisticsManagerHelper.FactoryDeliveriesWork.HasPendingWork && !LogisticsManagerHelper.WarehouseDeliveriesWork.HasPendingWork)
			{
				EmployeeInstance.UpdateSatisfactionWork.ProgressWork();
			}
		}
	}

	public static void EnqueueMainThreadAction(Action action)
	{
		Dispatcher.Invoke(action);
	}

	private void LateUpdate()
	{
		CheckAutoSave();
	}

	protected override void OnDestroy()
	{
		if (base.IsMainInstance)
		{
			base.OnDestroy();
			DebugLogConsole.onCommandExecuted = (Action)Delegate.Remove(DebugLogConsole.onCommandExecuted, new Action(InputActionHelper.ResetAllActions));
			FolderWatcherHelper.StopWatching(RadioPlayer.GetRadioPath());
			if (_personalGoalsHandle.IsValid())
			{
				Addressables.Release(_personalGoalsHandle);
			}
			SaveGameManager.JoinSaveGameThreads();
			GameEvent.onGameEventTriggered = null;
			ScheduleHelper.OnWorkShiftChanged.RemoveListener(OnWorkShiftChanged);
			ScheduleHelper.OnOpeningHourChanged.RemoveListener(OnOpeningHourChanged);
			EmployeeHelper.IsInitialized = false;
			CityMapHider.Work.DiscardPendingWork();
			BusinessSimulatorHelper.Work.DiscardPendingWork();
			LogisticsManagerHelper.WarehouseDeliveriesWork.DiscardPendingWork();
			LogisticsManagerHelper.FactoryDeliveriesWork.DiscardPendingWork();
			EmployeeInstance.UpdateSatisfactionWork.DiscardPendingWork();
			SewerSteam.Instances.Clear();
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		IsInFocus = hasFocus;
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		IsInFocus = !pauseStatus;
	}

	private void OnApplicationQuit()
	{
		isCitySceneBeingUnloaded = true;
		SaveGameManager.JoinSaveGameThreads();
	}

	public void OnValidate()
	{
		SetBuildingCutout();
	}

	private void OnExitBuilding(Address address)
	{
		InteriorElement.ClearMaterialVariantsCache();
	}

	public static Camera GetMainCamera()
	{
		if (!_mainCamera)
		{
			_mainCamera = Camera.main;
		}
		return _mainCamera;
	}

	[ContextMenu("ForceUpdateAchievementsOnSteam")]
	private void ForceUpdateAchievementsOnSteam()
	{
		foreach (GenericPersonalGoal item in personalGoals.Where((GenericPersonalGoal goal) => goal.IsCompleted && goal.usesSteamAchievements))
		{
			item.ForceUpdateOnSteam();
		}
		Singleton<SteamAPI>.Instance.onSteamUserStatsReceived.RemoveListener(ForceUpdateAchievementsOnSteam);
	}

	public static void SetPreventAutoSave(bool newPreventAutosaveValue)
	{
		preventAutoSave = newPreventAutosaveValue;
		if (!newPreventAutosaveValue)
		{
			TryRunPendingMidnightAutoSave();
		}
	}

	private void CheckAutoSave()
	{
		if (_nextAutoSave <= Time.unscaledTime && !preventAutoSave && !PlayerHelper.playerDead)
		{
			if (!SaveGameManager.HasChangesSinceLastSave())
			{
				_nextAutoSave = Time.unscaledTime + 5f;
			}
			else if (SaveGameManager.Save(SaveGameManager.SaveType.RecoverSave))
			{
				ResetNextAutoSave();
				Notifications.Show(NotificationType.Success, "gamemanager_notification_autosave_started", null, 4f, null, null, notificationSound: false);
			}
		}
	}

	public void ResetNextAutoSave()
	{
		float num = ((_customSecondsBetweenAutoSaves > 0) ? ((float)_customSecondsBetweenAutoSaves) : ((float)PlayerPrefSettings.MinutesBetweenAutoSaves * 60f));
		_nextAutoSave = Time.unscaledTime + num;
	}

	public void RunMainGameTick(float deltaTimeWithMultiplier)
	{
		MouseController.Run();
		PlacementHelper.Run();
		if (_pendingUpdateSecurityLevel)
		{
			BusinessHelper.UpdateAllSecurityLevels();
			_pendingUpdateSecurityLevel = false;
		}
		SaveGameManager.Current.Minute += deltaTimeWithMultiplier;
		while (SaveGameManager.Current.Minute >= 60f)
		{
			_pendingUpdateSecurityLevel = true;
			BusinessSimulatorHelper.RunHourly();
			SaveGameManager.Current.Hour++;
			SaveGameManager.Current.Minute -= 60f;
			if (SaveGameManager.Current.Hour >= 24)
			{
				SaveGameManager.Current.Hour -= 24;
				NewDay();
			}
			else
			{
				GlobalEvents.onNewHour?.Invoke();
			}
			JobHelper.RunHourly();
			ParkingSimulator.RunHourly();
			RecruitmentHelper.RunHourly();
			HappinessHelper.RunHourly();
			EmployeeHelper.RunHourly();
			PricingManagerHelper.RunHourly();
			bool flag = DeliveryHelper.IsRegularDeliveryHour();
			if (flag && TimeHelper.GetDayOfWeek(TimeHelper.CurrentDay) == DayOfWeekOrdered.Monday)
			{
				ProductMarketHelper.GenerateShortagesAndBackorders();
			}
			bool num = SaveGameManager.Current.Hour == 2;
			if (num)
			{
				LogisticsManagerHelper.DoAllFactoryDeliveries();
			}
			BusinessHelper.HandleWholesaleDeliveries();
			if (flag)
			{
				ImportPartnership.DoAllDeliveries();
			}
			if (num)
			{
				LogisticsManagerHelper.DoAllWarehouseDeliveries();
			}
			BusinessHelper.RunHourly();
			BuildingCleanlinessHelper.RunHourly();
			FurnitureDeliveryHelper.RunHourly();
			VehicleDeliveryHelper.RunHourly();
			FoodDeliveryHelper.RunHourly();
			RivalsHelper.RunHourly();
			GamePromptHelper.RunHourly();
			FoodDeliveryJobHelper.RunHourly();
			ContactsHelper.RunHourly();
			GameEvent.Invoke("ba:gameevent_newhour");
			InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(null, DynamicOverlayUpdateType.HourChangeUpdate);
			RunMidNightAutoSave();
			if (breakOnNewHour)
			{
				Debug.Break();
			}
		}
		EnergyHelper.UpdateEnergy(deltaTimeWithMultiplier);
		if (shouldUpdateAfterDeliveries && !LogisticsManagerHelper.FactoryDeliveriesWork.HasPendingWork && !LogisticsManagerHelper.WarehouseDeliveriesWork.HasPendingWork)
		{
			shouldUpdateAfterDeliveries = false;
			GameEvent.Invoke("ba:gameevent_itemcargochanged");
			if (BuildingManager.IsInsideBuilding)
			{
				GameEvent.Invoke("ba:gameevent_enteredbuilding");
			}
		}
	}

	private void NewDay()
	{
		SaveGameManager.Current.Day++;
		PlayerHelper.IncreasePlayerAge();
		GlobalEvents.onNewHour?.Invoke();
		NotificationsListUI.CleanOldNotifications();
		RealEstateHelper.RunDaily();
		EmployeeHelper.PayDailyWages();
		EmployeeHelper.WorkDaily();
		ParkingSimulator.RunDaily();
		BusinessHelper.RunDaily();
		CompetitionHelper.RunDaily();
		ProductMarketHelper.RunDaily();
		GlobalEvents.onNewDay?.Invoke();
		SaveGameManager.Current.deliveryJobLocationsDoneToday?.Clear();
		InstanceBehavior<AdManager>.Instance.RunDaily();
		TaxHelper.RunDaily();
		InstanceBehavior<UIs>.Instance.dailySummary.Run();
		RecordMidnightBankBalance();
		GameEvent.Invoke("ba:gameevent_newday");
		InvestmentFundHelper.RunDaily();
		SaveGameManager.Current.EnergyGeneratedFromConsumables = 0f;
		PortraitGenerator.Create(SaveGameManager.Current.charactersData.First(), null, InstanceBehavior<UIs>.Instance.topBar.avatar);
		EmployeeHelper.RunDaily();
		RivalsHelper.RunDaily();
		EnsureAvailableCinemaTheater.ApplyFix();
		Resources.UnloadUnusedAssets();
		if (breakOnNewDay)
		{
			Debug.Break();
		}
	}

	private static void RunMidNightAutoSave()
	{
		if (SaveGameManager.Current.Hour == 23 && !EnergyHelper.goingToHospital)
		{
			if (preventAutoSave)
			{
				PendingMidnightAutoSave = true;
			}
			else
			{
				ExecuteMidnightAutoSave();
			}
		}
	}

	private static void TryRunPendingMidnightAutoSave()
	{
		if (PendingMidnightAutoSave)
		{
			ExecuteMidnightAutoSave();
		}
	}

	private static void ExecuteMidnightAutoSave()
	{
		if (SaveGameManager.Save(SaveGameManager.SaveType.MidnightSave))
		{
			PendingMidnightAutoSave = false;
			Notifications.Show(NotificationType.Success, "gamemanager_notification_autosave_started", null, 4f, null, null, notificationSound: false);
		}
	}

	public static bool ShouldBlockKeyboardShortcuts()
	{
		if ((!DebugLogManager.Instance || !DebugLogManager.Instance.IsLogWindowVisible) && !HelpSystem.IsVisible && !Options.IsVisible && !PreviewTerminalUI.IsVisible)
		{
			return HasInputSelected();
		}
		return true;
	}

	public static bool HasInputSelected()
	{
		if (ScrollBarDraggingComponent.isScrollBarBeingDragged)
		{
			return true;
		}
		if ((bool)EventSystem.current?.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.activeInHierarchy && (EventSystem.current.currentSelectedGameObject.layer == LayerHelper.UiLayerIndex || EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.layer == LayerHelper.UiLayerIndex))
		{
			return true;
		}
		return false;
	}

	private void GlobalKeyEvents()
	{
		if (DebugLogManager.Instance.IsLogWindowVisible || CameraHelper.GetCurrentCamera() == InstanceBehavior<GameManager>.Instance.boatCinemacticCamera)
		{
			return;
		}
		if (PlayerAction.Cancel.Pressed())
		{
			CancelButtonHandler.HandleEscapeClick();
		}
		if (PlayerAction.NextOption.Pressed() && (bool)EventSystem.current.currentSelectedGameObject)
		{
			Selectable component = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
			if ((bool)component)
			{
				Selectable selectable = (PlayerAction.PerformActionWithoutConfirm.Pressing() ? (component.FindSelectable(Vector3.up) ?? component.FindSelectable(Vector3.left)) : (component.FindSelectable(Vector3.down) ?? component.FindSelectable(Vector3.right)));
				if ((bool)selectable)
				{
					EventSystem.current.SetSelectedGameObject(selectable.gameObject);
					if (selectable.TryGetComponent<TMP_InputField>(out var component2))
					{
						component2.stringPosition = 0;
						component2.selectionStringAnchorPosition = 0;
						component2.selectionStringFocusPosition = component2.text.Length;
					}
				}
			}
		}
		if (HasInputSelected() || BlueprintsPanel.IsOpen)
		{
			return;
		}
		if (PlayerAction.Menu.Pressed())
		{
			SignAppearance signAppearance = InstanceBehavior<UIs>.Instance.draggableWindows.signAppearance;
			if ((bool)signAppearance && signAppearance.IsOpen)
			{
				signAppearance.Close();
				return;
			}
			InstanceBehavior<UIs>.Instance.miniMenuUI.Toggle();
		}
		if (PlayerActivityUI.IsPanelOpen)
		{
			float num = 0f;
			if (PlayerAction.SliderLeft.Pressed())
			{
				num--;
			}
			if (PlayerAction.SliderRight.Pressed())
			{
				num++;
			}
			if (num != 0f)
			{
				InstanceBehavior<UIs>.Instance.playerActivityUI.ChangeSliderValue(num, exactMinutes: true);
			}
		}
		if (!Feedback.IsOpen)
		{
			if (PlayerAction.Confirm.Pressed())
			{
				if (HudConfirm.isOpen)
				{
					HudConfirm.onConfirm?.Invoke();
				}
				else if (InstanceBehavior<UIs>.Instance.fullMenu.schedule.scheduleConfirm.gameObject.activeInHierarchy)
				{
					InstanceBehavior<UIs>.Instance.fullMenu.schedule.scheduleConfirm.ClickConfirm();
				}
				else if (!FullMenu.IsOpen && !CityMap.IsOpen)
				{
					if (PlayerActivityUI.IsPanelOpen)
					{
						InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.Confirm);
					}
					else if (OverlayUI.IsVisible)
					{
						InstanceBehavior<UIs>.Instance.overlayUI.OnPlayerActionPressed(PlayerAction.Confirm);
					}
				}
			}
			if (PlayerAction.Click.Pressed() && UI.Elements.Dropdown.currentDropdown != null)
			{
				UI.Elements.Dropdown.currentDropdown.ClickAction();
			}
			if (!FullMenu.IsOpen && !CityMap.IsOpen && !BuildingPreview.isPreviewing)
			{
				if (PlayerAction.Interact.Pressed() && !CasinoBoatManager.boatSailInSequenceStarted)
				{
					if (PurchaseUI.IsPanelOpen)
					{
						if (InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.orderButton.interactable)
						{
							InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.PlaceOrder();
						}
						PlayerAction.Interact.Reset();
					}
					else if (PurchaseVehicleUI.IsPanelOpen)
					{
						PlayerAction.Interact.Reset();
						InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.Purchase();
					}
					else if (InstanceBehavior<UIs>.Instance.playerHUD.jobOfferPanel.isPanelOpen)
					{
						PlayerAction.Interact.Reset();
						InstanceBehavior<UIs>.Instance.playerHUD.jobOfferPanel.AcceptJob();
					}
					else if (PlayerActivityUI.IsPanelOpen)
					{
						InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.Interact);
					}
					else if (OverlayUI.IsVisible)
					{
						InstanceBehavior<UIs>.Instance.overlayUI.OnPlayerActionPressed(PlayerAction.Interact);
					}
				}
				if (PlayerAction.SecondaryInteract.Pressed())
				{
					if (PlayerActivityUI.IsPanelOpen)
					{
						InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.SecondaryInteract);
					}
					else if (OverlayUI.IsVisible)
					{
						InstanceBehavior<UIs>.Instance.overlayUI.OnPlayerActionPressed(PlayerAction.SecondaryInteract);
					}
				}
				if (PlayerAction.Sell.Pressed() && PlayerActivityUI.IsPanelOpen)
				{
					InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.Sell);
				}
			}
		}
		if (PlayerAction.OpenHelp.Released())
		{
			if (Feedback.IsOpen)
			{
				InstanceBehavior<Feedback>.Instance.Toggle(show: false);
			}
			InstanceBehavior<HelpSystem>.Instance.Toggle();
		}
		if (PlayerAction.OpenBugReport.Released())
		{
			if (HelpSystem.IsVisible)
			{
				InstanceBehavior<HelpSystem>.Instance.Toggle(show: false);
			}
			if (!SaveGameManager.IsModdedSave)
			{
				InstanceBehavior<Feedback>.Instance.Toggle();
			}
		}
		if (ShouldBlockKeyboardShortcuts())
		{
			return;
		}
		if (PlayerAction.QuickSave.Pressed())
		{
			if (preventAutoSave)
			{
				Notifications.ShowError("notification_cant_save", "notification_cant_save");
				return;
			}
			Input.ResetInputAxes();
			InstanceBehavior<UIs>.Instance.miniMenuUI.SaveGame();
		}
		if (PlayerAction.OpenMap.Pressed() && CityMap.CanOpenMap())
		{
			if (!FullMenu.IsOpen)
			{
				InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
			}
			else if (!CityMap.IsOpen)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
				InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
			}
		}
		if (PlayerAction.OpenBizman.Pressed())
		{
			FullMenu fullMenu = InstanceBehavior<UIs>.Instance.fullMenu;
			if (fullMenu.bizMan.gameObject.activeInHierarchy)
			{
				fullMenu.CloseFullMenu();
			}
			else if (BizMan.CanOpenFromShortcut())
			{
				fullMenu.bizMan.Open();
			}
		}
		if (PlayerAction.OpenNotifications.Pressed())
		{
			InstanceBehavior<UIs>.Instance.notificationsListUI.Toggle();
		}
		if (PlayerAction.Pause.Pressed())
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.TogglePause();
		}
		if (DebugLogManager.Instance.toggleWithKey)
		{
			if (Input.GetKeyDown(KeyCode.F7) && !FullMenu.IsOpen)
			{
				InstanceBehavior<UIs>.Instance.screenshot.ToggleUIVisibility();
			}
			if (Input.GetKeyDown(KeyCode.F8) && !FullMenu.IsOpen)
			{
				InstanceBehavior<UIs>.Instance.screenshot.ToggleFreeLookCamera();
			}
		}
		if (PlayerAction.AutoRun.Pressed() && !FullMenu.IsOpen && !CityMap.IsOpen && !Feedback.IsOpen && !HelpSystem.IsVisible && !PlacementSystem.IsInPlacementMode && !InteriorDesignerUI.IsOpen && (selectedVehicle == null || selectedVehicle.vehicleType.spawnInPlayerObject))
		{
			if (InputHelper.autoRunToggled)
			{
				playerController.ResetNavigation();
			}
			InputHelper.autoRunToggled = !InputHelper.autoRunToggled;
			if (InputHelper.autoRunToggled && MouseController.currentTargetEntity != null)
			{
				MouseController.ResetCurrentEntitySelected(MouseController.currentTargetEntity);
			}
		}
		if (PlayerAction.SkipSong.Pressed() && !InteriorDesignerUI.IsOpen)
		{
			if (CasinoBoatManager.IsOnCasinoBoat)
			{
				if (!CityMap.IsOpen && !FullMenu.IsOpen && !Feedback.IsOpen)
				{
					InstanceBehavior<CasinoBoatManager>.Instance.boatMusic.SkipSong();
				}
			}
			else if (InstanceBehavior<UIs>.Instance.smartphoneUI.radioControls.radioPlaying && !CityMap.IsOpen && !FullMenu.IsOpen && !Feedback.IsOpen)
			{
				radioPlayer.PlayNextStation();
			}
		}
		if (PlayerAction.SpecialInteract.Pressed() && !FullMenu.IsOpen && !CityMap.IsOpen && !Feedback.IsOpen)
		{
			if (InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.autoParkButton.gameObject.activeSelf && InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.autoParkButton.IsInteractable())
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.ClickAutoPark();
			}
			if (PlayerActivityUI.IsPanelOpen)
			{
				InstanceBehavior<UIs>.Instance.playerActivityUI.OnPlayerActionPressed(PlayerAction.SpecialInteract);
			}
			else if (OverlayUI.IsVisible)
			{
				InstanceBehavior<UIs>.Instance.overlayUI.OnPlayerActionPressed(PlayerAction.SpecialInteract);
			}
		}
	}

	public bool SaveGame(string saveGameName, bool skipSoundAndNotification = false)
	{
		bool num = SaveGameManager.Save(SaveGameManager.SaveType.Default, saveGameName);
		if (num)
		{
			StartCoroutine(CheckForSuccessfulSave(saveGameName, skipSoundAndNotification));
		}
		return num;
	}

	private IEnumerator CheckForSuccessfulSave(string saveGameName, bool skipSoundAndNotification)
	{
		yield return new WaitUntil(() => !SaveGameManager.SavingGameInProgress);
		if (!skipSoundAndNotification)
		{
			UiSoundHelper.Play(UiSound.SaveSuccessful);
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "name", saveGameName } };
			Notifications.Show(NotificationType.Success, "gamemanager_notification_save_successfull", notificationData);
		}
	}

	private static void OnGameEventTriggered(string gameEvent)
	{
		if (GameEvent.AutosaveTriggers.Contains(gameEvent) && SaveGameManager.MarkChange() && Application.isEditor)
		{
			Debug.Log("Will auto-save due to event: " + gameEvent);
		}
	}

	private static void OnWorkShiftChanged()
	{
		SaveGameManager.MarkChange();
	}

	private static void OnOpeningHourChanged(int hour, bool isOpen)
	{
		SaveGameManager.MarkChange();
	}

	[ConsoleMethod("SetMoney", "Set Player Money", new string[] { })]
	public static void Command_SetMoney(float amount)
	{
		ChangeMoney(amount - SaveGameManager.Current.Money, CheatTransactionInfo);
	}

	public static void Command_SetMoney(string amount)
	{
		float value;
		if (amount.Equals("i", StringComparison.OrdinalIgnoreCase))
		{
			ChangeMoney(float.MaxValue - SaveGameManager.Current.Money, CheatTransactionInfo);
		}
		else if (amount.FromShortCurrencyFormat(out value))
		{
			ChangeMoney(value - SaveGameManager.Current.Money, CheatTransactionInfo);
		}
	}

	[ConsoleMethod("winCasinoMoney", "Win casino money", new string[] { })]
	public static void Command_WinCasinoMoney(float amount)
	{
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino");
		ChangeMoney(amount, transactionInfo);
	}

	[ConsoleMethod("addMoney", "Add Money to Player", new string[] { })]
	public static void Command_ChangeMoney(float amount)
	{
		ChangeMoney(amount, CheatTransactionInfo);
	}

	[ConsoleMethod("addMoney", "Add Money to Player", new string[] { })]
	public static void Command_ChangeMoney(string amount)
	{
		float value;
		if (amount.Equals("i", StringComparison.OrdinalIgnoreCase))
		{
			ChangeMoney(float.MaxValue - SaveGameManager.Current.Money, CheatTransactionInfo);
		}
		else if (amount.FromShortCurrencyFormat(out value))
		{
			ChangeMoney(value, CheatTransactionInfo);
		}
	}

	[ConsoleMethod("addHunger", "Add Hunger to Player", new string[] { })]
	public static void Command_ChangeHunger(int amount)
	{
		InstanceBehavior<GameManager>.Instance.ChangeHunger(amount);
	}

	[ConsoleMethod("addAge", "Add Age to Player", new string[] { })]
	public static void Command_ChangeAge(float amount)
	{
		PlayerHelper.CharacterData.ageInDays += TimeHelper.GetDaysByYears(amount);
		InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.UpdateVisualAge();
	}

	public void ChangeHunger(int amount)
	{
		EnergyHelper.SetCurrentHunger(Mathf.Min(SaveGameManager.Current.Hunger + (float)amount, 100f));
		GameEvent.Invoke("ba:gameevent_foodeaten");
	}

	[ConsoleMethod("addHappiness", "Add Happiness to Player", new string[] { })]
	public static void Command_ChangeHappiness(int amount)
	{
		InstanceBehavior<GameManager>.Instance.ChangeHappiness(amount);
	}

	public void ChangeHappiness(int amount)
	{
		SaveGameManager.Current.Happiness += amount;
	}

	public static bool ChangeMoneySafe(float amount, TransactionInfo transactionInfo, int? dayOfTransaction = null, Address address = null, bool force = false, bool showNotification = false)
	{
		if (amount == 0f)
		{
			return true;
		}
		if (amount < 0f && SaveGameManager.Current.Money < 0f - amount && !force)
		{
			if (showNotification && (bool)InstanceBehavior<UIs>.Instance)
			{
				Notifications.ShowInsufficientMoney();
			}
			return false;
		}
		ChangeMoney(amount, transactionInfo, dayOfTransaction, address);
		return true;
	}

	private static void ChangeMoney(float amount, TransactionInfo transactionInfo, int? dayOfTransaction = null, Address address = null)
	{
		if (amount == 0f)
		{
			return;
		}
		amount = Mathf.Clamp(amount, float.MinValue + SaveGameManager.Current.Money, float.MaxValue - SaveGameManager.Current.Money);
		List<string> categories = transactionInfo.Categories;
		if (categories != null && categories.Contains("ba:transactioncategory_casino"))
		{
			if (amount > 0f)
			{
				SaveGameManager.Current.achievementsData.totalCasinoWin += amount;
				SaveGameManager.Current.CurrentTaxPeriodGamblingWinnings += amount;
			}
			else
			{
				SaveGameManager.Current.CurrentTaxPeriodGamblingLosses -= amount;
			}
		}
		InstanceBehavior<UIs>.Instance.topBar.AddMoneyTransaction(amount);
		SaveGameManager.Current.Money += amount;
		Transaction transaction = new Transaction(transactionInfo)
		{
			amount = amount,
			address = address,
			balance = Mathf.Floor(SaveGameManager.Current.Money)
		};
		if (dayOfTransaction.HasValue)
		{
			transaction.timestamp.Day = dayOfTransaction.Value;
		}
		SaveGameManager.Current.Transactions.Enqueue(transaction);
		TaxHelper.TrackTransaction(transaction);
		ReduceTransactionQueue();
		if (amount < 0f)
		{
			UiSoundHelper.Play(UiSound.MoneySpend, randomPitch: true);
		}
		GameEvent.Invoke("ba:gameevent_moneychange");
	}

	private static void ReduceTransactionQueue()
	{
		while (SaveGameManager.Current.Transactions.Count > 1000 && SaveGameManager.Current.Transactions.First().timestamp.Day < SaveGameManager.Current.Day - 6)
		{
			SaveGameManager.Current.Transactions.Dequeue();
		}
	}

	private static void RecordMidnightBankBalance()
	{
		GameInstance current = SaveGameManager.Current;
		if (current.midnightBankBalances == null)
		{
			current.midnightBankBalances = new List<float>();
		}
		while (SaveGameManager.Current.midnightBankBalances.Count >= 7)
		{
			SaveGameManager.Current.midnightBankBalances.RemoveAt(0);
		}
		SaveGameManager.Current.midnightBankBalances.Add(SaveGameManager.Current.Money);
	}

	public static void SendTextMessage(Contact contact, string messageKey, Dictionary<string, string> messageData = null, TextMessage.ContextAction contextAction = null, AdditionalMessageData additionalMessageData = null, bool notify = true)
	{
		TextMessage textMessage = new TextMessage
		{
			messageKey = messageKey,
			messageData = messageData,
			additionalData = additionalMessageData,
			timestamp = TimeHelper.Now(),
			read = false,
			contextAction = contextAction,
			isNewInteraction = true
		};
		contact.SendMessage(textMessage, notify);
		InstanceBehavior<UIs>.Instance?.smartphoneUI.UpdateBadgeCount(AppName.Contacts);
	}

	[ConsoleMethod("CustomSecondsBetweenAutosave", "Set the seconds between autosave (use 0 to reset)", new string[] { })]
	public static void Command_SetSecondsBetweenAutosave(int seconds)
	{
		InstanceBehavior<GameManager>.Instance._customSecondsBetweenAutoSaves = seconds;
		InstanceBehavior<GameManager>.Instance.ResetNextAutoSave();
	}

	[ConsoleMethod("addEnergy", "Add Energy to Player", new string[] { })]
	public static void Command_GenerateEnergy(float amount)
	{
		EnergyHelper.GenerateEnergy(amount);
	}

	[ConsoleMethod("setEnergy", "Sets Players Energy", new string[] { })]
	public static void Command_SetEnergy(float amount)
	{
		SaveGameManager.Current.Energy = amount;
	}

	[ConsoleMethod("tpa", "Teleports the Player to the passed Address", new string[] { }, AutoCompleteMap = new string[] { "streetName=StreetNames" })]
	public static void Command_TeleportPlayerToAddress(int houseNumber, string streetName)
	{
		if (IsPlayerWalking)
		{
			SetPlayerPositionBasedOnAddress(new Address(streetName, houseNumber));
		}
	}

	[ConsoleMethod("tpb", "Teleports the Player to the passed BusinessType", new string[] { }, AutoCompleteMap = new string[] { "businessType=BusinessTypes" })]
	public static void Command_TeleportPlayerToBusiness(string businessType)
	{
		Command_TeleportPlayerToBusiness(businessType, 0);
	}

	[ConsoleMethod("tpb", "Teleports the Player to the passed BusinessType", new string[] { }, AutoCompleteMap = new string[] { "businessType=BusinessTypes" })]
	public static void Command_TeleportPlayerToBusiness(string businessType, int index)
	{
		if (IsPlayerWalking)
		{
			List<BuildingRegistration> list = (from b in SaveGameManager.Current.BuildingRegistrations
				where b.businessTypeName == businessType
				orderby b.RentedByPlayer descending
				select b).ToList();
			BuildingRegistration buildingRegistration = ((list.Count > index) ? list[index] : ((list.Count > 0) ? list.Where((BuildingRegistration b) => !b.RentedByPlayer).GetRandom() : null));
			if (buildingRegistration != null)
			{
				SetPlayerPositionBasedOnAddress(buildingRegistration.Address);
			}
			else
			{
				Debug.LogError("Could not find Business " + businessType);
			}
		}
	}

	[ConsoleMethod("tppv", "Teleports the Player to a player owned building with the passed building type, size, and version", new string[] { }, AutoCompleteMap = new string[] { "buildingSize=BuildingSizes" })]
	public static void Command_TeleportPlayerToPlayerBusiness(string buildingSize, int version)
	{
		if (IsPlayerWalking)
		{
			List<BuildingRegistration> list = (from b in SaveGameManager.Current.BuildingRegistrations
				where b.BuildingCached.BuildingSize == buildingSize && b.BuildingCached.BuildingVersion == version
				where b.RentedByPlayer
				select b).ToList();
			BuildingRegistration buildingRegistration = ((list.Count > 0) ? list.GetRandom() : null);
			if (buildingRegistration != null)
			{
				SetPlayerPositionBasedOnAddress(buildingRegistration.Address);
			}
			else
			{
				Debug.LogError($"Could not find Building with building size {buildingSize} and version {version}");
			}
		}
	}

	[ConsoleMethod("tpv", "Teleports the Player to a non-rented building with the passed building type, size, and version", new string[] { }, AutoCompleteMap = new string[] { "buildingSize=BuildingSizes" })]
	public static void Command_TeleportPlayerToVersion(string buildingSize, int version)
	{
		if (IsPlayerWalking)
		{
			List<BuildingRegistration> list = (from b in SaveGameManager.Current.BuildingRegistrations
				where b.BuildingCached.BuildingSize == buildingSize && b.BuildingCached.BuildingVersion == version
				where !b.RentedByPlayer
				select b).ToList();
			BuildingRegistration buildingRegistration = ((list.Count > 0) ? list.GetRandom() : null);
			if (buildingRegistration != null)
			{
				SetPlayerPositionBasedOnAddress(buildingRegistration.Address);
			}
			else
			{
				Debug.LogError($"Could not find Building with building size {buildingSize} and version {version}");
			}
		}
	}

	[ConsoleMethod("tpd", "Teleports the Player the his current Set Destination", new string[] { })]
	public static void Command_TeleportPlayerToDestination()
	{
		if (IsPlayerWalking && SaveGameManager.Current.customDestination != null)
		{
			SetPlayerPositionBasedOnAddress(SaveGameManager.Current.customDestination);
		}
	}

	[ConsoleMethod("tpdi", "Teleports the Player the his current Set Destination inside", new string[] { })]
	public static void Command_TeleportPlayerToDestinationInside()
	{
		if (IsPlayerWalking && !(SaveGameManager.Current.customDestination == null))
		{
			Building building = BuildingHelper.GetBuilding(SaveGameManager.Current.customDestination);
			if (building != null)
			{
				InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building);
			}
		}
	}

	[ConsoleMethod("tpt", "Teleports the Player the his current Quest Target", new string[] { })]
	public static void Command_TeleportPlayerToQuestTarget()
	{
		if (IsPlayerWalking && !InstanceBehavior<GuidersManager>.Instance.mainQuestGuider.CurrentAddress.IsUndefined())
		{
			SetPlayerPositionBasedOnAddress(InstanceBehavior<GuidersManager>.Instance.mainQuestGuider.CurrentAddress);
		}
	}

	[ConsoleMethod("tpg", "Teleports the player to the guider", new string[] { })]
	public static void Command_TeleportPlayerToGuider(DirectionGuiderType guiderType)
	{
		if (IsPlayerWalking)
		{
			Address guiderAddress = GuidersManager.GetGuiderAddress(guiderType);
			if (guiderAddress.IsUndefined())
			{
				Debug.LogWarning($"No guider active for type {guiderType}");
			}
			else
			{
				SetPlayerPositionBasedOnAddress(guiderAddress);
			}
		}
	}

	[ConsoleMethod("tpw", "Teleports the Player to a Waypoint", new string[] { })]
	public static void Command_TeleportPlayerToWaypoint(string waypointName)
	{
		waypointName = waypointName.ToLowerInvariant();
		string text = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList()
			.FirstOrDefault((string x) => x.StartsWith(waypointName + "|"));
		if (text != null)
		{
			float[] array = text.Split('|')[1].Split(',').Select(float.Parse).ToArray();
			InstanceBehavior<GameManager>.Instance.StartCoroutine(SetPlayerPosition(new Vector3(array[0], array[1], array[2])));
		}
		else
		{
			Debug.LogWarning("Waypoint " + waypointName + " not found");
		}
	}

	[ConsoleMethod("ClearWaypoints", "Clears all Waypoints", new string[] { })]
	public static void Command_ClearWaypoints()
	{
		UnityEngine.PlayerPrefs.DeleteKey("tpwWaypoints");
		Debug.Log("Cleared all Waypoints");
	}

	[ConsoleMethod("AddWaypoint", "Adds a Waypoint for teleporting with the tpw command", new string[] { })]
	public static void Command_AddWaypoint(string waypointName)
	{
		if (BuildingManager.IsInsideBuilding)
		{
			Debug.LogWarning("Cannot Add Waypoint while inside a Building");
			return;
		}
		waypointName = waypointName.ToLowerInvariant();
		Vector3 position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
		List<string> list = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList();
		int num = list.FindIndex((string x) => x.StartsWith(waypointName + "|"));
		string text = $"{waypointName}|{position.x},{position.y},{position.z}";
		if (num >= 0)
		{
			list[num] = text;
		}
		else
		{
			list.Add(text);
		}
		Debug.Log($"Added Waypoint {waypointName} at {position}");
		UnityEngine.PlayerPrefs.SetString("tpwWaypoints", string.Join(";", list));
	}

	[ConsoleMethod("RemoveWaypoint", "Removes a Waypoint for teleporting with the tpw command", new string[] { })]
	public static void Command_RemoveWaypoint(string waypointName)
	{
		waypointName = waypointName.ToLowerInvariant();
		List<string> list = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList();
		int num = list.FindIndex((string x) => x.StartsWith(waypointName + "|"));
		if (num >= 0)
		{
			list.RemoveAt(num);
			UnityEngine.PlayerPrefs.SetString("tpwWaypoints", string.Join(";", list));
			Debug.Log("Removed Waypoint " + waypointName);
		}
		else
		{
			Debug.LogWarning("Waypoint " + waypointName + " not found");
		}
	}

	[ConsoleMethod("ListWaypoints", "Lists all Waypoints", new string[] { })]
	public static void Command_ListWaypoints()
	{
		List<string> list = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList();
		for (int i = 1; i < list.Count; i++)
		{
			string[] array = list[i].Split('|');
			Debug.Log("Waypoint " + array[0] + " at " + array[1] + "\n");
		}
	}

	[ConsoleMethod("CopyWaypoints", "Copies all Waypoints to the Clipboard", new string[] { })]
	public static void Command_CopyWaypoints()
	{
		GUIUtility.systemCopyBuffer = UnityEngine.PlayerPrefs.GetString("tpwWaypoints");
		Debug.Log("Copied Waypoints to Clipboard");
	}

	[ConsoleMethod("PasteWaypoints", "Pastes all Waypoints from the Clipboard", new string[] { })]
	public static void Command_PasteWaypoints()
	{
		List<string> list = GUIUtility.systemCopyBuffer.Split(';').ToList();
		List<string> list2 = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList();
		foreach (string item in list)
		{
			string[] array = item.Split('|');
			if (array.Length == 2 && array[1].Split(',').Length == 3)
			{
				string waypointName = array[0].ToLowerInvariant();
				if (!list2.Any((string w) => w.StartsWith(waypointName + "|")))
				{
					list2.Add(item);
				}
				else
				{
					Debug.LogWarning("Waypoint " + waypointName + " already exists, skipping.");
				}
			}
			else
			{
				Debug.LogWarning("Invalid waypoint format: " + item);
			}
		}
		if (list2.Count == 0)
		{
			Debug.LogWarning("No valid waypoints found in Clipboard");
			return;
		}
		UnityEngine.PlayerPrefs.SetString("tpwWaypoints", string.Join(";", list2));
		Debug.Log("Appended valid waypoints from Clipboard");
	}

	[ConsoleMethod("ToggleTraffic", "Toggles Traffic spawning", new string[] { })]
	public static void Command_ToggleTraffic()
	{
		InstanceBehavior<GameManager>.Instance.spawnTraffic = !InstanceBehavior<GameManager>.Instance.spawnTraffic;
		if (InstanceBehavior<GameManager>.Instance.spawnTraffic)
		{
			Manager.SetTrafficDensity(40);
		}
		else
		{
			Manager.SetTrafficDensity(0);
			Manager.ClearTraffic();
		}
		Debug.Log("Traffic spawning is now " + (InstanceBehavior<GameManager>.Instance.spawnTraffic ? "active" : "inactive"));
	}

	[ConsoleMethod("ToggleFpsTestMode", "Toggles Fps test mode", new string[] { })]
	public static void Command_ToggleFpsTestMode()
	{
		Command_ToggleTraffic();
		PedestrianSpawner.Command_ToggleSpawning();
		ParkingLaneGenerator.Command_ToggleSpawning();
	}

	[ConsoleMethod("ToggleShadowCasterDebug", "Toggles Shadow Caster Debug overlay", new string[] { })]
	public static void Command_ToggleShadowCasterDebug()
	{
		if (!BuildingManager.IsInsideBuilding)
		{
			Debug.LogWarning("Cannot Toggle Shadow Caster Debug outside a Building");
			return;
		}
		if (_shadowCasterDebugPass == null)
		{
			_shadowCasterDebugPass = InstanceBehavior<GameManager>.Instance.passVolume.customPasses.FirstOrDefault((CustomPass x) => x.name == "ShadowCasterDebug");
		}
		if (_shadowCasterDebugPass == null)
		{
			Debug.LogWarning("Shadow Caster Debug pass not found");
			return;
		}
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			allItemController.UpdateDebugShadowCasterOverlay();
		}
		_shadowCasterDebugPass.enabled = !_shadowCasterDebugPass.enabled;
		isShadowCasterDebugViewEnabled = _shadowCasterDebugPass.enabled;
	}

	[ConsoleMethod("ToggleShowOnlyNonStaticShadowCasters", "Hides all static and non shadow casters", new string[] { })]
	public static void Command_ToggleShowOnlyNonStaticShadowCasters()
	{
		hideStaticShadowCastersMeshRenderers = !hideStaticShadowCastersMeshRenderers;
		MeshRenderer[] array = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in array)
		{
			if (meshRenderer.gameObject.activeInHierarchy && (meshRenderer.staticShadowCaster || meshRenderer.shadowCastingMode == ShadowCastingMode.Off) && !meshRenderer.GetComponent<NavMeshElement>())
			{
				meshRenderer.enabled = !hideStaticShadowCastersMeshRenderers;
			}
		}
	}

	[ConsoleMethod("SetBehaviorManagerUpdateTime", "Set it to 0 to make it run every frame", new string[] { })]
	public static void Command_SetBehaviorManagerUpdateTime(float time)
	{
		BehaviorManager.instance.UpdateInterval = ((time > 0f) ? UpdateIntervalType.SpecifySeconds : UpdateIntervalType.EveryFrame);
		BehaviorManager.instance.UpdateIntervalSeconds = time;
	}

	[ConsoleMethod("LogTextureMemoryUsed", "Logs information about the memory used by textures", new string[] { })]
	public static void Command_LogTextureMemoryUsed()
	{
		Debug.Log("Texture memory:\n" + $"Current: {(float)Texture.currentTextureMemory / 1048576f:F1} MB\n" + $"Desired: {(float)Texture.desiredTextureMemory / 1048576f:F1} MB\n" + $"Target: {(float)Texture.targetTextureMemory / 1048576f:F1} MB\n" + $"Total full-res: {(float)Texture.totalTextureMemory / 1048576f:F1} MB\n" + $"Non-streaming: {(float)Texture.nonStreamingTextureMemory / 1048576f:F1} MB\n" + $"Streaming textures: {Texture.streamingTextureCount}");
	}

	public static void SetPlayerPositionBasedOnAddress(Address address)
	{
		Vector3 playerPositionBasedOnAddress = GetPlayerPositionBasedOnAddress(address);
		if (!(playerPositionBasedOnAddress == Vector3.positiveInfinity))
		{
			InstanceBehavior<GameManager>.Instance.StartCoroutine(SetPlayerPosition(playerPositionBasedOnAddress));
		}
	}

	public static Vector3 GetPlayerPositionBasedOnAddress(Address address)
	{
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			Building building = cityBuildingController.building;
			if (!(building == null) && !(building.StreetName != address.streetName) && building.StreetNumber == address.streetNumber)
			{
				if ((bool)cityBuildingController.entranceDoors[0].doorTransform)
				{
					return cityBuildingController.entranceDoors[0].doorTransform.position;
				}
				if (cityBuildingController.driveInEntrances.Length != 0)
				{
					return cityBuildingController.driveInEntrances[0].transform.position;
				}
			}
		}
		Debug.Log($"Count not find Building {address.streetName} {address.streetNumber}");
		return Vector3.positiveInfinity;
	}

	public static IEnumerator SetPlayerPosition(Vector3 position)
	{
		InstanceBehavior<CityManager>.Instance.cityMap.Close();
		if (BuildingManager.IsInsideBuilding)
		{
			yield return InstanceBehavior<BuildingManager>.Instance.ExitFromBuildingCoroutine(0);
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			yield return UndergroundParkingManager.ExitParkingCoroutine();
		}
		InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(position);
	}

	public List<VehicleController> SpawnPlayerVehicles(Address address)
	{
		Physics.SyncTransforms();
		List<VehicleController> list = new List<VehicleController>();
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances.Where((VehicleInstance x) => x.Address == address).ToList())
		{
			try
			{
				if (VehicleHelper.AllPlayerVehicles.FirstOrDefault((VehicleController x) => x.vehicleInstance.id == vehicleInstance.id) != null)
				{
					Debug.LogError("ERROR: Vehicle Duplication Happened!!!!");
					if (DebugLogManager.Instance.toggleWithKey)
					{
						Notifications.ShowError("VEHICLE DUPLICATION BUG HAPPENING!!!");
					}
					continue;
				}
				string idWithoutType = vehicleInstance.vehicleTypeName.GetIdWithoutType();
				GameObject gameObject = PrefabHelper.CreatePrefab("Vehicles/PlayerVehicles/" + idWithoutType, itemsContainer);
				gameObject.transform.position = vehicleInstance.position;
				gameObject.transform.rotation = vehicleInstance.rotation;
				if (!vehicleInstance.VehicleType.spawnInPlayerObject && vehicleInstance.VehicleType.HasTag(TagRef.Vehicletag.isscooter) && vehicleInstance.Address.IsUndefined() && gameObject.transform.position.y < -0.25f)
				{
					gameObject.transform.position = new Vector3(gameObject.transform.position.x, 0f, gameObject.transform.position.z);
				}
				VehicleHelper.DestroyBlockingVehicles(gameObject, vehicleInstance.VehicleType);
				VehicleController component = gameObject.GetComponent<VehicleController>();
				component.UpdateNavMeshTargets();
				component.SetVehicleInstance(vehicleInstance);
				if (component is ScooterController || vehicleInstance.VehicleType.spawnInPlayerObject)
				{
					continue;
				}
				PointOfInterest pointOfInterest = InstanceBehavior<CityManager>.Instance?.cityMap?.AddPoi(gameObject.transform, InstanceBehavior<GlobalReferences>.Instance.vehiclePOIIcon, InstanceBehavior<GlobalReferences>.Instance.vehiclePOIBackgroundColor);
				if ((bool)pointOfInterest)
				{
					pointOfInterest.SetPermanent();
					component.poi = pointOfInterest;
					if (vehicleInstance.parkingState == ParkingState.Illegal)
					{
						pointOfInterest.SetBackground(InstanceBehavior<GlobalReferences>.Instance.vehicleIllegalParkingPOIBackgroundColor);
					}
				}
				list.Add(component);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		if (SaveGameManager.Current.ActiveVehicleId == null)
		{
			return list;
		}
		if (!string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if (currentVehicle == null)
			{
				Debug.LogError("Vehicle instance with ID " + SaveGameManager.Current.ActiveVehicleId + " not found");
				SaveGameManager.Current.ActiveVehicleId = null;
				return list;
			}
			List<VehicleController> list2 = (from x in UnityEngine.Object.FindObjectsByType<VehicleController>(FindObjectsSortMode.None)
				where x.vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId
				select x).ToList();
			if (list2.Count == 0)
			{
				return list;
			}
			VehicleController currentVehicleController = list2.First();
			list.Add(currentVehicleController);
			if (currentVehicle.Address != address)
			{
				return list;
			}
			int count = list2.Count;
			if (count <= 1)
			{
				if (count == 1)
				{
					coroutineManager.ExecuteAfterOneFrame(delegate
					{
						currentVehicleController.EnterVehicle();
					});
				}
			}
			else
			{
				Debug.LogError("SpawnPlayerVehicles EnterVehicle failed: multiple matching player vehicles with ID " + SaveGameManager.Current.ActiveVehicleId);
			}
		}
		return list;
	}

	public IEnumerator HospitalRespawn()
	{
		GlobalEvents.onHospitalRespawnStarts?.Invoke();
		if (PlayerHelper.IsHoldingAMop)
		{
			PlayerHelper.ItemInstanceInHands = null;
		}
		else if (PlacementSystem.IsInPlacementMode)
		{
			PlacementHelper.CancelPlacementMode();
		}
		InstanceBehavior<UIs>.Instance.previewTerminalUI.HideTerminal();
		playerController.ResetNavigation();
		playerController.SetNavigationBlocker(NavigationBlocker.HospitalSequence);
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			playerController.Character.SetHandContent(null);
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.Toggle(isEnabled: false);
			for (int num = PlayerHelper.ItemInstanceInHands.cargoInstances.Count - 1; num >= 0; num--)
			{
				CargoInstance cargoInstance = PlayerHelper.ItemInstanceInHands.cargoInstances[num];
				if (!cargoInstance.paid)
				{
					PlayerHelper.ItemInstanceInHands.RemoveFromCargo(cargoInstance);
				}
			}
		}
		yield return playerController.Character.animator.RunAnimation(AnimationType.Faint);
		yield return UiFader.Fade();
		playerController.Character.ResetAnimator();
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.Hospital);
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			yield return InstanceBehavior<UIs>.Instance.timeMachine.StopTimeMachineCoroutine();
		}
		playerController.ResetNavigation();
		Timestamp timestamp = TimeHelper.Now();
		if (timestamp.Hour >= 8)
		{
			timestamp.AddHours(24);
		}
		timestamp.Hour = 8;
		timestamp.Minute = 0f;
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0, playFadeAnimation: false);
		}
		else if (UndergroundParkingManager.IsInsideParking)
		{
			UndergroundParkingManager.ExitParking(playFadeAnimation: false);
		}
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			ItemController itemController = PrefabHelper.CreatePrefabItem(PlayerHelper.ItemInstanceInHands.ItemCached.itemName);
			ItemController component = itemController.GetComponent<ItemController>();
			if ((bool)component)
			{
				component.ItemInstance = PlayerHelper.ItemInstanceInHands;
				component.TogglePhysics(physicsEnabled: false);
				playerController.Character.SetHandContent(itemController.transform);
			}
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands, spawnIntoPlayerHands: false);
		}
		TransportToHospital();
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "hospital_respawn_timemachine_info");
		SaveGameManager.Current.achievementsData.hospitalization++;
		GameEvent.Invoke(string.Empty);
		yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		SaveGameManager.Current.Energy = 100f;
		SaveGameManager.Current.Hunger = 100f;
		playerController.Character.ResetZombieState();
		yield return UiFader.UnFade();
		HappinessHelper.AddModifier("ba:happinessmodifier_went_to_hospital");
		ChangeMoney(-2000f, new TransactionInfo("ba:transaction_hospitalbill"));
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.None);
		EnergyHelper.goingToHospital = false;
		InstanceBehavior<UIs>.Instance.hospitalizationNotification.Show();
	}

	public void TransportToHospital()
	{
		Building closestMedicalCenter = GetClosestMedicalCenter();
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(closestMedicalCenter);
		indoorCamera.GetComponent<PedestrianCam>().angle = -15f;
		pedestrianCamera.GetComponent<PedestrianCam>().angle = -15f;
		playerController.ResetNavigation();
		playerController.Character.Reset();
		playerController.UnsetNavigationBlocker(NavigationBlocker.HospitalSequence);
		playerController.ResetNavigation();
	}

	private static void UpdateFaintVignette()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		bool flag = SaveGameManager.Current.Energy <= 0f;
		if (InstanceBehavior<UIs>.Instance.faintVignette.gameObject.activeSelf != flag)
		{
			InstanceBehavior<UIs>.Instance.faintVignette.gameObject.SetActive(flag);
			if (flag)
			{
				Notifications.Show(NotificationType.Warning, "notification_low_energy_while_playing");
			}
		}
	}

	private static Building GetClosestMedicalCenter()
	{
		Building building = BuildingHelper.GetBuilding(hospitalAddress);
		float num = float.PositiveInfinity;
		foreach (Building value in BuildingHelper.SpecialServiceBuildings.Values)
		{
			if (value == building)
			{
				continue;
			}
			string businessTypeName = value.SpecialService.businessTypeName;
			if (!(businessTypeName != "ba:businesstype_clinic") || !(businessTypeName != "ba:businesstype_hospital"))
			{
				float num2 = Vector3.SqrMagnitude(InstanceBehavior<CityManager>.Instance.FindCityBuildingController(value.Address).transform.position - PlayerHelper.GetPosition());
				if (!(num2 >= num))
				{
					building = value;
					num = num2;
				}
			}
		}
		return building;
	}

	public static bool IsDrivingVehicle()
	{
		if (InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			return !InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.spawnInPlayerObject;
		}
		return false;
	}

	public static bool IsAnyMiniGameActive()
	{
		if (!VideoGameSetup.IsAnyVideoGamePlaying() && !GolfPlatformController.PlayingInstance)
		{
			return TennisCourt.PlayingInstance;
		}
		return true;
	}

	public static void SetMinutesMultiplier(float normalized)
	{
		MinutesMultiplier = normalized * 1f;
	}

	[Button(null, EButtonEnableMode.Always)]
	private void SetBuildingCutout()
	{
		Shader.SetGlobalFloat(PoHeightID, Height);
		Shader.SetGlobalFloat(PoHeightOffsetID, HeightOffset);
		Shader.SetGlobalFloat(ScaleID, NoiseScale);
	}
}
