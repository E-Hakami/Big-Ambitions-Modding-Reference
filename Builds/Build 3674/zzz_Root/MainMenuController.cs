using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigAmbitions.GameAnalytics;
using BigAmbitions.InputSystem;
using BigAmbitions.SaveSystem;
using BlueprintsUI;
using DG.Tweening;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using Newtonsoft.Json;
using Scenes.MainMenu;
using Steamworks;
using TMPro;
using UI;
using UI.Components;
using UI.Elements;
using UI.Load;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;

public class MainMenuController : InstanceBehavior<MainMenuController>
{
	public static bool showBlueprintLibraryOnStart;

	private static bool dismissedExperimentalBuildWarning;

	private static readonly bool ForceWindowedScreen = Environment.GetCommandLineArgs().Contains("-windowed");

	private static readonly bool ForceCrashOnStartup = Environment.GetCommandLineArgs().Contains("-forceCrashOnStartup");

	public BlueprintsPanel blueprintsPanel;

	[SerializeField]
	private TextMeshProUGUI versioning;

	[SerializeField]
	private Options options;

	[SerializeField]
	private NewsAndUpdates newsAndUpdates;

	[SerializeField]
	private SaveGameCompatibilityUI saveGameCompatibilityUI;

	[SerializeField]
	private LoadGame loadGame;

	[SerializeField]
	private Button continueButton;

	[SerializeField]
	private ModTooltip continueModTooltip;

	[SerializeField]
	private TextLocalizationComponent continueLabel;

	[SerializeField]
	private Button loadGameButton;

	[SerializeField]
	private GameObject welcomeMessage;

	[SerializeField]
	private GameObject systemRequirements;

	[SerializeField]
	private Transform folderReadAccessError;

	[SerializeField]
	private TMP_Text folderReadAccessErrorLabel;

	[SerializeField]
	private GameObject dataTrackingPopup;

	public GameObject startView;

	[SerializeField]
	private GameObject newGameOptions;

	[SerializeField]
	private GameObject credits;

	[SerializeField]
	private string[] streamingAndRecordingPrograms;

	[SerializeField]
	private GameObject experimentalBuildWarning;

	public static GameVersion currentVersion;

	private int _currentMenuAction;

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			if (ForceCrashOnStartup)
			{
				Utils.ForceCrash(ForcedCrashCategory.FatalError);
			}
			GlobalEvents.Init();
			AddressableLoader.RegisterMainMenu();
			string text = Path.Combine(Application.persistentDataPath, "Radio");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string path = Path.Combine(text, "ReadMe.txt");
			if (!File.Exists(path))
			{
				File.WriteAllText(path, "Supported file types:\n- MP2/MP3 MPEG (.mp2/.mp3)\n- Ogg Vorbis (.ogg)\n- Microsoft WAV (.wav)");
			}
			currentVersion = GameVersion.GetCurrent();
			UnityInitialization.Initialize(currentVersion.experimental ? "experimental" : "production");
			RegisterCrashConsentUpdate();
			Time.timeScale = 1f;
			SaveGameManager.Current = null;
			ApplySaveGameFolderFix();
			if (Directory.Exists(SaveGamePathHelper.SaveGameFolderPath))
			{
				saveGameCompatibilityUI.ShowIfNeeded();
			}
			else
			{
				Directory.CreateDirectory(SaveGamePathHelper.SaveGameFolderPath);
				SaveGamePathHelper.CreateCurrentVersionSaveGameFolder();
			}
			CultureHelper.UpdateStoredCultureInfo();
			if (ForceWindowedScreen)
			{
				Screen.SetResolution(Screen.width, Screen.height, fullscreen: false);
			}
			experimentalBuildWarning.SetActive(GameVersion.GetCurrent().experimental && !dismissedExperimentalBuildWarning && !Environment.GetCommandLineArgs().Contains("-disable-experimental-build-warning"));
		}
	}

	private static void RegisterCrashConsentUpdate()
	{
		PlayerPrefs.Changed += TryUpdateCrashConsent;
	}

	private static void TryUpdateCrashConsent(PlayerPref pref)
	{
		if (pref == PlayerPref.allowTracking)
		{
			GameCrashReportHelper.ApplyCrashConsent();
		}
	}

	private static void DeregisterCrashConsentUpdate()
	{
		PlayerPrefs.Changed -= TryUpdateCrashConsent;
	}

	public void NextMainMenuAction()
	{
		_currentMenuAction++;
		switch (_currentMenuAction)
		{
		case 1:
			if (PlayerPrefSettings.ShowWelcomeScreen)
			{
				welcomeMessage.SetActive(value: true);
			}
			else if (PlayerPrefSettings.ShowDataTrackingPopup)
			{
				dataTrackingPopup.SetActive(value: true);
			}
			else
			{
				NextMainMenuAction();
			}
			break;
		case 2:
		{
			List<SystemRequirement.SystemRequirementData> requirements = SystemRequirement.GetRequirements();
			bool flag = requirements.Any((SystemRequirement.SystemRequirementData x) => x.State != SystemRequirement.Status.Ok && x.Type != SystemRequirement.SystemRequirements.FolderAccess);
			bool flag2 = requirements.Exists((SystemRequirement.SystemRequirementData x) => x.Type == SystemRequirement.SystemRequirements.FolderAccess);
			if (CrashFileChecker.AnyCrashInLast7Days())
			{
				Options.SetQualityLevelToLow();
			}
			if (flag)
			{
				systemRequirements.SetActive(value: true);
			}
			if (flag2)
			{
				ShowFolderAccessWindow();
				if (!flag)
				{
					NextMainMenuAction();
				}
			}
			if (!flag && !flag2)
			{
				NextMainMenuAction();
			}
			break;
		}
		case 3:
			PlayerPrefSettings.LastPlayedVersion = currentVersion.GetFullVersionString();
			startView.SetActive(value: true);
			break;
		}
	}

	private void ShowFolderAccessWindow()
	{
		folderReadAccessError.gameObject.SetActive(value: true);
		string folder = SystemRequirement.folderChecking.Replace("\\", "/");
		folderReadAccessErrorLabel.text = LocalizorManager.GetLocalization("systemrequirements_folderaccesserrorlabel", new { folder });
	}

	private void Start()
	{
		options.Load();
		loadGame.LoadSaveGames();
		SetUpButtons();
		UIs.SetUIZooming();
		GameAnalyticsHelper.StartCollectingData();
		GameCrashReportHelper.ApplyCrashConsent();
		versioning.text = currentVersion.GetFullVersionString();
		Debug.Log("Loaded " + currentVersion.GetFullVersionString());
		NextMainMenuAction();
		if (showBlueprintLibraryOnStart)
		{
			blueprintsPanel.Show(BlueprintCategory.MyLibrary);
			startView.SetActive(value: false);
			showBlueprintLibraryOnStart = false;
		}
	}

	private void Update()
	{
		if (PlayerAction.Click.Pressed() && UI.Elements.Dropdown.currentDropdown != null)
		{
			UI.Elements.Dropdown.currentDropdown.ClickAction();
		}
	}

	public void SetUpButtons()
	{
		continueButton.onClick.RemoveAllListeners();
		List<SaveGameManager.SaveGameStruct> list = (from x in SaveGamePathHelper.GetAllSaveGamesFromVersion()
			orderby x.lastPlayedDate descending
			select x).ToList();
		if (list.Count == 0)
		{
			continueButton.interactable = false;
			loadGameButton.interactable = false;
			continueModTooltip.Clear();
			continueLabel.Key = "menu_continue";
			continueLabel.Suffix = "";
			return;
		}
		loadGameButton.interactable = true;
		SaveGameManager.SaveGameStruct lastSaveGame = list.FirstOrDefault();
		if (lastSaveGame == null)
		{
			continueButton.interactable = false;
			continueModTooltip.Clear();
			continueLabel.Key = "menu_continue";
			continueLabel.Suffix = "";
			return;
		}
		string text = lastSaveGame.characterData?.name ?? "...";
		if (text.Length > 15)
		{
			text = text.Substring(0, 15) + "...";
		}
		continueLabel.Key = "menu_continue";
		continueLabel.Suffix = " (" + text + ")";
		continueButton.onClick.AddListener(delegate
		{
			loadGame.Load(lastSaveGame);
		});
		continueButton.interactable = true;
		continueModTooltip.Setup(lastSaveGame);
	}

	public void NewGame()
	{
		if (!newGameOptions.activeSelf)
		{
			RectTransform component = newGameOptions.GetComponent<RectTransform>();
			component.position = new Vector3(component.position.x, -5000f, 0f);
			newGameOptions.SetActive(value: true);
			component.DOLocalMoveY(0f, 0.5f).SetLink(newGameOptions).SetUpdate(isIndependentUpdate: true);
		}
	}

	public void StartNewGame(GameVariables difficulty)
	{
		loadGame.CleanupTextures();
		newsAndUpdates.CleanupTextures();
		SaveGameManager.New(difficulty);
		LoadScene.LoadIntro();
	}

	public void OpenURL(string url)
	{
		Application.OpenURL(url);
	}

	public void ShowCredits()
	{
		credits.SetActive(value: true);
		startView.SetActive(value: false);
	}

	public void ShowBlueprints()
	{
		blueprintsPanel.Show();
		startView.SetActive(value: false);
	}

	public void OpenUrlOnSteamOrBrowser(string url)
	{
		if (Singleton<SteamAPI>.Instance.steamApiEnabled)
		{
			SteamFriends.OpenWebOverlay(url);
		}
		else
		{
			OpenURL(url);
		}
	}

	public void DismissExperimentalBuildWarning()
	{
		dismissedExperimentalBuildWarning = true;
		experimentalBuildWarning.SetActive(value: false);
	}

	public void ExitGame()
	{
		Application.Quit();
		loadGame.CleanupTextures();
		newsAndUpdates.CleanupTextures();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		GameManager.IsInFocus = hasFocus;
	}

	protected override void OnDestroy()
	{
		if (base.IsMainInstance)
		{
			base.OnDestroy();
			DeregisterCrashConsentUpdate();
			loadGame.CleanupTextures();
			newsAndUpdates.CleanupTextures();
		}
	}

	public static void ApplySaveGameFolderFix()
	{
		string path = Path.Combine(Application.persistentDataPath, "SaveGames");
		List<string> list = (Directory.Exists(path) ? Directory.GetFiles(path).ToList() : new List<string>());
		string path2 = Path.Combine(Application.persistentDataPath, "alpha10");
		if (Directory.Exists(path2) && !Directory.Exists(path))
		{
			string[] directories = Directory.GetDirectories(path2);
			foreach (string path3 in directories)
			{
				list.AddRange(Directory.GetFiles(path3).ToList());
			}
		}
		foreach (string item in list)
		{
			try
			{
				if (Path.GetExtension(item) == ".json")
				{
					GameInstance gameInstance = SaveGameSerializationHelper.DeserializeJsonData(item);
					string text = Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(gameInstance.characterId ?? gameInstance.charactersData.FirstOrDefault()?.name ?? "Default"), Path.GetFileName(item));
					string text2 = item.Replace(".json", ".jpg");
					string destFileName = text.Replace(".json", ".jpg");
					if (File.Exists(text2))
					{
						File.Move(text2, destFileName);
					}
					if (File.Exists(text))
					{
						File.Delete(text);
					}
					File.Move(item, text);
				}
				else if (Path.GetExtension(item) == ".hsg")
				{
					GameInstance gameInstance2 = SaveGameSerializationHelper.DeserializeBinaryData(item);
					string text3 = Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(gameInstance2.characterId ?? gameInstance2.charactersData.FirstOrDefault()?.name ?? "Default"), Path.GetFileName(item));
					string text4 = item.Replace(".hsg", ".jpg");
					string destFileName2 = text3.Replace(".hsg", ".jpg");
					if (File.Exists(text4))
					{
						File.Move(text4, destFileName2);
					}
					if (File.Exists(text3))
					{
						File.Delete(text3);
					}
					File.Move(item, text3);
				}
			}
			catch (JsonReaderException arg)
			{
				Debug.Log($"Couldn't load {item} due to: {arg}");
			}
		}
	}
}
