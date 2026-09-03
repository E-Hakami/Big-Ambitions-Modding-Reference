using System;
using System.Collections;
using Blueprints;
using BlueprintsUI;
using Buildings.Indoors.InteriorDesign;
using DG.Tweening;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player;
using Player.HUD.ItemInfoOverlays;
using Scenes.MainMenu;
using Steamworks;
using TMPro;
using UI.InteriorDesigner;
using UI.Load;
using UI.Notification;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.MiniMenu;

public class MiniMenu : MonoBehaviour
{
	public Image panel;

	public BlueprintsPanel blueprintsPanel;

	public UnityEvent onCloseOptionsMenu = new UnityEvent();

	[SerializeField]
	private TMP_InputField saveGameName;

	private bool _usingThisTextInput;

	public static Action<bool> OnToggled;

	public static bool IsOpen { get; private set; }

	private void Start()
	{
		IsOpen = false;
		panel.gameObject.SetActive(value: false);
		InstanceBehavior<UIs>.Instance.options?.gameObject.SetActive(value: false);
		saveGameName.text = FileSystemHelper.MakeValidFilename(SaveGameManager.Current.SaveGameName).Trim();
		if (!Singleton<SteamAPI>.Instance.steamApiEnabled || !SteamUtils.IsSteamInBigPictureMode)
		{
			return;
		}
		saveGameName.onSelect.AddListener(delegate
		{
			_usingThisTextInput = true;
			SteamUtils.ShowGamepadTextInput(GamepadTextInputMode.Normal, GamepadTextInputLineMode.SingleLine, "common_name".GetLocalization(), (saveGameName.characterLimit == 0) ? 100 : saveGameName.characterLimit, saveGameName.text);
		});
		SteamUtils.OnGamepadTextInputDismissed += delegate(bool submitted)
		{
			if (_usingThisTextInput & submitted)
			{
				saveGameName.text = SteamUtils.GetEnteredGamepadText();
			}
			_usingThisTextInput = false;
		};
	}

	public void SaveGame()
	{
		saveGameName.text = saveGameName.text.Trim();
		if (ValidateSaveGameName(saveGameName.text))
		{
			if (SaveGamePathHelper.IsSaveGameAlreadyPresent(SaveGameManager.Current.characterId, saveGameName.text))
			{
				LanguageChangeEventDataHolder bodyData = LanguageChangeEventDataHolder.Create("mini_menu_hud_confirm_save_game_exists", new
				{
					name = saveGameName.text
				});
				Action onConfirmAction = ExecuteSave;
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
			}
			else
			{
				ExecuteSave();
			}
		}
		void ExecuteSave()
		{
			if (InstanceBehavior<GameManager>.Instance.SaveGame(saveGameName.text) && panel.gameObject.activeSelf)
			{
				Toggle(show: false);
			}
		}
	}

	private bool ValidateSaveGameName(string saveName)
	{
		if (string.IsNullOrWhiteSpace(saveName))
		{
			return false;
		}
		saveName = saveName.Trim();
		if (saveName.StartsWith("."))
		{
			Notifications.ShowError("bizman_notification_name_cannot_start_dot", "bizman_notification_name_cannot_start_dot", trackOnSaveGame: false);
			return false;
		}
		if (saveName != FileSystemHelper.MakeValidFilename(saveName))
		{
			Notifications.ShowError("notification_save_game_invalid_characters", "notification_save_game_invalid_characters", trackOnSaveGame: false);
			return false;
		}
		return true;
	}

	public void SaveAndExitToDesktop()
	{
		saveGameName.text = saveGameName.text.Trim();
		if (string.IsNullOrEmpty(saveGameName.text))
		{
			saveGameName.text = SaveGameManager.Current.SaveGameName.Trim();
		}
		if (!string.IsNullOrEmpty(saveGameName.text) && ValidateSaveGameName(saveGameName.text))
		{
			StartCoroutine(SaveAndExitToDesktopCoroutine());
		}
	}

	private IEnumerator SaveAndExitToDesktopCoroutine()
	{
		if (InstanceBehavior<GameManager>.Instance.SaveGame(saveGameName.text, skipSoundAndNotification: true))
		{
			yield return new WaitUntil(() => !SaveGameManager.SavingGameInProgress);
			yield return SaveGameManager.JoinSaveGameThreadsCoroutine();
			GameManager.isCitySceneBeingUnloaded = true;
			yield return CloseGameAsync();
		}
	}

	public void OpenMainMenu()
	{
		InstanceBehavior<OverlayManager>.Instance.HideOverlays();
		Debug.Log("Returned to main menu");
		if (!Application.isEditor)
		{
			LanguageChangeEventDataHolder bodyData = "mini_menu_hud_confirm_unsaved_changes_menu".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				StartCoroutine(LoadScene.LoadMainMenuFromCity());
			});
		}
		else
		{
			StartCoroutine(LoadScene.LoadMainMenuFromCity());
		}
	}

	public void QuitToDesktop()
	{
		LanguageChangeEventDataHolder bodyData = "mini_menu_hud_confirm_unsaved_changes_desktop".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			StartCoroutine(QuitToDesktopCoroutine());
		});
	}

	private IEnumerator QuitToDesktopCoroutine()
	{
		yield return SaveGameManager.JoinSaveGameThreadsCoroutine();
		GameManager.isCitySceneBeingUnloaded = true;
		yield return CloseGameAsync();
	}

	private static IEnumerator CloseGameAsync()
	{
		LoadScene.isLoading = true;
		Image component = GameObject.Find("BlackImage").GetComponent<Image>();
		yield return component.DOFade(1f, 0.25f).SetUpdate(isIndependentUpdate: true);
		for (int i = 0; i < SceneManager.sceneCount - 1; i++)
		{
			Scene sceneAt = SceneManager.GetSceneAt(i);
			if (!(sceneAt.name == "LightsAndPostprocessing"))
			{
				yield return SceneManager.UnloadSceneAsync(sceneAt);
			}
		}
		Debug.Log("Unloading all resources...");
		Resources.UnloadUnusedAssets();
		Debug.Log("Unloading all asset bundles...");
		AssetBundle.UnloadAllAssetBundles(unloadAllObjects: true);
		Debug.Log("Quitting game...");
		Application.Quit();
	}

	public void OpenOptions()
	{
		InstanceBehavior<UIs>.Instance.options.gameObject.SetActive(value: true);
		Options.IsVisible = true;
		TemporarilyHide();
	}

	public void CloseOptions()
	{
		if (!InstanceBehavior<UIs>.Instance.options.TryCloseDifficultyCustomizeUI())
		{
			InstanceBehavior<UIs>.Instance.options.gameObject.SetActive(value: false);
			Options.IsVisible = false;
			onCloseOptionsMenu.Invoke();
			UndoTemporarilyHide();
		}
	}

	public void OpenBlueprints()
	{
		blueprintsPanel.Show();
		TemporarilyHide();
	}

	public void CloseBlueprints()
	{
		blueprintsPanel.Hide();
		UndoTemporarilyHide();
	}

	private void TemporarilyHide()
	{
		panel.gameObject.SetActive(value: false);
		IsOpen = false;
	}

	private void UndoTemporarilyHide()
	{
		panel.gameObject.SetActive(value: true);
		IsOpen = true;
	}

	public void Toggle()
	{
		Toggle(!panel.gameObject.activeSelf);
	}

	public void Toggle(bool show)
	{
		if (!show || (!InstanceBehavior<UIs>.Instance.playerActivityUI.ShouldBePaused && !InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.IsDoingPurchase && !InteriorDesignerUI.IsOpen && !FullMenu.IsOpen && !InstanceBehavior<UIs>.Instance.hospitalizationNotification.IsVisible))
		{
			SignAppearance signAppearance = InstanceBehavior<UIs>.Instance.draggableWindows.signAppearance;
			if (show && (bool)signAppearance && signAppearance.IsOpen)
			{
				signAppearance.Close();
			}
			panel.gameObject.SetActive(show);
			IsOpen = show;
			InstanceBehavior<SfxManager>.Instance.SetSoundSnapshotCityMap(show, 0.2f);
			OnToggled?.Invoke(show);
			if (show)
			{
				PlacementHelper.CancelPlacementModeIfIsActive();
				InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true);
			}
			else if (!CityMap.IsOpen)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
			}
			if (!CityMap.IsOpen)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(show);
			}
			HandleUiElements(show);
		}
	}

	private void HandleUiElements(bool hideUi)
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			if (hideUi)
			{
				CityBuildingController.StopHighlight();
				BlurEffect.Enable();
			}
			else
			{
				BlurEffect.Disable();
			}
			InstanceBehavior<UIs>.Instance.HideUiMiniMenu(hideUi);
		}
	}

	public void WishlistNow()
	{
		Singleton<SteamAPI>.Instance.OpenUrlOnSteamOrBrowser("https://store.steampowered.com/app/1331550/Big_Ambitions/");
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsOpen = false;
		OnToggled = null;
	}
}
