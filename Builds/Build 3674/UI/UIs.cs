using System;
using System.Linq;
using BigAmbitions.PlacementSystem;
using Buildings.Indoors.InteriorDesign;
using Character.Customization;
using IngameDebugConsole;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemWarningIcons;
using PlayerActivity;
using Scenes.MainMenu;
using Timemachine;
using Tutorial.SideQuests;
using UI.CustomUI;
using UI.DailySummary;
using UI.DraggableWindows;
using UI.InGameUI;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Monologues;
using UI.Notification;
using UI.Overlays;
using UI.PlayerHUD;
using UI.Purchase;
using UI.PurchaseVehicle;
using UI.Smartphone;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Tasks;
using UI.Topbar;
using UI.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class UIs : InstanceBehavior<UIs>
{
	public CanvasGroup canvasGroup;

	public UI.PlayerHUD.PlayerHUD playerHUD;

	public FullMenu fullMenu;

	public TimeMachine timeMachine;

	public UI.DraggableWindows.DraggableWindows draggableWindows;

	public UI.MiniMenu.MiniMenu miniMenuUI;

	public InteriorDesignerUI interiorDesignerUI;

	public UI.DailySummary.DailySummary dailySummary;

	public GameSpeedController gameSpeed;

	public BuildingResume buildingResume;

	public ScreenshotController screenshot;

	public TasksUI tasksUI;

	public SmartphoneUI smartphoneUI;

	public UI.Topbar.Topbar topBar;

	public BuildingPreview buildingPreview;

	public Options options;

	public Image blackOverlay;

	public TextLocalizationComponent blackOverlayLabel;

	public CasinoMessageUI casinoMessage;

	public HospitalizationNotification hospitalizationNotification;

	public ItemsList itemsList;

	public TutorialUI tutorialUI;

	public SideQuestUI sideQuestUI;

	public MonologueUI monologueUI;

	public NotificationsListUI notificationsListUI;

	public CityMapFilters mapFilters;

	public CityMapSubwayStations cityMapSubwayStations;

	public FuneralUI funeralUI;

	public ChangeCharacterClothesUI changeCharacterClothesUI;

	public ChangeCharacterHairUI changeCharacterHairUI;

	public PlasticSurgeryUI plasticSurgeryUI;

	public CustomColorPicker customColorPicker;

	public PlayerActivityUI playerActivityUI;

	public OverlayUI overlayUI;

	public PreviewTerminalUI previewTerminalUI;

	public EmployeePresetCustomizer employeePresetCustomizer;

	public RivalEmployeesUi rivalEmployeesUi;

	public TextInputUI textInputUi;

	public ScheduleEmployeeSelection scheduleEmployeeSelection;

	public FaintVignette faintVignette;

	[Header("Collapsible windows")]
	public CollapsibleWindow smartphoneCollapsibleWindow;

	public CollapsibleWindow todoTasksCollapsibleWindow;

	protected override void Awake()
	{
		base.Awake();
		if (!base.IsMainInstance)
		{
			return;
		}
		SetUIZooming();
		GlobalEvents.onScreenshotModeToggled = (Action<bool>)Delegate.Combine(GlobalEvents.onScreenshotModeToggled, (Action<bool>)delegate(bool visible)
		{
			canvasGroup.interactable = visible;
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			if (playerHUD.manageCargoUI.isPanelOpen)
			{
				playerHUD.manageCargoUI.Close();
			}
		});
		if ((bool)options)
		{
			GlobalEvents.RegisterOnGameLoadedCallback(options.Load);
		}
	}

	private void Start()
	{
		if ((bool)smartphoneCollapsibleWindow)
		{
			smartphoneCollapsibleWindow.onStateChange.AddListener(delegate(bool newState)
			{
				SaveGameManager.Current.uiVariables.smartphoneCollapsed = newState;
			});
			smartphoneCollapsibleWindow.ChangeState(SaveGameManager.Current.uiVariables.smartphoneCollapsed, instant: true);
		}
		if ((bool)todoTasksCollapsibleWindow)
		{
			todoTasksCollapsibleWindow.onStateChange.AddListener(delegate(bool newState)
			{
				SaveGameManager.Current.uiVariables.todoTasksCollapsed = newState;
			});
			todoTasksCollapsibleWindow.ChangeState(SaveGameManager.Current.uiVariables.todoTasksCollapsed, instant: true);
		}
		if ((bool)DebugLogManager.Instance)
		{
			DebugLogManager.Instance.toggleWithKey = Debug.isDebugBuild || Environment.GetCommandLineArgs().Contains("-console");
		}
	}

	public static void SetUIZooming()
	{
		float uiZooming = PlayerPrefSettings.uiZooming;
		CanvasScaler[] array = UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (CanvasScaler obj in array)
		{
			obj.scaleFactor = uiZooming;
			obj.referenceResolution = new Vector2(Options.defaultReferenceResolution.x / uiZooming, Options.defaultReferenceResolution.y / uiZooming);
		}
	}

	public void HideUI()
	{
		CloseActiveUis();
		if (CityMap.IsOpen)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
		}
		screenshot.ToggleUIVisibility();
	}

	public void CloseActiveUis()
	{
		if (PurchaseVehicleUI.runningShowcaseAnimation)
		{
			playerHUD.purchaseVehicleUI.CancelShowcaseAnimation();
		}
		if (notificationsListUI != null && notificationsListUI.isVisible)
		{
			notificationsListUI.Toggle();
		}
		if (timeMachine != null && timeMachine.isRunning)
		{
			timeMachine.CancelTimeMachine();
		}
		if (!(playerHUD == null))
		{
			if (playerHUD.manageCargoUI != null && playerHUD.manageCargoUI.isPanelOpen)
			{
				playerHUD.manageCargoUI.Close();
			}
			if (PlacementSystem.IsInPlacementMode)
			{
				PlacementHelper.CancelPlacementMode();
			}
			if (PlayerActivityUI.IsPanelOpen)
			{
				playerActivityUI.CancelActivity();
			}
			if (PurchaseUI.IsPanelOpen && playerHUD.purchaseUI.cancelButton.interactable)
			{
				playerHUD.purchaseUI.Close();
			}
			if (PurchaseVehicleUI.IsPanelOpen)
			{
				playerHUD.purchaseVehicleUI.Close();
			}
			if (playerHUD.jobOfferPanel.isPanelOpen)
			{
				playerHUD.jobOfferPanel.Cancel();
			}
		}
	}

	public void HideUiMiniMenu(bool hide)
	{
		CloseActiveUis();
		HidePoiCanvas(hide);
		playerHUD.itemPanelUI.HidePanel(hide);
		playerHUD.currentBuildingUI.gameObject.SetActive(!hide);
		playerHUD.currentJobUI.Hide(hide);
		playerHUD.dialogUI.HidePanel(hide);
		tasksUI.ChangeVisibility(!hide);
		buildingResume.gameObject.SetActive(!hide);
		topBar.gameObject.SetActive(!hide);
		monologueUI.PauseMonologue(hide, hideDialog: true);
		if (CityMap.IsOpen)
		{
			if (InstanceBehavior<CityManager>.Instance.cityMap.isSubwayMode)
			{
				cityMapSubwayStations.gameObject.SetActive(!hide);
			}
			else
			{
				mapFilters.gameObject.SetActive(!hide);
			}
		}
		else
		{
			smartphoneUI.gameObject.SetActive(!hide && !GameManager.IsAnyMiniGameActive());
		}
		InstanceBehavior<ItemWarningIconManager>.Instance?.HandleCanvasVisibility(hide);
	}

	public void HideUiInteriorDesigner(bool hide)
	{
		CloseActiveUis();
		HidePoiCanvas(hide);
		playerHUD?.itemPanelUI.HidePanel(hide);
		playerHUD?.currentJobUI.Hide(hide);
		playerHUD?.currentBuildingUI.gameObject.SetActive(!hide);
		smartphoneUI?.gameObject.SetActive(!hide);
		tasksUI?.gameObject.SetActive(!hide);
		topBar?.gameObject.SetActive(!hide);
		monologueUI?.PauseMonologue(hide, hideDialog: true);
	}

	public void HidePoiCanvas(bool hide)
	{
		screenshot?.HidePoiCanvas(hide);
	}
}
