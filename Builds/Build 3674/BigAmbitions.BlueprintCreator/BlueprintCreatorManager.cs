using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BAModAPI;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.ModsInternal;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SaveSystem;
using Buildings.Indoors.InteriorDesign;
using BusinessLayoutSets;
using Cinemachine;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using TMPro;
using UI.Components;
using UI.Elements;
using UI.InteriorDesigner;
using UI.Load;
using UI.Smartphone.Apps.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BigAmbitions.BlueprintCreator;

public class BlueprintCreatorManager : MonoBehaviour
{
	private static Action OnBlueprintCreatorInit;

	private static bool Initialized;

	[SerializeField]
	private BlueprintCreatorCamera cam;

	[SerializeField]
	private MouseSettings mouseSettings;

	[SerializeField]
	private List<CinemachineVirtualCameraBase> cameras;

	public static event Action OnReturnToMainMenu;

	private void Awake()
	{
		Initialized = false;
		Resources.UnloadUnusedAssets();
		TimeHelper.use12h = PlayerPrefSettings.use12h;
		UnitHelper.useImperial = PlayerPrefSettings.useImperial;
		CultureHelper.UpdateStoredCultureInfo();
		LocalizorManager.showNonCriticalWarnings = !GameManager.IsDevMode;
		DebugLogConsole.onCommandExecuted = (Action)Delegate.Combine(DebugLogConsole.onCommandExecuted, new Action(InputActionHelper.ResetAllActions));
	}

	private void Start()
	{
		LoadingAsyncTaskManager.AddTask(Init());
	}

	private void OnDestroy()
	{
		DebugLogConsole.onCommandExecuted = (Action)Delegate.Remove(DebugLogConsole.onCommandExecuted, new Action(InputActionHelper.ResetAllActions));
	}

	private void Update()
	{
		if (Initialized)
		{
			HandleInput();
			MouseController.Run();
			PlacementHelper.Run();
		}
	}

	private async Task Init()
	{
		LoadingSpinner.Show();
		InputHelper.SetupPlayerInput();
		AddressableLoader.RegisterBlueprintCreator();
		MouseController.Init(mouseSettings);
		CameraHelper.Init(cameras);
		InteriorDesignerHelper.Init(cam.timeOfDayController, cam.placementCam, blueprintCreatorMode: true);
		InteriorElementsHelper.Init();
		PlacementHelper.Init(cam.placementCam, cam.indoorCam);
		await BusinessLayoutSetHelper.Init();
		ItemHelper.Init((ItemInstance _) => InstanceBehavior<BuildingManager>.Instance.buildingRegistration, blueprintCreator: true);
		BigAmbitions.PlacementSystem.PlacementSystem.mainCamera = Camera.main;
		BuildingManager.ignoreSeasons = true;
		OnBlueprintCreatorInit?.Invoke();
		OnBlueprintCreatorInit = null;
		LoadingSpinner.Hide();
		MainMenuMusic.Stop();
		Initialized = true;
	}

	public static void RegisterOnInit(Action action)
	{
		if (action != null)
		{
			if (Initialized)
			{
				action();
			}
			else
			{
				OnBlueprintCreatorInit = (Action)Delegate.Combine(OnBlueprintCreatorInit, action);
			}
		}
	}

	private static void HandleInput()
	{
		if (PlayerAction.Cancel.Pressed())
		{
			if (!HandleEscapeClick())
			{
				OnReturnToMainMenu?.Invoke();
			}
		}
		else
		{
			if (DebugLogManager.Instance.IsLogWindowVisible)
			{
				return;
			}
			if (PlayerAction.Confirm.Pressed() && HudConfirm.isOpen)
			{
				HudConfirm.onConfirm?.Invoke();
			}
			if (PlayerAction.OpenHelp.Released() && (bool)InstanceBehavior<HelpSystem>.Instance)
			{
				if (Feedback.IsOpen)
				{
					InstanceBehavior<Feedback>.Instance?.Toggle(show: false);
				}
				InstanceBehavior<HelpSystem>.Instance.Toggle();
			}
			if (PlayerAction.OpenBugReport.Released() && (bool)InstanceBehavior<Feedback>.Instance)
			{
				if (HelpSystem.IsVisible)
				{
					InstanceBehavior<HelpSystem>.Instance?.Toggle(show: false);
				}
				InstanceBehavior<Feedback>.Instance.Toggle();
			}
		}
	}

	private static bool HandleEscapeClick()
	{
		if (HasInputSelected())
		{
			GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
			if (!currentSelectedGameObject.activeInHierarchy || (bool)currentSelectedGameObject.GetComponent<Button>() || (bool)currentSelectedGameObject.GetComponent<Toggle>())
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
			else
			{
				UI.Elements.Dropdown componentInParent = currentSelectedGameObject.GetComponentInParent<UI.Elements.Dropdown>();
				if ((bool)componentInParent)
				{
					componentInParent.HideOptions();
					EventSystem.current.SetSelectedGameObject(null);
					return true;
				}
				if ((bool)currentSelectedGameObject.GetComponent<TMP_InputField>())
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
			}
		}
		if (DebugLogManager.Instance.IsLogWindowVisible)
		{
			DebugLogManager.Instance.HideLogWindow();
			return true;
		}
		if (InstanceBehavior<HelpSystem>.Instance != null && InstanceBehavior<HelpSystem>.Instance.container.activeInHierarchy)
		{
			InstanceBehavior<HelpSystem>.Instance.Toggle(show: false);
			return true;
		}
		if (InstanceBehavior<Feedback>.Instance != null && Feedback.IsOpen && !ModLifecycleLoader.IsScopeLoaded(ModActivationScope.BlueprintCreator))
		{
			InstanceBehavior<Feedback>.Instance.Toggle(show: false);
			return true;
		}
		if (HudConfirm.isOpen)
		{
			HudConfirm.onClose?.Invoke();
			return true;
		}
		if (InteriorDesignerUI.IsOpen)
		{
			InteriorDesignerUI.OnEscapeClick.Invoke();
			return true;
		}
		return false;
	}

	private static bool HasInputSelected()
	{
		if (ScrollBarDraggingComponent.isScrollBarBeingDragged)
		{
			return true;
		}
		if (EventSystem.current?.currentSelectedGameObject == null || !EventSystem.current.currentSelectedGameObject.activeInHierarchy)
		{
			return false;
		}
		if (EventSystem.current.currentSelectedGameObject.layer != LayerHelper.UiLayerIndex)
		{
			return EventSystem.current.currentSelectedGameObject.transform.parent.gameObject.layer == LayerHelper.UiLayerIndex;
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Initialized = false;
		OnBlueprintCreatorInit = null;
		OnReturnToMainMenu = null;
	}
}
