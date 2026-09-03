using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigAmbitions.GameAnalytics;
using BigAmbitions.InputSystem;
using BigAmbitions.Mods;
using Buildings.Indoors.InteriorDesign;
using CameraControllers;
using DG.Tweening;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using NWH.VehiclePhysics2.Sound;
using Seasons;
using Settings;
using Steamworks;
using TMPro;
using UI;
using UI.Elements;
using UI.MainMenu;
using UI.Notification;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class Options : MonoBehaviour
{
	public enum Quality
	{
		Low,
		Medium,
		High
	}

	public static Action onAiStoreMusicVolumeUpdated;

	private static readonly List<string> ShadowsOptions = new List<string> { "menu_options_graphics_shadows_off", "menu_options_graphics_shadows_sun_moon", "menu_options_graphics_shadows_all" };

	[Header("Values")]
	[SerializeField]
	private float minUIZooming = 0.5f;

	[SerializeField]
	private float maxUIZooming = 2f;

	[SerializeField]
	private int minTimeBetweenAutoSaves = 1;

	[SerializeField]
	private int maxTimeBetweenAutoSaves = 60;

	[SerializeField]
	private int minAutoSavesPerGame = 1;

	[SerializeField]
	private int maxAutoSavesPerGame = 10;

	[SerializeField]
	private int minGameSpeed = 1;

	[SerializeField]
	private int maxGameSpeed = 5;

	[SerializeField]
	private float minGamma = -0.5f;

	[SerializeField]
	private float maxGamma = 0.5f;

	[SerializeField]
	private VolumeProfile volumeProfile;

	[SerializeField]
	private List<NumberFormatSetup> numberFormatSetups;

	[Header("Category list")]
	[SerializeField]
	private RectTransform splitterIndicator;

	[SerializeField]
	private float indicatorAnimationTime = 0.2f;

	[SerializeField]
	private TextLocalizationComponent closeLabel;

	[SerializeField]
	private TextLocalizationComponent resetCategoryLabel;

	[Header("Graphics")]
	[SerializeField]
	private Toggle fullscreen;

	[SerializeField]
	private UI.Elements.Dropdown resolutionDropdown;

	[SerializeField]
	private UI.Elements.Dropdown vSyncAndFPSLimitDropdown;

	[SerializeField]
	private UI.Elements.Dropdown gfxQualityDropdown;

	[SerializeField]
	private UI.Elements.Dropdown particleQualityDropdown;

	[SerializeField]
	private UI.Elements.Dropdown textureQualityDropdown;

	[SerializeField]
	private UI.Elements.Dropdown antiAliasingDropdown;

	[SerializeField]
	private UI.Elements.Dropdown shadowsDropdown;

	[SerializeField]
	private UI.Elements.Dropdown hbaoQualityDropdown;

	[SerializeField]
	private Slider gammaSlider;

	[SerializeField]
	private TMP_Text gammaPercentage;

	[SerializeField]
	private Slider uiZoomingSlider;

	[SerializeField]
	private TMP_Text uiZoomingPercentage;

	[SerializeField]
	private Toggle lowDetailCityMap;

	[SerializeField]
	private GameObject resetWindowsPositionsSection;

	[SerializeField]
	private Toggle showFps;

	[Header("Audio")]
	[SerializeField]
	private Slider globalVolume;

	[SerializeField]
	private Slider menuMusicVolume;

	[SerializeField]
	private Slider radioVolume;

	[SerializeField]
	private Slider sfxVolume;

	[SerializeField]
	private Slider aiStoreMusicVolume;

	[SerializeField]
	private AudioMixer audioMixer;

	[SerializeField]
	private AudioMixer vehicleAudioMixer;

	[SerializeField]
	private TextLocalizationComponent songsFound;

	[Header("Controls")]
	[SerializeField]
	private Toggle vehicleMouseInput;

	[SerializeField]
	private Toggle invertRotation;

	[SerializeField]
	private Toggle runByDefaultIndoors;

	[SerializeField]
	private Transform bindings;

	[SerializeField]
	private Toggle steeringAssist;

	[Header("Others")]
	[SerializeField]
	private UI.Elements.Dropdown languageDropdown;

	[SerializeField]
	private GameObject difficultyContainer;

	[SerializeField]
	private UI.Elements.Dropdown difficultyDropdown;

	[SerializeField]
	private GameObject difficultyCustomizeButton;

	[SerializeField]
	private GameObject difficultyCustomizeUI;

	[SerializeField]
	private CustomGamePanel difficultyCustomizePanel;

	[SerializeField]
	private UI.Elements.Dropdown numberFormatDropdown;

	[SerializeField]
	private Toggle timeFormatToggle;

	[SerializeField]
	private Toggle unitsToggle;

	[SerializeField]
	private Toggle seasonalDecorationsToggle;

	[SerializeField]
	private Toggle controlsHintsToggle;

	[SerializeField]
	private Slider timeBetweenAutoSavesSlider;

	[SerializeField]
	private TextLocalizationComponent timeBetweenAutoSavesAmount;

	[SerializeField]
	private Slider maxAutoSavesPerGameSlider;

	[SerializeField]
	private TextLocalizationComponent maxAutoSavesPerGameAmount;

	[SerializeField]
	private GameObject unstuckPanel;

	[SerializeField]
	private Button unstuckButton;

	[SerializeField]
	private Slider gameSpeedSlider;

	[SerializeField]
	private TMP_Text gameSpeedMultiplier;

	[SerializeField]
	private Toggle allowTrackingToggle;

	[SerializeField]
	private Button requestDataDeletionButton;

	[Header("Mods")]
	[SerializeField]
	private GameObject modsTabButton;

	private static AsyncOperationHandle<IList<QualitySettingsData>> QualitySettingsHandle;

	private static List<QualitySettingsData> QualitySettingsData = new List<QualitySettingsData>();

	private string[] _availableLanguages;

	private string _selectedCategory;

	private RectTransform _selectedCategoryButton;

	private List<(int width, int height)> _resolutions;

	private CanvasScaler[] _canvasScalers;

	private LiftGammaGain _liftGammaGain;

	private int _lastScreenWidth;

	private int _lastScreenHeight;

	private const float ValidationDebounceInSeconds = 0.5f;

	private Coroutine _validateSongsCoroutine;

	private readonly List<string> _unsupportedSongFiles = new List<string>();

	public static Vector2 defaultReferenceResolution = new Vector2(3840f, 2160f);

	public static float gameSpeedSliderMultiplier = 10f;

	private const int MenuAndCharacterCreatorMaxFrameRate = 144;

	public static bool IsVisible { get; set; }

	private Dictionary<string, Action> ResetActions => new Dictionary<string, Action>
	{
		{ "Audio", ResetAudioSettingsToDefault },
		{ "Controls", ResetControlsSettingsToDefaults },
		{ "Graphics", ResetGraphicsSettingsToDefaults },
		{ "Others", ResetOthersSettingsToDefault },
		{ "Mods", ResetModSettingsToDefaults }
	};

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsVisible = false;
		QualitySettingsHandle = default(AsyncOperationHandle<IList<QualitySettingsData>>);
		QualitySettingsData = new List<QualitySettingsData>();
	}

	private void Awake()
	{
		SetQualitySettingsDataIfNeeded();
		_liftGammaGain = volumeProfile?.components.FirstOrDefault((VolumeComponent x) => x is LiftGammaGain) as LiftGammaGain;
		foreach (Transform category in base.transform.Find("Menu"))
		{
			category.GetComponent<Button>().onClick.AddListener(delegate
			{
				if (!(_selectedCategory == category.name))
				{
					_selectedCategoryButton = category.GetComponent<RectTransform>();
					SelectCategory(category.name, _selectedCategoryButton);
					_selectedCategory = category.name;
				}
			});
		}
		OptionsService.OnChanged += RefreshModsTabButton;
		RefreshModsTabButton();
		_canvasScalers = UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		SetUpKeysLabels();
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		_lastScreenWidth = Screen.width;
		_lastScreenHeight = Screen.height;
	}

	private static void SetQualitySettingsDataIfNeeded()
	{
		if (QualitySettingsHandle.IsValid())
		{
			return;
		}
		QualitySettingsHandle = Addressables.LoadAssetsAsync<QualitySettingsData>("QualitySettingsData", null);
		QualitySettingsData = QualitySettingsHandle.WaitForCompletion().OrderBy(delegate(QualitySettingsData data)
		{
			int num = Array.FindIndex(QualitySettings.names, (string qName) => data.name.Contains(qName));
			return (num < 0) ? int.MaxValue : num;
		}).ToList();
	}

	private void SetUpKeysLabels()
	{
		closeLabel.Suffix = PlayerAction.Cancel.AsSuffix();
	}

	private void UpdateResetButtonLabels()
	{
		resetCategoryLabel.Key = "menu_options_reset_category_to_defaults";
		resetCategoryLabel.Arguments = new
		{
			selectedCategory = "menu_options_" + _selectedCategory
		};
	}

	private void Start()
	{
		_resolutions = (from x in Screen.resolutions.Select((Resolution x) => (width: x.width, height: x.height)).Distinct()
			where x.width >= 1280 && x.height >= 720
			select x).ToList();
		SetUpFullScreenSetting();
		SetUpResolutionSetting();
		SetUpvSyncAndFPSSetting();
		SetUpShowFps();
		SetUpParticleQualitySetting();
		SetUpTextureQualitySetting();
		SetUpGfxQualitySetting();
		SetUpAntiAliasingSetting();
		SetUpHbaoQualitySetting();
		SetUpShadowsSetting();
		SetUpGammaSlider();
		SetUpLowDetailMap();
		SetUpUIZoomingSetting();
		SetUpVolumeSetting();
		SetSongsFound();
		SetUpVehicleMouseInputSetting();
		SetUpInvertRotationSetting();
		SetUpRunByDefaultIndoorsSetting();
		SetUpSteeringAssistSetting();
		SetUpLanguageSetting();
		SetUpDifficultySetting();
		SetUpFormats();
		SetUpSeasonalDecorationsSetting();
		SetUpControlsHintsSetting();
		SetResetWindowsSetting();
		SetUpTimeBetweenAutoSavesSetting();
		SetUpMaxAutoSavesPerGameSetting();
		SetUpUnstuckButton();
		SetUpGameSpeedSetting();
		SetUpTrackingSetting();
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
		if (string.IsNullOrEmpty(_selectedCategory))
		{
			StartCoroutine(SelectInitialCategoryDelayed());
		}
	}

	private void OnLanguageChanged()
	{
		if (base.isActiveAndEnabled)
		{
			StartCoroutine(DeferUpdateSelectedCategory());
			SetUpvSyncAndFPSSetting();
			SetUpAntiAliasingSetting();
			SetUpHbaoQualitySetting();
			SetUpShadowsSetting();
			SetUpParticleQualitySetting();
			SetUpTextureQualitySetting();
			SetUpGfxQualitySetting();
		}
	}

	private IEnumerator SelectInitialCategoryDelayed()
	{
		yield return new WaitForEndOfFrame();
		base.transform.Find("Menu").GetChild(0)?.GetComponent<Button>().onClick.Invoke();
	}

	private void Update()
	{
		if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
		{
			_lastScreenWidth = Screen.width;
			_lastScreenHeight = Screen.height;
			StartCoroutine(DeferUpdateSelectedCategory());
		}
		if (PlayerAction.Cancel.Pressed() && GameObject.Find("UI")?.transform.Find("StartView") != null)
		{
			CloseOptions();
		}
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
		{
			ResetAllCategoriesToDefaults();
		}
	}

	public void CloseOptions()
	{
		UnityEngine.PlayerPrefs.Save();
		base.gameObject.SetActive(value: false);
		IsVisible = false;
		GameObject.Find("UI").transform.Find("StartView")?.gameObject.SetActive(value: true);
	}

	private void OnEnable()
	{
		unstuckPanel.SetActive(InstanceBehavior<GameManager>.Instance != null);
		requestDataDeletionButton.interactable = true;
		steeringAssist.SetIsOnWithoutNotify(PlayerPrefSettings.SteeringAssist);
		SetSongsFound();
		ValidateSongs();
	}

	public void Load()
	{
		float volume = GetVolume(PlayerPrefSettings.GlobalVolume);
		audioMixer.SetFloat("MasterVolume", volume);
		vehicleAudioMixer.SetFloat("attenuation", volume);
		SoundManager.SetOriginalAttenuation(volume);
		audioMixer.SetFloat("MenuMusicVolume", GetVolume(PlayerPrefSettings.MenuMusicVolume));
		audioMixer.SetFloat("RadioVolume", GetVolume(PlayerPrefSettings.RadioVolume));
		float volume2 = GetVolume(PlayerPrefSettings.SfxVolume);
		audioMixer.SetFloat("FXVolume", volume2);
		vehicleAudioMixer.SetFloat("fx", volume2);
		audioMixer.SetFloat("OutsideBuildingMusicVolume", GetVolume(PlayerPrefSettings.AiStoreMusicVolume));
		if (UnityEngine.PlayerPrefs.HasKey("vSyncAndFPSLimit"))
		{
			int num = UnityEngine.PlayerPrefs.GetInt("vSyncAndFPSLimit");
			if (num >= 2)
			{
				num += 3;
			}
			PlayerPrefSettings.vSyncAndFPSLimitV2 = num;
			UnityEngine.PlayerPrefs.DeleteKey("vSyncAndFPSLimit");
		}
		SetFPSSetting(PlayerPrefSettings.vSyncAndFPSLimitV2);
		AntiAliasingHelper.SetAntiAliasingSetting(AntiAliasingHelper.GetPlayerAntiAliasingSetting());
		SetHbaoQuality((Quality)PlayerPrefSettings.hbaoQuality);
		SetShadows(PlayerPrefSettings.shadows);
		_liftGammaGain = volumeProfile?.components.FirstOrDefault((VolumeComponent x) => x is LiftGammaGain) as LiftGammaGain;
		SetUpGammaSlider();
		gammaSlider.value = PlayerPrefSettings.gamma;
		SetUpUIZoomingSetting();
		LocalizorManager.SetUsedLanguage(PlayerPrefSettings.Locale);
		TimeHelper.use12h = PlayerPrefSettings.use12h;
		UnitHelper.useImperial = PlayerPrefSettings.useImperial;
		GameManager.SetMinutesMultiplier(PlayerPrefSettings.GameSpeed);
		InputHelper.SetupPlayerInput();
		if (!PlayerPrefs.HasKey(PlayerPref.LowDetailCityMap) && Singleton<SteamAPI>.Instance.steamApiEnabled && SteamUtils.IsRunningOnSteamDeck)
		{
			PlayerPrefSettings.LowDetailCityMap = true;
			QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
		}
	}

	public void ResetSelectedCategoryToDefaults()
	{
		if (ResetActions.TryGetValue(_selectedCategory, out var value))
		{
			value();
		}
		else
		{
			Debug.LogError("No reset action found for category: " + _selectedCategory);
		}
	}

	public void ResetAllCategoriesToDefaults()
	{
		foreach (Action value in ResetActions.Values)
		{
			value();
		}
	}

	public void ResetAllCategoriesToDefaultsWithConfirm()
	{
		if (PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			ResetAllCategoriesToDefaults();
			return;
		}
		LanguageChangeEventDataHolder bodyData = "settings_are_you_sure_reset_all".Localize();
		Action onConfirmAction = ResetAllCategoriesToDefaults;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction, null, "common_reset_all");
	}

	private void SelectCategory(string category, RectTransform button, bool instant = false)
	{
		splitterIndicator.sizeDelta = new Vector2(button.rect.width, splitterIndicator.sizeDelta.y);
		if (instant)
		{
			Vector3 position = splitterIndicator.position;
			position.x = button.position.x;
			splitterIndicator.position = position;
		}
		else
		{
			splitterIndicator.DOMoveX(button.position.x, indicatorAnimationTime).SetLink(splitterIndicator.gameObject).SetUpdate(isIndependentUpdate: true);
		}
		foreach (Transform item in base.transform.Find("Views"))
		{
			item.gameObject.SetActive(item.name == category);
		}
		_selectedCategory = category;
		UpdateResetButtonLabels();
	}

	private IEnumerator DeferUpdateSelectedCategory()
	{
		yield return null;
		if ((bool)_selectedCategoryButton)
		{
			SelectCategory(_selectedCategory, _selectedCategoryButton, instant: true);
		}
	}

	private void ResetGraphicsSettingsToDefaults()
	{
		fullscreen.isOn = true;
		(int, int) tuple = _resolutions.Last();
		Screen.SetResolution(tuple.Item1, tuple.Item2, fullscreen.isOn);
		resolutionDropdown.ResetSelectedOption(_resolutions.Count - 1);
		PlayerPrefs.DeleteKey(PlayerPref.vSyncAndFPSLimitV2);
		vSyncAndFPSLimitDropdown.ResetSelectedOption(PlayerPrefSettings.vSyncAndFPSLimitV2);
		PlayerPrefs.DeleteKey(PlayerPref.antiAliasingSetting);
		antiAliasingDropdown.ResetSelectedOption((int)AntiAliasingHelper.GetPlayerAntiAliasingSetting());
		PlayerPrefs.DeleteKey(PlayerPref.hbaoQuality);
		hbaoQualityDropdown.ResetSelectedOption(PlayerPrefSettings.hbaoQuality);
		PlayerPrefs.DeleteKey(PlayerPref.shadows);
		shadowsDropdown.ResetSelectedOption(PlayerPrefSettings.shadows);
		PlayerPrefs.DeleteKey(PlayerPref.LowDetailCityMap);
		lowDetailCityMap.isOn = PlayerPrefSettings.LowDetailCityMap;
		PlayerPrefs.DeleteKey(PlayerPref.uiZooming);
		uiZoomingSlider.value = PlayerPrefSettings.uiZooming;
		PlayerPrefs.DeleteKey(PlayerPref.showFps);
		showFps.isOn = PlayerPrefSettings.showFps;
		PlayerPrefs.DeleteKey(PlayerPref.gamma);
		gammaSlider.value = PlayerPrefSettings.gamma;
		OnQualitySettingsChanged(QualitySettings.GetQualityLevel());
		UnityEngine.PlayerPrefs.Save();
	}

	private void ResetModSettingsToDefaults()
	{
		OptionsService.ResetAllToDefaults();
	}

	private void SetUpFullScreenSetting()
	{
		fullscreen.isOn = Screen.fullScreen;
		fullscreen.onValueChanged.AddListener(delegate
		{
			Screen.SetResolution(Screen.width, Screen.height, fullscreen.isOn);
		});
	}

	private void SetUpResolutionSetting()
	{
		List<string> resolutionOptions = _resolutions.Select(((int width, int height) r) => $"{r.width}x{r.height}").ToList();
		int selectedOption = resolutionOptions.FindIndex((string x) => x == $"{Screen.width}x{Screen.height}");
		resolutionDropdown.SetOptions(resolutionOptions, localize: false, selectedOption);
		resolutionDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			List<int> source = resolutionOptions[optionIndex].Split('x').Select(int.Parse).ToList();
			Screen.SetResolution(source.First(), source.Last(), fullscreen.isOn);
		});
	}

	private void SetUpvSyncAndFPSSetting()
	{
		float num = (float)Screen.currentResolution.refreshRateRatio.value;
		List<string> newOptions = new List<string>
		{
			"menu_options_none".GetLocalization(),
			LocalizorManager.GetLocalization("menu_options_fps_vsync_x", new
			{
				x = Mathf.CeilToInt(num / 1f)
			}),
			LocalizorManager.GetLocalization("menu_options_fps_vsync_x", new
			{
				x = Mathf.CeilToInt(num / 2f)
			}),
			LocalizorManager.GetLocalization("menu_options_fps_vsync_x", new
			{
				x = Mathf.CeilToInt(num / 3f)
			}),
			LocalizorManager.GetLocalization("menu_options_fps_vsync_x", new
			{
				x = Mathf.CeilToInt(num / 4f)
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 30
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 60
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 90
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 120
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 144
			}),
			LocalizorManager.GetLocalization("menu_options_fps_x_fps", new
			{
				x = 240
			})
		};
		vSyncAndFPSLimitDropdown.onOptionSelected.RemoveAllListeners();
		vSyncAndFPSLimitDropdown.SetOptions(newOptions, localize: false, PlayerPrefSettings.vSyncAndFPSLimitV2);
		vSyncAndFPSLimitDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			PlayerPrefSettings.vSyncAndFPSLimitV2 = optionIndex;
			SetFPSSetting(optionIndex);
		});
	}

	private void SetUpShowFps()
	{
		showFps.isOn = PlayerPrefSettings.showFps;
		showFps.onValueChanged.AddListener(delegate(bool on)
		{
			PlayerPrefSettings.showFps = on;
			FpsMeter.onShowFpsOptionChanged.Invoke(on);
		});
	}

	private void SetUpGfxQualitySetting()
	{
		List<string> newOptions = QualitySettings.names.Select((string x) => "menu_options_quality_" + x).ToList();
		gfxQualityDropdown.onOptionSelected.RemoveAllListeners();
		gfxQualityDropdown.SetOptions(newOptions, localize: true, QualitySettings.GetQualityLevel());
		gfxQualityDropdown.onOptionSelected.AddListener(OnQualitySettingsChanged);
	}

	private void OnQualitySettingsChanged(int optionIndex)
	{
		QualitySettings.SetQualityLevel(optionIndex, applyExpensiveChanges: true);
		particleQualityDropdown.SelectOption(optionIndex);
		PlayerPrefSettings.particleQuality = optionIndex;
		textureQualityDropdown.SelectOption(optionIndex);
		PlayerPrefSettings.textureQuality = optionIndex;
	}

	private void SetUpParticleQualitySetting()
	{
		List<string> newOptions = QualitySettings.names.Select((string x) => "menu_options_quality_" + x).ToList();
		particleQualityDropdown.onOptionSelected.RemoveAllListeners();
		int num = PlayerPrefSettings.particleQuality;
		if (num == -1)
		{
			num = (PlayerPrefSettings.particleQuality = QualitySettings.GetQualityLevel());
		}
		particleQualityDropdown.SetOptions(newOptions, localize: true, num);
		particleQualityDropdown.onOptionSelected.AddListener(SetParticleQuality);
	}

	private static void SetParticleQuality(int optionIndex)
	{
		PlayerPrefSettings.particleQuality = optionIndex;
		QualitySettings.particleRaycastBudget = QualitySettingsData[optionIndex].particleRaycastBudget;
		QualitySettings.softParticles = QualitySettingsData[optionIndex].softParticles;
	}

	private void SetUpTextureQualitySetting()
	{
		List<string> newOptions = QualitySettings.names.Select((string x) => "menu_options_quality_" + x).ToList();
		textureQualityDropdown.onOptionSelected.RemoveAllListeners();
		int num = PlayerPrefSettings.textureQuality;
		if (num == -1)
		{
			num = (PlayerPrefSettings.textureQuality = QualitySettings.GetQualityLevel());
		}
		textureQualityDropdown.SetOptions(newOptions, localize: true, num);
		textureQualityDropdown.onOptionSelected.AddListener(SetTextureSettings);
	}

	private static void SetTextureSettings(int optionIndex)
	{
		PlayerPrefSettings.textureQuality = optionIndex;
		QualitySettings.anisotropicFiltering = QualitySettingsData[optionIndex].anisotropicFiltering;
		QualitySettings.streamingMipmapsMemoryBudget = QualitySettingsData[optionIndex].streamingMipmapsMemoryBudget;
		QualitySettings.streamingMipmapsRenderersPerFrame = QualitySettingsData[optionIndex].streamingMipmapsRenderersPerFrame;
		QualitySettings.streamingMipmapsMaxLevelReduction = QualitySettingsData[optionIndex].streamingMipmapsMaxLevelReduction;
		QualitySettings.streamingMipmapsMaxFileIORequests = QualitySettingsData[optionIndex].streamingMipmapsMaxFileIORequests;
		QualitySettings.globalTextureMipmapLimit = QualitySettingsData[optionIndex].globalTextureMipmapLimit;
	}

	private void SetUpAntiAliasingSetting()
	{
		List<string> newOptions = (from x in Enum.GetNames(typeof(AntiAliasingSetting))
			select (!(x == "None")) ? ("menu_options_aa_" + x) : "menu_options_none").ToList();
		antiAliasingDropdown.onOptionSelected.RemoveAllListeners();
		antiAliasingDropdown.SetOptions(newOptions, localize: true, (int)AntiAliasingHelper.GetPlayerAntiAliasingSetting());
		antiAliasingDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			PlayerPrefSettings.antiAliasingSetting = optionIndex;
			AntiAliasingHelper.SetAntiAliasingSetting((AntiAliasingSetting)optionIndex);
		});
	}

	private void SetUpHbaoQualitySetting()
	{
		List<string> qualityOptions = GetQualityOptions();
		hbaoQualityDropdown.onOptionSelected.RemoveAllListeners();
		hbaoQualityDropdown.SetOptions(qualityOptions, localize: true, UnityEngine.PlayerPrefs.GetInt("hbaoQuality", 2));
		hbaoQualityDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			PlayerPrefSettings.hbaoQuality = optionIndex;
			SetHbaoQuality((Quality)optionIndex);
		});
	}

	private void SetUpShadowsSetting()
	{
		shadowsDropdown.onOptionSelected.RemoveAllListeners();
		int num = PlayerPrefSettings.shadows;
		if (num < 0 || num >= ShadowsOptions.Count)
		{
			num = ShadowsOptions.Count - 1;
		}
		shadowsDropdown.SetOptions(ShadowsOptions, localize: true, num);
		shadowsDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			PlayerPrefSettings.shadows = optionIndex;
			SetShadows(optionIndex);
		});
	}

	private void SetUpGammaSlider()
	{
		gammaSlider.minValue = minGamma;
		gammaSlider.maxValue = maxGamma;
		gammaSlider.value = PlayerPrefSettings.gamma;
		if (_liftGammaGain != null)
		{
			_liftGammaGain.gamma.value = new Vector4(_liftGammaGain.gamma.value.x, _liftGammaGain.gamma.value.y, _liftGammaGain.gamma.value.z, gammaSlider.value);
		}
		int percentage = Mathf.RoundToInt((gammaSlider.value - minGamma) / (maxGamma - minGamma) * 100f);
		gammaPercentage.text = $"{percentage}%";
		gammaSlider.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.gamma = gammaSlider.value;
			if (_liftGammaGain != null)
			{
				_liftGammaGain.gamma.value = new Vector4(_liftGammaGain.gamma.value.x, _liftGammaGain.gamma.value.y, _liftGammaGain.gamma.value.z, gammaSlider.value);
			}
			percentage = Mathf.RoundToInt((gammaSlider.value - minGamma) / (maxGamma - minGamma) * 100f);
			gammaPercentage.text = $"{percentage}%";
		});
	}

	private void SetUpLowDetailMap()
	{
		lowDetailCityMap.isOn = PlayerPrefSettings.LowDetailCityMap;
		lowDetailCityMap.onValueChanged.AddListener(delegate(bool on)
		{
			PlayerPrefSettings.LowDetailCityMap = on;
		});
	}

	private void SetUpUIZoomingSetting()
	{
		uiZoomingSlider.minValue = minUIZooming;
		uiZoomingSlider.maxValue = maxUIZooming;
		uiZoomingSlider.value = UnityEngine.PlayerPrefs.GetFloat("uiZooming", 1f);
		uiZoomingSlider.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.uiZooming = uiZoomingSlider.value;
			CanvasScaler[] canvasScalers = _canvasScalers;
			foreach (CanvasScaler obj in canvasScalers)
			{
				obj.scaleFactor = uiZoomingSlider.value;
				obj.referenceResolution = new Vector2(defaultReferenceResolution.x / uiZoomingSlider.value, defaultReferenceResolution.y / uiZoomingSlider.value);
			}
			uiZoomingPercentage.text = $"{Mathf.RoundToInt(uiZoomingSlider.value * 100f)}%";
		});
		uiZoomingPercentage.text = $"{Mathf.RoundToInt(uiZoomingSlider.value * 100f)}%";
	}

	private void SetResetWindowsSetting()
	{
		resetWindowsPositionsSection.SetActive(SaveGameManager.Current != null && InstanceBehavior<UIs>.Instance != null);
	}

	public void ResetWindowsPosition()
	{
		if (!(InstanceBehavior<UIs>.Instance == null))
		{
			InstanceBehavior<UIs>.Instance.draggableWindows.ResetWindowsPositions();
			Notifications.Show(NotificationType.Success, "menu_options_reset_windows_notification", null, 4f, "menu_options_reset_windows_notification");
		}
	}

	private void ResetAudioSettingsToDefault()
	{
		PlayerPrefs.DeleteKey(PlayerPref.GlobalVolume);
		globalVolume.value = PlayerPrefSettings.GlobalVolume;
		PlayerPrefs.DeleteKey(PlayerPref.MenuMusicVolume);
		menuMusicVolume.value = PlayerPrefSettings.MenuMusicVolume;
		PlayerPrefs.DeleteKey(PlayerPref.RadioVolume);
		radioVolume.value = PlayerPrefSettings.RadioVolume;
		PlayerPrefs.DeleteKey(PlayerPref.SfxVolume);
		sfxVolume.value = PlayerPrefSettings.SfxVolume;
		PlayerPrefs.DeleteKey(PlayerPref.AiStoreMusicVolume);
		aiStoreMusicVolume.value = PlayerPrefSettings.AiStoreMusicVolume;
		UnityEngine.PlayerPrefs.Save();
	}

	private void SetUpVolumeSetting()
	{
		globalVolume.SetValueWithoutNotify(PlayerPrefSettings.GlobalVolume);
		globalVolume.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.GlobalVolume = globalVolume.value;
			float volume = GetVolume(globalVolume.value);
			audioMixer.SetFloat("MasterVolume", volume);
			vehicleAudioMixer.SetFloat("attenuation", volume);
			SoundManager.SetOriginalAttenuation(volume);
		});
		menuMusicVolume.SetValueWithoutNotify(PlayerPrefSettings.MenuMusicVolume);
		menuMusicVolume.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.MenuMusicVolume = menuMusicVolume.value;
			audioMixer.SetFloat("MenuMusicVolume", GetVolume(menuMusicVolume));
		});
		radioVolume.SetValueWithoutNotify(PlayerPrefSettings.RadioVolume);
		radioVolume.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.RadioVolume = radioVolume.value;
			audioMixer.SetFloat("RadioVolume", GetVolume(radioVolume));
			if (InstanceBehavior<UIs>.Instance != null)
			{
				InstanceBehavior<UIs>.Instance.smartphoneUI.radioControls.UpdateVolume();
			}
		});
		sfxVolume.SetValueWithoutNotify(PlayerPrefSettings.SfxVolume);
		sfxVolume.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.SfxVolume = sfxVolume.value;
			float volume = GetVolume(sfxVolume);
			audioMixer.SetFloat("FXVolume", volume);
			vehicleAudioMixer.SetFloat("fx", volume);
		});
		aiStoreMusicVolume.SetValueWithoutNotify(PlayerPrefSettings.AiStoreMusicVolume);
		aiStoreMusicVolume.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.AiStoreMusicVolume = aiStoreMusicVolume.value;
			audioMixer.SetFloat("OutsideBuildingMusicVolume", GetVolume(aiStoreMusicVolume));
			if (InstanceBehavior<BuildingManager>.Instance?.buildingRegistration != null && !InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
			{
				LoudspeakerController.onBuildingRadioVolumeChanged?.Invoke();
			}
			onAiStoreMusicVolumeUpdated?.Invoke();
		});
	}

	public static float GetVolume(Slider slider)
	{
		return GetVolume(slider.value);
	}

	public static float GetVolume(float value)
	{
		if (!Mathf.Approximately(value, 0f))
		{
			return Mathf.Log10(value) * 20f;
		}
		return -80f;
	}

	public void UpdateRadioVolume()
	{
		radioVolume.SetValueWithoutNotify(PlayerPrefSettings.RadioVolume);
	}

	private void ResetControlsSettingsToDefaults()
	{
		PlayerPrefs.DeleteKey(PlayerPref.VehicleMouseInput);
		vehicleMouseInput.isOn = PlayerPrefSettings.VehicleMouseInput;
		PlayerPrefs.DeleteKey(PlayerPref.InvertRotation);
		invertRotation.isOn = PlayerPrefSettings.InvertRotation;
		PlayerPrefs.DeleteKey(PlayerPref.RunByDefaultIndoors);
		runByDefaultIndoors.isOn = PlayerPrefSettings.RunByDefaultIndoors;
		PlayerPrefs.DeleteKey(PlayerPref.SteeringAssist);
		steeringAssist.isOn = PlayerPrefSettings.SteeringAssist;
		UnityEngine.PlayerPrefs.DeleteKey("rebinds" + InstanceBehavior<GlobalReferences>.Instance.PlayerInput.name);
		UnityEngine.PlayerPrefs.DeleteKey("rebinds" + InstanceBehavior<GlobalReferences>.Instance.VehicleInput.name);
		UnityEngine.PlayerPrefs.Save();
		RebindActionUI[] componentsInChildren = bindings.GetComponentsInChildren<RebindActionUI>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ResetToDefault();
		}
	}

	private void SetUpVehicleMouseInputSetting()
	{
		vehicleMouseInput.isOn = PlayerPrefSettings.VehicleMouseInput;
		vehicleMouseInput.onValueChanged.AddListener(delegate(bool state)
		{
			PlayerPrefSettings.VehicleMouseInput = state;
			InputHelper.SetupPlayerInput();
		});
	}

	private void SetUpInvertRotationSetting()
	{
		invertRotation.isOn = PlayerPrefSettings.InvertRotation;
		invertRotation.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.InvertRotation = invertRotation.isOn;
			PedestrianCam.invertRotation = invertRotation.isOn;
		});
	}

	private void SetUpRunByDefaultIndoorsSetting()
	{
		runByDefaultIndoors.isOn = PlayerPrefSettings.RunByDefaultIndoors;
		runByDefaultIndoors.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.RunByDefaultIndoors = runByDefaultIndoors.isOn;
			PlayerController.runByDefaultIndoors = runByDefaultIndoors.isOn;
		});
	}

	private void SetUpSteeringAssistSetting()
	{
		steeringAssist.isOn = PlayerPrefSettings.SteeringAssist;
		steeringAssist.onValueChanged.AddListener(delegate
		{
			PlayerPrefSettings.SteeringAssist = steeringAssist.isOn;
		});
	}

	private void ResetOthersSettingsToDefault()
	{
		PlayerPrefs.DeleteKey(PlayerPref.MinutesBetweenAutoSaves);
		timeBetweenAutoSavesSlider.value = PlayerPrefSettings.MinutesBetweenAutoSaves;
		PlayerPrefs.DeleteKey(PlayerPref.MaxAutoSavesPerGame);
		maxAutoSavesPerGameSlider.value = PlayerPrefSettings.MaxAutoSavesPerGame;
		PlayerPrefs.DeleteKey(PlayerPref.use12h);
		timeFormatToggle.isOn = PlayerPrefSettings.use12h;
		PlayerPrefs.DeleteKey(PlayerPref.useImperial);
		unitsToggle.isOn = PlayerPrefSettings.useImperial;
		PlayerPrefs.DeleteKey(PlayerPref.NumberFormat);
		numberFormatDropdown.SelectOption(0);
		PlayerPrefs.DeleteKey(PlayerPref.GameSpeed);
		gameSpeedSlider.value = PlayerPrefSettings.GameSpeed * gameSpeedSliderMultiplier;
		PlayerPrefs.DeleteKey(PlayerPref.SeasonalDecorations);
		seasonalDecorationsToggle.isOn = PlayerPrefSettings.SeasonalDecorations;
		PlayerPrefs.DeleteKey(PlayerPref.ControlHints);
		controlsHintsToggle.isOn = PlayerPrefSettings.ControlHints;
		UnityEngine.PlayerPrefs.Save();
	}

	private void SetUpLanguageSetting()
	{
		_availableLanguages = LocalizorManager.GetAvailableLanguages();
		int selectedOption = Array.FindIndex(_availableLanguages, (string x) => x == LocalizorManager.LoadedLocale);
		List<string> newOptions = _availableLanguages.Select(LocalizorManager.GetAvailableLanguagesPrettified).ToList();
		languageDropdown.SetOptions(newOptions, localize: false, selectedOption);
		languageDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			string text = _availableLanguages[optionIndex];
			UnityEngine.PlayerPrefs.SetString("Locale", text);
			LocalizorManager.SetUsedLanguage(text);
		});
	}

	private void SetUpDifficultySetting()
	{
		if (!InstanceBehavior<GameManager>.Instance || InteriorDesignerHelper.BlueprintCreatorMode)
		{
			difficultyContainer.SetActive(value: false);
			return;
		}
		Difficulty difficulty = SaveGameManager.Current.gameVariables.difficulty;
		bool flag = difficulty == Difficulty.Custom;
		difficultyDropdown.gameObject.SetActive(!flag);
		difficultyCustomizeButton.gameObject.SetActive(flag);
		if (!flag)
		{
			DifficultySetting difficultySettings = DifficultySetting.GetDifficultySettings(difficulty);
			List<string> newOptions = InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Select((DifficultySetting x) => x.key).ToList();
			int selectedOption = Array.IndexOf(InstanceBehavior<GlobalReferences>.Instance.difficultySettings, difficultySettings);
			difficultyDropdown.SetOptions(newOptions, localize: true, selectedOption);
			difficultyDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
			{
				SaveGameManager.ApplyNewDifficulty(InstanceBehavior<GlobalReferences>.Instance.difficultySettings[optionIndex].ToGameVariables());
			});
		}
	}

	private void SetUpFormats()
	{
		SetupNumberFormat();
		timeFormatToggle.SetIsOnWithoutNotify(PlayerPrefSettings.use12h);
		unitsToggle.SetIsOnWithoutNotify(PlayerPrefSettings.useImperial);
		timeFormatToggle.onValueChanged.AddListener(delegate(bool val)
		{
			TimeHelper.use12h = val;
			PlayerPrefSettings.use12h = val;
			LocalizorManager.OnLanguageChanged();
		});
		unitsToggle.onValueChanged.AddListener(delegate(bool val)
		{
			UnitHelper.useImperial = val;
			PlayerPrefSettings.useImperial = val;
			LocalizorManager.OnLanguageChanged();
		});
	}

	private void SetupNumberFormat()
	{
		int num = numberFormatSetups.FindIndex(0, (NumberFormatSetup format) => format.Id == PlayerPrefSettings.NumberFormat);
		List<string> newOptions = numberFormatSetups.ConvertAll((NumberFormatSetup format) => format.VisualFormat);
		if (num < 0)
		{
			num = 0;
		}
		numberFormatDropdown.SetOptions(newOptions, localize: false, num);
		numberFormatDropdown.onOptionSelected.AddListener(delegate(int optionIndex)
		{
			PlayerPrefSettings.NumberFormat = numberFormatSetups[optionIndex].Id;
			CultureHelper.UpdateStoredCultureInfo();
		});
	}

	private void SetUpSeasonalDecorationsSetting()
	{
		seasonalDecorationsToggle.SetIsOnWithoutNotify(PlayerPrefSettings.SeasonalDecorations);
		seasonalDecorationsToggle.onValueChanged.AddListener(delegate(bool val)
		{
			PlayerPrefSettings.SeasonalDecorations = val;
			SeasonHelper.onSeasonalDecorationsOptionChanged.Invoke(val);
		});
	}

	private void SetUpControlsHintsSetting()
	{
		controlsHintsToggle.SetIsOnWithoutNotify(PlayerPrefSettings.ControlHints);
		controlsHintsToggle.onValueChanged.AddListener(delegate(bool value)
		{
			PlayerPrefSettings.ControlHints = value;
		});
	}

	public void OpenRadioFolder()
	{
		FolderHelper.OpenFolder(RadioPlayer.GetRadioPath());
	}

	public void RefreshSongs()
	{
		if (InstanceBehavior<GameManager>.Instance != null)
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.TryLoadLocalSongs();
		}
		SetSongsFound();
		ValidateSongs();
	}

	private void ValidateSongs()
	{
		if (base.isActiveAndEnabled)
		{
			if (_validateSongsCoroutine != null)
			{
				StopCoroutine(_validateSongsCoroutine);
			}
			_validateSongsCoroutine = StartCoroutine(ValidateSongsCoroutine());
		}
	}

	private IEnumerator ValidateSongsCoroutine()
	{
		yield return new WaitForSecondsRealtime(0.5f);
		yield return AudioFileFormatHelper.FindUnsupportedAudioFiles(RadioPlayer.GetRadioPath(), _unsupportedSongFiles);
		SetSongsFound();
	}

	private void SetSongsFound()
	{
		int num = 0;
		string[] files = Directory.GetFiles(RadioPlayer.GetRadioPath());
		foreach (string text in files)
		{
			if (AudioFileFormatHelper.GetAudioTypeFromExtension(text) != AudioType.UNKNOWN && !_unsupportedSongFiles.Contains(Path.GetFileName(text)))
			{
				num++;
			}
		}
		if (_unsupportedSongFiles.Count > 0)
		{
			songsFound.Key = "menu_options_song_files_found_with_unsupported";
			songsFound.Arguments = new
			{
				supportedFileCount = num,
				unsupportedFileNames = _unsupportedSongFiles.Listify()
			};
		}
		else
		{
			songsFound.Key = "menu_options_song_files_found";
			songsFound.Arguments = new
			{
				count = num
			};
		}
	}

	private void SetUpTimeBetweenAutoSavesSetting()
	{
		timeBetweenAutoSavesSlider.minValue = minTimeBetweenAutoSaves;
		timeBetweenAutoSavesSlider.maxValue = maxTimeBetweenAutoSaves;
		timeBetweenAutoSavesSlider.value = PlayerPrefSettings.MinutesBetweenAutoSaves;
		timeBetweenAutoSavesSlider.onValueChanged.AddListener(delegate
		{
			int minutes = (PlayerPrefSettings.MinutesBetweenAutoSaves = (int)timeBetweenAutoSavesSlider.value);
			if ((bool)InstanceBehavior<GameManager>.Instance)
			{
				InstanceBehavior<GameManager>.Instance.ResetNextAutoSave();
			}
			timeBetweenAutoSavesAmount.Arguments = new { minutes };
		});
		timeBetweenAutoSavesAmount.Arguments = new
		{
			minutes = (int)timeBetweenAutoSavesSlider.value
		};
	}

	private void SetUpMaxAutoSavesPerGameSetting()
	{
		maxAutoSavesPerGameSlider.minValue = minAutoSavesPerGame;
		maxAutoSavesPerGameSlider.maxValue = maxAutoSavesPerGame;
		maxAutoSavesPerGameSlider.value = UnityEngine.PlayerPrefs.GetInt("MaxAutoSavesPerGame", 3);
		maxAutoSavesPerGameSlider.onValueChanged.AddListener(delegate
		{
			int num = (int)maxAutoSavesPerGameSlider.value;
			UnityEngine.PlayerPrefs.SetInt("MaxAutoSavesPerGame", num);
			maxAutoSavesPerGameAmount.Arguments = new
			{
				amount = num
			};
		});
		maxAutoSavesPerGameAmount.Arguments = new
		{
			amount = (int)maxAutoSavesPerGameSlider.value
		};
	}

	private void SetUpUnstuckButton()
	{
		unstuckButton.onClick.RemoveAllListeners();
		unstuckButton.onClick.AddListener(PlayerController.Unstuck);
	}

	private void SetUpGameSpeedSetting()
	{
		gameSpeedSlider.minValue = minGameSpeed;
		gameSpeedSlider.maxValue = maxGameSpeed;
		gameSpeedSlider.value = PlayerPrefSettings.GameSpeed * gameSpeedSliderMultiplier;
		gameSpeedSlider.onValueChanged.AddListener(delegate(float newValue)
		{
			float num2 = (PlayerPrefSettings.GameSpeed = newValue / gameSpeedSliderMultiplier);
			GameManager.SetMinutesMultiplier(num2);
			int num4 = Mathf.RoundToInt(100f * num2);
			gameSpeedMultiplier.text = $"{num4}%";
		});
		int num = Mathf.RoundToInt(100f / (float)maxGameSpeed * gameSpeedSlider.value);
		gameSpeedMultiplier.text = $"{num}%";
	}

	private void SetUpTrackingSetting()
	{
		allowTrackingToggle.isOn = PlayerPrefSettings.allowTracking;
		allowTrackingToggle.onValueChanged.AddListener(delegate(bool allow)
		{
			PlayerPrefSettings.allowTracking = allow;
		});
	}

	public void RequestDataDeletion()
	{
		GameAnalyticsHelper.RequestDataDeletion();
		Notifications.Show(NotificationType.Success, "menu_options_data_deletion_requested", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		requestDataDeletionButton.interactable = false;
	}

	public static void SetFPSSetting(int selectedTarget)
	{
		int num = -1;
		int vSyncCount = 0;
		switch (selectedTarget)
		{
		case 1:
			vSyncCount = 1;
			break;
		case 2:
			vSyncCount = 2;
			break;
		case 3:
			vSyncCount = 3;
			break;
		case 4:
			vSyncCount = 4;
			break;
		case 5:
			num = 30;
			break;
		case 6:
			num = 60;
			break;
		case 7:
			num = 90;
			break;
		case 8:
			num = 120;
			break;
		case 9:
			num = 144;
			break;
		case 10:
			num = 240;
			break;
		}
		if (SaveGameManager.Current == null)
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = GetMenuFrameRate(num);
		}
		else
		{
			QualitySettings.vSyncCount = vSyncCount;
			Application.targetFrameRate = num;
		}
	}

	private static int GetMenuFrameRate(int targetFrameRateFromSetting)
	{
		if (targetFrameRateFromSetting <= 0)
		{
			return 144;
		}
		return Mathf.Min(targetFrameRateFromSetting, 144);
	}

	public static void SetHbaoQuality(Quality quality)
	{
		if (!(InstanceBehavior<GameManager>.Instance?.timeOfDayController == null))
		{
			InstanceBehavior<GameManager>.Instance.timeOfDayController.SetHbaoQuality(quality);
		}
	}

	public static void SetShadows(int setting)
	{
		GlobalEvents.onShadowsSettingChanged?.Invoke(setting);
	}

	public static void SetQualityLevelToLow()
	{
		QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);
		PlayerPrefSettings.antiAliasingSetting = 2;
		AntiAliasingHelper.SetAntiAliasingSetting(AntiAliasingSetting.Smaa1X);
		PlayerPrefSettings.hbaoQuality = 0;
		SetHbaoQuality(Quality.Low);
		SetQualitySettingsDataIfNeeded();
		SetTextureSettings(0);
		SetParticleQuality(0);
		PlayerPrefSettings.shadows = 0;
		SetShadows(0);
	}

	private List<string> GetQualityOptions()
	{
		return (from x in Enum.GetNames(typeof(Quality))
			select "menu_options_quality_" + x).ToList();
	}

	private void RefreshModsTabButton()
	{
		bool flag = OptionsService.RegisteredEntries.Count > 0;
		modsTabButton.SetActive(flag);
		if (!flag && _selectedCategory == "Mods")
		{
			base.transform.Find("Menu").GetChild(0)?.GetComponent<Button>().onClick.Invoke();
		}
	}

	public void OpenDifficultyCustomizeUI()
	{
		difficultyCustomizeUI.gameObject.SetActive(value: true);
		difficultyCustomizePanel.SetPresetValuesFromCurrent();
	}

	public void ConfirmDifficultyCustomizeUI()
	{
		if (!HasDifficultyCustomizeUIUnsavedChanges())
		{
			difficultyCustomizeUI.gameObject.SetActive(value: false);
		}
		else if (HasDifficultyCustomizeUIUnsavedChanges())
		{
			HudConfirm.Show(null, "ba:difficulty_customize_prompt_confirm", delegate
			{
				SaveGameManager.ApplyNewDifficulty(CustomGameOptionsHandler.GetPreset());
				difficultyCustomizeUI.gameObject.SetActive(value: false);
			});
		}
	}

	public void CloseDifficultyCustomizeUI()
	{
		TryCloseDifficultyCustomizeUI();
	}

	public bool TryCloseDifficultyCustomizeUI()
	{
		if (!difficultyCustomizeUI.gameObject.activeSelf)
		{
			return false;
		}
		if (HasDifficultyCustomizeUIUnsavedChanges())
		{
			HudConfirm.Show(null, "change_character_clothes_unsaved_changes_warning", delegate
			{
				difficultyCustomizeUI.gameObject.SetActive(value: false);
			});
			return true;
		}
		difficultyCustomizeUI.gameObject.SetActive(value: false);
		return true;
	}

	private bool HasDifficultyCustomizeUIUnsavedChanges()
	{
		if (!difficultyCustomizeUI.gameObject.activeSelf)
		{
			return false;
		}
		return !CustomGameOptionsHandler.GetPreset().EqualsFlexibleValues(SaveGameManager.Current.gameVariables);
	}

	private void OnDisable()
	{
		InputHelper.UpdateBindings();
	}

	private void OnDestroy()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
		if (QualitySettingsHandle.IsValid())
		{
			Addressables.Release(QualitySettingsHandle);
		}
		OptionsService.OnChanged -= RefreshModsTabButton;
	}
}
