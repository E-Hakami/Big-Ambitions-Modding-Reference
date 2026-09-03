using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.PlacementSystem;
using Buildings.Indoors;
using Buildings.Indoors.InteriorDesign;
using CameraControllers;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI.Notification;
using UI.Purchase;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class InteriorDesignerUI : MonoBehaviour
{
	private const ToolName DefaultTool = ToolName.Hand;

	public static Action onOpenInteriorDesigner;

	public static Action onCloseInteriorDesigner;

	public static Action<IRevertibleAction> onManualExecuteAction;

	public static readonly UnityEvent OnEscapeClick = new UnityEvent();

	public static readonly UnityEvent OnUndoRedo = new UnityEvent();

	public static readonly UnityEvent<bool> OnInteriorDesignerToggle = new UnityEvent<bool>();

	public static bool blockInput;

	public static bool wasClickingUI;

	private static InteriorDesignerController Controller;

	[SerializeField]
	private InteriorDesignerActionPanelUI actionPanelUI;

	[SerializeField]
	private InteriorDesignerInfoPanelUI infoPanelUI;

	[SerializeField]
	private GameObject toolbar;

	[Header("Buttons")]
	[SerializeField]
	private Button undoButton;

	[SerializeField]
	private Button redoButton;

	[SerializeField]
	private Button toggleWallsButton;

	[SerializeField]
	private GameObject changeFloorButtonsParent;

	[Header("Toggle Walls Sprites")]
	[SerializeField]
	private Sprite wallsVisibleSprite;

	[SerializeField]
	private Sprite wallsHiddenSprite;

	[SerializeField]
	private Sprite wallsPartlyHiddenSprite;

	public bool saveBlueprintOnConfirm;

	public bool undoAllAfterBlueprintSave;

	private readonly Stack<IRevertibleAction> _redoStack = new Stack<IRevertibleAction>();

	private readonly Stack<IRevertibleAction> _undoStack = new Stack<IRevertibleAction>();

	private PedestrianCam _indoorCam;

	private PedestrianCam _outdoorCam;

	private PlacementCam _placementCam;

	private List<ToolButton> _toolButtons;

	private readonly List<ToolName> _disabledTools = new List<ToolName>();

	private Func<bool, IEnumerator> _alternativeCameraToggle;

	public static bool IsOpen { get; private set; }

	public bool HasChanges => _undoStack.Count > 0;

	private void OnEnable()
	{
		if (Controller == null)
		{
			Controller = UnityEngine.Object.FindObjectOfType<InteriorDesignerController>();
		}
		if ((object)_placementCam == null)
		{
			_placementCam = InstanceBehavior<GameManager>.Instance?.indoorPlacementCamera.GetComponentInParent<PlacementCam>();
		}
		if ((object)_indoorCam == null)
		{
			_indoorCam = InstanceBehavior<GameManager>.Instance?.indoorCamera.GetComponent<PedestrianCam>();
		}
		if ((object)_outdoorCam == null)
		{
			_outdoorCam = InstanceBehavior<GameManager>.Instance?.pedestrianCamera.GetComponent<PedestrianCam>();
		}
	}

	private void Awake()
	{
		_toolButtons = toolbar.GetComponentsInChildren<ToolButton>().ToList();
	}

	private void Start()
	{
		SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Combine(SaveBlueprintUI.onClosed, new Action<bool>(OnSaveBlueprintUIClosed));
		OnEscapeClick.AddListener(OnESC);
		toolbar.SetActive(value: false);
		infoPanelUI.gameObject.SetActive(value: false);
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			if (IsOpen)
			{
				Hide();
			}
		});
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Combine(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
		UpdateWallsVisibilityIcon(WallsVisibilityHelper.currentWallsVisibility);
	}

	private void Update()
	{
		if (PlayerAction.Click.Pressed() && EventSystem.current.IsPointerOverGameObject())
		{
			wasClickingUI = true;
		}
		if (PlayerAction.Click.Released())
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				wasClickingUI = false;
			});
		}
		if (IsOpen && !blockInput && !(EventSystem.current == null) && !(EventSystem.current.currentSelectedGameObject != null) && !DebugLogManager.Instance.IsLogWindowVisible)
		{
			if (InteriorDesignerController.CurrentTool == null)
			{
				HandleHotkeys();
				return;
			}
			ToolInputAction input = InteriorDesignerInput.HandleInput(Controller);
			ProcessAction(input);
		}
	}

	public void ProcessAction(ToolInputAction input)
	{
		IRevertibleAction revertibleAction = InteriorDesignerController.CurrentTool.ProcessAction(input);
		if (revertibleAction == null)
		{
			HandleHotkeys();
			return;
		}
		ExecuteRevertibleAction(revertibleAction);
		if (IInteriorDesignerTool.toolToOpenAfterUsage != ToolName.None)
		{
			ToggleTool(IInteriorDesignerTool.toolToOpenAfterUsage, IInteriorDesignerTool.toolToOpenAfterUsageArguments);
			IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.None;
			IInteriorDesignerTool.toolToOpenAfterUsageArguments = null;
		}
		onManualExecuteAction?.Invoke(revertibleAction);
		onManualExecuteAction = null;
	}

	public void ExecuteRevertibleAction(IRevertibleAction action)
	{
		if (action.Do())
		{
			_redoStack.Clear();
			_undoStack.Push(action);
			UpdateUndoRedoButtons();
		}
	}

	private void OnDestroy()
	{
		IsOpen = false;
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
		SaveBlueprintUI.onClosed = (Action<bool>)Delegate.Remove(SaveBlueprintUI.onClosed, new Action<bool>(OnSaveBlueprintUIClosed));
	}

	public void DisableTool(ToolName toolName)
	{
		if (!_disabledTools.Contains(toolName))
		{
			_disabledTools.Add(toolName);
		}
		if (IsOpen)
		{
			UpdateAvailableTools();
		}
	}

	private void HandleHotkeys()
	{
		if (Feedback.IsOpen || HelpSystem.IsVisible || HudConfirm.isOpen)
		{
			return;
		}
		if (InteriorDesignerAction.Undo.Pressed())
		{
			Undo();
			return;
		}
		if (InteriorDesignerAction.Redo.Pressed())
		{
			Redo();
			return;
		}
		if (InteriorDesignerAction.ToggleWalls.Pressed())
		{
			ToggleWalls();
			return;
		}
		if (InteriorDesignerAction.ShowUpperFloor.Pressed())
		{
			ShowUpperFloor();
			return;
		}
		if (InteriorDesignerAction.ShowLowerFloor.Pressed())
		{
			ShowLowerFloor();
			return;
		}
		CheckToolHotkey(InteriorDesignerAction.OpenHandTool, ToolName.Hand);
		CheckToolHotkey(InteriorDesignerAction.OpenEyedropperTool, ToolName.Eyedropper);
		CheckToolHotkey(InteriorDesignerAction.OpenPaletteTool, ToolName.Palette);
		CheckToolHotkey(InteriorDesignerAction.OpenFloorTool, ToolName.Floor);
		CheckToolHotkey(InteriorDesignerAction.OpenWallTool, ToolName.Wall);
		CheckToolHotkey(InteriorDesignerAction.OpenQueueTool, ToolName.Queue);
		CheckToolHotkey(InteriorDesignerAction.OpenSecurityTool, ToolName.Security);
		CheckToolHotkey(InteriorDesignerAction.OpenProducerTool, ToolName.Producer);
		CheckToolHotkey(InteriorDesignerAction.OpenTimeOfDayTool, ToolName.TimeOfDay);
		CheckToolHotkey(InteriorDesignerAction.OpenBlueprintPanel, ToolName.SaveBlueprint);
		CheckToolHotkey(InteriorDesignerAction.OpenSellTool, ToolName.Sell);
		CheckToolHotkey(InteriorDesignerAction.OpenFurnitureTool, ToolName.Furniture);
		CheckToolHotkey(InteriorDesignerAction.OpenDuplicateTool, ToolName.Duplicate);
		CheckToolHotkey(InteriorDesignerAction.OpenPackageTool, ToolName.Package);
		if (InteriorDesignerAction.Apply.Pressed())
		{
			HideWithConfirm();
		}
	}

	private void CheckToolHotkey(InteriorDesignerAction action, ToolName tool)
	{
		if (action.Pressed() && !_disabledTools.Contains(tool))
		{
			ToggleTool(tool);
		}
	}

	public void Undo()
	{
		if (_undoStack.Count >= 1)
		{
			IRevertibleAction revertibleAction = _undoStack.Pop();
			if (revertibleAction.Undo())
			{
				_redoStack.Push(revertibleAction);
				OnUndoRedo.Invoke();
			}
			UpdateUndoRedoButtons();
		}
	}

	public void Redo()
	{
		if (_redoStack.Count >= 1)
		{
			IRevertibleAction revertibleAction = _redoStack.Pop();
			if (revertibleAction.Do())
			{
				_undoStack.Push(revertibleAction);
				OnUndoRedo.Invoke();
			}
			UpdateUndoRedoButtons();
		}
	}

	private void UndoAllAndHideWithConfirm()
	{
		if (_undoStack.IsEmpty())
		{
			Hide();
			return;
		}
		if (PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			Execute();
			return;
		}
		LanguageChangeEventDataHolder bodyData = "interiordesigner_are_you_sure_discard".Localize();
		Action onConfirmAction = Execute;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction, null, "common_discard_changes");
		void Execute()
		{
			int count = _undoStack.Count;
			for (int i = 0; i < count; i++)
			{
				_undoStack.Pop().Undo();
			}
			InstanceBehavior<BuildingManager>.Instance.SerializeInteriorDesign();
			Hide();
		}
	}

	public void ToggleWalls()
	{
		WallsVisibilityHelper.ToggleWalls();
	}

	public void ShowUpperFloor()
	{
		ChangeFloor(1);
	}

	public void ShowLowerFloor()
	{
		ChangeFloor(0);
	}

	public void ChangeFloor(int heightIndex)
	{
		if (!(InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController == null))
		{
			ExecuteRevertibleAction(new ChangeFloorRevertibleAction(heightIndex));
		}
	}

	private void UpdateWallsVisibilityIcon(WallsVisibility wallsVisibility)
	{
		Image image = toggleWallsButton.image;
		image.sprite = wallsVisibility switch
		{
			WallsVisibility.AllVisible => wallsVisibleSprite, 
			WallsVisibility.AllHidden => wallsHiddenSprite, 
			_ => wallsPartlyHiddenSprite, 
		};
	}

	public void HideWithConfirm()
	{
		if (PlayerAction.PerformActionWithoutConfirm.Pressing() || (_undoStack.IsEmpty() && !saveBlueprintOnConfirm))
		{
			Hide();
			return;
		}
		if (saveBlueprintOnConfirm)
		{
			ToggleTool(ToolName.SaveBlueprint);
			return;
		}
		LanguageChangeEventDataHolder bodyData = "interiordesigner_are_you_sure_apply".Localize();
		Action onConfirmAction = Hide;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction, null, "common_apply_changes");
	}

	private void OnSaveBlueprintUIClosed(bool hasSaved)
	{
		if (hasSaved && undoAllAfterBlueprintSave)
		{
			foreach (IRevertibleAction item in _undoStack)
			{
				item.Undo();
			}
			Hide();
		}
		else if (IsOpen && InteriorDesignerController.CurrentToolName == ToolName.SaveBlueprint)
		{
			ToggleTool(ToolName.Hand);
		}
	}

	public void Show()
	{
		Toggle(show: true);
	}

	public void Hide()
	{
		Toggle(show: false);
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.None;
		IInteriorDesignerTool.toolToOpenAfterUsageArguments = null;
	}

	private void Toggle(bool show)
	{
		if (!show || CanOpen())
		{
			if (!show)
			{
				InteriorDesignerController.HandleOnClose();
			}
			StartCoroutine(ToggleCamera(show));
			InstanceBehavior<UIs>.Instance?.HideUiInteriorDesigner(show);
			toolbar.SetActive(show);
			infoPanelUI.gameObject.SetActive(show);
			if (!show)
			{
				MouseController.Reset();
				InstanceBehavior<UIs>.Instance?.gameSpeed?.DisableTimeControl(disabled: false);
				InstanceBehavior<UIs>.Instance?.gameSpeed?.Reset();
				ToggleTool(ToolName.None);
				InteriorElementTool.ClearOriginalInteriorElementsData();
				_disabledTools.Clear();
				_alternativeCameraToggle = null;
				EnableOverlays();
				ItemHelper.PauseAutoFillShelves = false;
				infoPanelUI.OnExitInteriorDesigner();
				BusinessSecurityHelper.UpdateCamerasCoverage();
				onCloseInteriorDesigner?.Invoke();
			}
			else
			{
				changeFloorButtonsParent.SetActive(InstanceBehavior<BuildingManager>.Instance?.multipleHeightsBuildingController != null);
				UpdateAvailableTools();
				InteriorDesignerController.OnOpen();
				_undoStack.Clear();
				_redoStack.Clear();
				UpdateUndoRedoButtons();
				InstanceBehavior<UIs>.Instance?.gameSpeed?.SetPause(newPause: true, showOverlay: false);
				InstanceBehavior<UIs>.Instance?.gameSpeed?.DisableTimeControl(disabled: true);
				DisableOverlays();
				ItemHelper.PauseAutoFillShelves = true;
				actionPanelUI.OnOpenInteriorDesigner();
				infoPanelUI.OnOpenInteriorDesigner();
				ToggleTool(ToolName.Hand);
				onOpenInteriorDesigner?.Invoke();
			}
			infoPanelUI.gameObject.SetActive(show);
			InstanceBehavior<BuildingManager>.Instance?.buildingRegistration.GenerateInteriorDesignerLookup();
			IsOpen = show;
			OnInteriorDesignerToggle.Invoke(show);
		}
	}

	private void EnableOverlays()
	{
		InstanceBehavior<OverlayManager>.Instance.EnableOverlays();
		InstanceBehavior<OverlayManager>.Instance.ctaDisabled = false;
	}

	private void DisableOverlays()
	{
		InstanceBehavior<OverlayManager>.Instance.DisableOverlays();
		InstanceBehavior<OverlayManager>.Instance.ctaDisabled = true;
	}

	private void UpdateUndoRedoButtons()
	{
		undoButton.interactable = _undoStack.Count > 0;
		redoButton.interactable = _redoStack.Count > 0;
	}

	private void UpdateAvailableTools()
	{
		foreach (ToolButton toolButton in _toolButtons)
		{
			toolButton.SetUp(ToggleTool, _disabledTools.Contains(toolButton.toolName));
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(toolbar.GetComponent<RectTransform>());
	}

	public void OnCloseButton()
	{
		if (InteriorDesignerController.CurrentTool != null)
		{
			if (InteriorDesignerController.CurrentToolName == ToolName.Hand)
			{
				if (InteriorDesignerController.CurrentTool.HandleEsc())
				{
					return;
				}
			}
			else if (!InteriorDesignerController.CurrentTool.HandleEsc())
			{
				ToggleTool(ToolName.Hand);
			}
		}
		UndoAllAndHideWithConfirm();
	}

	private void OnESC()
	{
		if (InteriorDesignerController.CurrentTool != null)
		{
			if (IInteriorDesignerTool.toolToOpenAfterUsage != ToolName.None)
			{
				ToggleTool(IInteriorDesignerTool.toolToOpenAfterUsage, IInteriorDesignerTool.toolToOpenAfterUsageArguments);
				IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.None;
				IInteriorDesignerTool.toolToOpenAfterUsageArguments = null;
			}
			else if (InteriorDesignerController.CurrentToolName == ToolName.Hand)
			{
				if (!InteriorDesignerController.CurrentTool.HandleEsc())
				{
					UndoAllAndHideWithConfirm();
				}
			}
			else if (!InteriorDesignerController.CurrentTool.HandleEsc())
			{
				ToggleTool(ToolName.Hand);
			}
		}
		else
		{
			UndoAllAndHideWithConfirm();
		}
	}

	public void SetAlternativeCameraToggle(Func<bool, IEnumerator> alternativeCameraToggle)
	{
		_alternativeCameraToggle = alternativeCameraToggle;
	}

	private IEnumerator ToggleCamera(bool show)
	{
		if (_alternativeCameraToggle != null)
		{
			yield return _alternativeCameraToggle(show);
		}
		else if (show)
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.InteriorDesignerUI);
			_placementCam.UpdateBounds();
			_placementCam.distance = _indoorCam.distance;
			_placementCam.transform.position = InstanceBehavior<GameManager>.Instance.playerController.transform.position;
			MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
			if (multipleHeightsBuildingController != null)
			{
				float yPositionForCamera = multipleHeightsBuildingController.GetYPositionForCamera(multipleHeightsBuildingController.currentHeightIndex);
				_placementCam.transform.SetPositionY(yPositionForCamera);
			}
			_placementCam.ForceUpdateCameraPosition();
			_placementCam.RotateCamera(_indoorCam.angle - _placementCam.currentAngle);
			CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.indoorPlacementCamera);
		}
		else
		{
			_indoorCam.angle = _placementCam.currentAngle;
			if ((bool)_outdoorCam)
			{
				_outdoorCam.angle = _indoorCam.angle;
			}
			_indoorCam.distance = _placementCam.distance;
			yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.indoorCamera);
			InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.InteriorDesignerUI);
		}
	}

	private static bool CanOpen()
	{
		if (InstanceBehavior<BuildingManager>.Instance.exitingBuilding)
		{
			return false;
		}
		if (PlayerActivityUI.IsPanelOpen || PurchaseUI.IsPanelOpen)
		{
			Notifications.Show(NotificationType.Error, "interiordesigner_cant_use_while_interacting_with_workstation", null, 4f, "cannotOpenInteriorDesigner");
			return false;
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			Notifications.ShowError("interiordesigner_cant_use_while_carrying_item", "cannotOpenInteriorDesigner");
			return false;
		}
		if (VehicleHelper.IsInsideVehicle())
		{
			Notifications.ShowError("interiordesigner_cant_use_while_driving", "cannotOpenInteriorDesigner");
			return false;
		}
		return !EnergyHelper.goingToHospital;
	}

	public void ToggleTool(ToolName tool)
	{
		ToggleTool(tool, null);
	}

	public void ToggleTool(ToolName tool, object arguments)
	{
		if (InteriorDesignerController.CurrentToolName == tool)
		{
			if (tool == ToolName.None)
			{
				return;
			}
			tool = ToolName.None;
		}
		if (tool != ToolName.None && !InteriorDesignerController.GetTool(tool).CanUseTool(tool))
		{
			return;
		}
		InteriorDesignerController.CurrentTool?.OnToolDeselected();
		foreach (ToolButton toolButton in _toolButtons)
		{
			toolButton.SetSelected(tool != ToolName.None && toolButton.toolName == tool);
		}
		InteriorDesignerController.SetTool(tool);
		InteriorDesignerController.CurrentTool?.OnToolSelected(arguments);
		actionPanelUI.Show(tool);
	}

	[ConsoleMethod("OpenInteriorDesigner", "Opens the interior designer UI if inside any building", new string[] { })]
	public static void Command_OpenInteriorDesigner()
	{
		if (BuildingManager.IsInsideBuilding)
		{
			InteriorDesignerAction.Apply.Reset();
			InstanceBehavior<BuildingManager>.Instance.SerializeInteriorDesign();
			InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Furniture);
			InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Duplicate);
			InstanceBehavior<UIs>.Instance.interiorDesignerUI.Show();
		}
		else
		{
			Debug.LogError("You need to be inside a building to use this");
		}
	}

	[ConsoleMethod("CloseInteriorDesigner", "Closes the interior designer UI", new string[] { })]
	public static void Command_CloseInteriorDesigner()
	{
		InteriorDesignerAction.Apply.Reset();
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.Hide();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onOpenInteriorDesigner = null;
		onCloseInteriorDesigner = null;
		onManualExecuteAction = null;
		OnEscapeClick.RemoveAllListeners();
		OnUndoRedo.RemoveAllListeners();
		OnInteriorDesignerToggle.RemoveAllListeners();
		blockInput = false;
		wasClickingUI = false;
	}
}
