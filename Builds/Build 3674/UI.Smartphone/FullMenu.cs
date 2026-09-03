using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using GamePrompt.Runtime.Scripts;
using Helpers;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.SmartphoneUI;
using TMPro;
using UI.Load;
using UI.Notification;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Contacts;
using UI.Smartphone.Apps.EconoView;
using UI.Smartphone.Apps.Feedback;
using UI.Smartphone.Apps.MyEmployees;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Smartphone;

public class FullMenu : MonoBehaviour
{
	public CanvasGroup canvasGroup;

	public Transform appsContainer;

	public float transitionDuration;

	public TextLocalizationComponent appNameLabel;

	public TextLocalizationComponent closeLabel;

	public Button bottomBarHelpButton;

	public Button bottomBarSubmitBugButton;

	[SerializeField]
	private SmartphoneApps apps;

	[SerializeField]
	private Transform appButtonTemplate;

	[SerializeField]
	private TextLocalizationComponent dateLabel;

	[SerializeField]
	private TextMeshProUGUI timeLabel;

	[SerializeField]
	private BasicTooltip moneyTooltip;

	[SerializeField]
	private BasicTooltip yearsTooltip;

	public BizMan bizMan;

	public EconoView econoView;

	public MyEmployees myEmployees;

	public BizManSchedule schedule;

	[FormerlySerializedAs("contacts")]
	public ContactsApp contactsApp;

	private readonly Dictionary<AppName, FullMenuAppButton> _appButtons = new Dictionary<AppName, FullMenuAppButton>();

	private Tween _blurTransition;

	private AppName _currentAppSelected;

	private CanvasGroup _inGameUiCanvasGroup;

	private bool _isPausedBeforeOpeningFullMenu;

	private bool _isTimeControlDisabledBeforeOpeningFullMenu;

	private CanvasGroup _playerHudCanvasGroup;

	private bool _shouldUnpauseOnClose;

	public static bool IsPausedBeforeOpeningFullMenu => InstanceBehavior<UIs>.Instance?.fullMenu._isPausedBeforeOpeningFullMenu ?? false;

	public static bool IsOpen { get; private set; }

	private static bool CanUpdateCityMap
	{
		get
		{
			if (CityMap.IsOpen && !BuildingPreview.isPreviewing && (bool)InstanceBehavior<UIs>.Instance.mapFilters)
			{
				return InstanceBehavior<CityManager>.Instance.cityMap;
			}
			return false;
		}
	}

	private void Start()
	{
		canvasGroup.alpha = 0f;
		canvasGroup.gameObject.SetActive(value: false);
		InstanceBehavior<UIs>.Instance.topBar.UpdateMoneyTooltip(moneyTooltip);
		_playerHudCanvasGroup = InstanceBehavior<UIs>.Instance.playerHUD.GetComponent<CanvasGroup>();
		_inGameUiCanvasGroup = InstanceBehavior<OverlayManager>.Instance?.GetComponent<CanvasGroup>();
		GlobalEvents.RegisterOnGameLoadedCallback(SetUpKeysLabels);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		appButtonTemplate.ResetTemplate();
		foreach (SmartphoneApp app in apps.appList.Where((SmartphoneApp x) => x.showInFullMenu))
		{
			Transform obj = appButtonTemplate.CreateElement();
			obj.name = app.appName.ToStringFast();
			obj.GetComponent<Button>().onClick.AddListener(delegate
			{
				ShowApp(app.appName);
			});
			FullMenuAppButton component = obj.GetComponent<FullMenuAppButton>();
			component.SetTitle(app.appName);
			component.SetIcon(app.icon);
			_appButtons.Add(app.appName, component);
		}
		bottomBarHelpButton.onClick.AddListener(delegate
		{
			InstanceBehavior<HelpSystem>.Instance.Toggle();
		});
		bottomBarSubmitBugButton.onClick.AddListener(delegate
		{
			InstanceBehavior<Feedback>.Instance.Toggle();
		});
	}

	public void Update()
	{
		if (!LoadScene.isLoading && InstanceBehavior<UIs>.Instance.topBar.UpdateMoneyValue())
		{
			InstanceBehavior<UIs>.Instance.topBar.UpdateMoneyTooltip(moneyTooltip);
		}
	}

	private void SetUpKeysLabels()
	{
		closeLabel.Suffix = PlayerAction.Cancel.AsSuffix();
	}

	public void CloseFullMenu()
	{
		if (InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.bizManSettings.HasUnsavedChanges)
		{
			if (string.IsNullOrEmpty(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.buildingRegistration.BusinessName))
			{
				Notifications.ShowError("bizman_settings_notification_name_empty");
				return;
			}
			HudConfirm.Show(null, "change_character_clothes_unsaved_changes_warning", delegate
			{
				InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
			});
		}
		else
		{
			InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
		}
	}

	public void Toggle(bool show)
	{
		if (HudConfirm.isOpen)
		{
			HudConfirm.onClose?.Invoke();
			return;
		}
		if (show)
		{
			dateLabel.Arguments = new
			{
				DayOfWeek = "common_" + TimeHelper.GetDayOfWeek(),
				CurrentNumberDay = SaveGameManager.Current.Day
			};
			timeLabel.SetCurrentFormattedTime();
			int yearsByDays = TimeHelper.GetYearsByDays(SaveGameManager.Current.Day);
			int daysAmount = SaveGameManager.Current.Day - TimeHelper.GetDaysByYears(yearsByDays);
			yearsTooltip.localizationArguments = new
			{
				yearsAmount = yearsByDays,
				daysAmount = daysAmount
			};
		}
		_playerHudCanvasGroup.alpha = ((!show) ? 1 : 0);
		if (_inGameUiCanvasGroup != null)
		{
			_inGameUiCanvasGroup.alpha = ((!show) ? 1 : 0);
		}
		InstanceBehavior<UIs>.Instance.tasksUI?.ChangeVisibility(!show);
		if (show)
		{
			canvasGroup.gameObject.SetActive(value: true);
		}
		TweenerCore<float, float, FloatOptions> tweenerCore = canvasGroup.DOFade(show ? 1 : 0, transitionDuration).SetLink(canvasGroup.gameObject).SetUpdate(isIndependentUpdate: true);
		tweenerCore.onComplete = (TweenCallback)Delegate.Combine(tweenerCore.onComplete, (TweenCallback)delegate
		{
			if (!show)
			{
				canvasGroup.gameObject.SetActive(value: false);
				if (CanUpdateCityMap)
				{
					InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
				}
			}
		});
		IsOpen = show;
		if (CanUpdateCityMap)
		{
			InstanceBehavior<UIs>.Instance.mapFilters.gameObject.SetActive(!show);
			InstanceBehavior<CityManager>.Instance.cityMap.poiTemplate.transform.parent.gameObject.SetActive(!show);
			if (show)
			{
				CityMapFilters.HideAllOutlines();
			}
			else
			{
				InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
			}
		}
		InstanceBehavior<SfxManager>.Instance?.SetSoundSnapshotCityMap(show, 0.2f);
		if (show)
		{
			if (!BuildingPreview.isPreviewing)
			{
				_isPausedBeforeOpeningFullMenu = InstanceBehavior<UIs>.Instance.gameSpeed.Paused;
				_isTimeControlDisabledBeforeOpeningFullMenu = InstanceBehavior<UIs>.Instance.gameSpeed.isTimeControlDisabled;
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			GamePromptManager.Instance.TriggerEvent("OPENED_FULL_MENU_APP", _currentAppSelected.ToStringFast());
		}
		else
		{
			if (ShouldUnpauseGame())
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
			}
			else
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true);
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(_isTimeControlDisabledBeforeOpeningFullMenu);
			GamePromptManager.Instance.TriggerEvent("CLOSED_FULL_MENU_APP", _currentAppSelected.ToStringFast());
		}
		GlobalEvents.onFullMenuToggle?.Invoke(show);
		EventSystem.current.SetSelectedGameObject(null);
	}

	private static bool ShouldUnpauseGame()
	{
		if (IsPausedBeforeOpeningFullMenu)
		{
			return false;
		}
		if ((InstanceBehavior<UIs>.Instance.playerActivityUI == null || !InstanceBehavior<UIs>.Instance.playerActivityUI.ShouldBePaused) && !CityMap.IsOpen && (InstanceBehavior<UIs>.Instance.playerHUD == null || !InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.IsDoingPurchase) && !BuildingPreview.isPreviewing)
		{
			if (!(InstanceBehavior<UIs>.Instance.playerHUD == null) && InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen)
			{
				return CasinoBoatManager.IsOnCasinoBoat;
			}
			return true;
		}
		return false;
	}

	public void ShowApp(string appName)
	{
		ShowApp(Enum.Parse<AppName>(appName));
	}

	public void ShowApp(AppName appName)
	{
		if (bizMan.business.bizManSettings.HasUnsavedChanges)
		{
			if (string.IsNullOrEmpty(InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.buildingRegistration.BusinessName))
			{
				Notifications.ShowError("bizman_settings_notification_name_empty");
				return;
			}
			HudConfirm.Show(null, "change_character_clothes_unsaved_changes_warning", delegate
			{
				SelectApp(appName);
			});
		}
		else
		{
			SelectApp(appName);
		}
	}

	private void SelectApp(AppName appName)
	{
		string text = appName.ToStringFast();
		foreach (Transform item in appsContainer.transform)
		{
			item.gameObject.SetActive(value: false);
			if (!(item.name != text))
			{
				CanvasGroup component = item.GetComponent<CanvasGroup>();
				item.gameObject.SetActive(value: true);
				if (IsOpen)
				{
					component.alpha = 0f;
					component.DOFade(1f, transitionDuration).SetLink(item.gameObject).SetUpdate(isIndependentUpdate: true);
				}
				else
				{
					component.alpha = 1f;
				}
			}
		}
		appNameLabel.Key = appName.GetLocalizeKey();
		_appButtons[_currentAppSelected].HideSelectedIcon();
		_currentAppSelected = appName;
		_appButtons[_currentAppSelected].ShowSelectedIcon();
		if (!IsOpen)
		{
			Toggle(show: true);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsOpen = false;
	}
}
