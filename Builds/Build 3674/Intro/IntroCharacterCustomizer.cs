using System;
using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using BehaviorDesigner.Runtime.ObjectDrawers;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Characters.Skills;
using Blueprints;
using Character.Customization;
using Characters;
using Extensions;
using Helpers;
using HorizonBasedAmbientOcclusion.HighDefinition;
using Localizor;
using Scenes.MainMenu;
using Settings;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Intro;

public class IntroCharacterCustomizer : CharacterCustomizer
{
	private static bool DeveloperToolsEnabled = Application.isEditor || Environment.GetCommandLineArgs().Contains("-enable-dev-tools");

	public CharacterZoom characterZoom;

	[SerializeField]
	private GameObject intro;

	[SerializeField]
	private Button randomizeButton;

	[SerializeField]
	private Button toggleSlidersButton;

	[SerializeField]
	private VolumeProfile volume;

	[SerializeField]
	[FloatSlider(0f, 1f)]
	private float initialStrength;

	[SerializeField]
	[FloatSlider(0f, 1f)]
	private float initialFatness;

	[SerializeField]
	private GradientSlider skinColorGradientSlider;

	[SerializeField]
	private EyesColorPicker eyesColorPicker;

	[SerializeField]
	private GradientSlider eyesColorGradientSlider;

	public NamePicker characterNamePicker;

	public ElementPicker bodyPicker;

	public BodyValues bodyValues;

	[SerializeField]
	private GameObject bodyPanel;

	[SerializeField]
	private BodyCustomization bodyCustomization;

	[SerializeField]
	private GameObject optionsPanel;

	[SerializeField]
	private AppearanceBlendshapeOptions optionsPanelScript;

	[Header("Bored animations")]
	[SerializeField]
	protected BoredAnimations boredAnimations;

	[SerializeField]
	protected Image boredAnimationsImage;

	[SerializeField]
	protected Sprite boredAnimationsOnSprite;

	[SerializeField]
	protected Sprite boredAnimationsOffSprite;

	[SerializeField]
	private AppearanceSetter humanDefinitionHigh;

	[SerializeField]
	private AppearanceSetter humanDefinitionLow;

	private HBAO _hbao;

	private MenuVertical.Category[] _defaultCategories;

	private AppearanceTag[] _defaultTags;

	private bool _showingAllOptions;

	private const int TestAgeInDays = 1080;

	public AppearanceTag[] Tags => tags;

	private void Awake()
	{
		appearanceSetter = humanDefinitionHigh;
		humanDefinitionLow.gameObject.SetActive(value: false);
		_defaultCategories = menu.categories.ToArray();
		_defaultTags = tags.ToArray();
		AntiAliasingHelper.SetAntiAliasingSetting(AntiAliasingHelper.GetPlayerAntiAliasingSetting());
		if (CultureHelper.CultureInfo == null)
		{
			CultureHelper.UpdateStoredCultureInfo();
		}
		SetUpHbao();
		if (!InputHelper.IsInitialized())
		{
			InputHelper.SetupPlayerInput();
		}
		randomizeButton.onClick.AddListener(RandomizeButtonClick);
		CitizenHelper.Init();
		appearanceSetter.SetRandomAppearance(tags);
		appearanceSetter.data.strength = initialStrength;
		appearanceSetter.data.fatness = initialFatness;
		eyesColorPicker.Initialize();
		eyesColorGradientSlider.RandomizeColor();
		skinColorGradientSlider.RandomizeColor();
		appearanceSetter.data.color = skinColorGradientSlider.GetColor();
		appearanceSetter.UpdateVisuals();
		int ageInDays = ((SaveGameManager.Current == null) ? 1080 : TimeHelper.GetDaysByYears(SaveGameManager.Current.gameVariables.startingAge));
		appearanceSetter.SetAgeInDays(ageInDays);
		onMenuSelected = (Action)Delegate.Combine(onMenuSelected, new Action(ClosePanels));
		toggleSlidersButton.onClick.AddListener(ToggleSlidersPanel);
		onSubCategoryChanged.AddListener(delegate(AppearanceElementType elementType)
		{
			bool flag = optionsPanelScript.HasOptionsForElement(elementType);
			toggleSlidersButton.gameObject.SetActive(flag);
			optionsPanelScript.Show(flag && optionsPanelScript.isVisible, elementType);
		});
		toggleSlidersButton.gameObject.SetActive(optionsPanelScript.HasOptionsForElement(currentElementType));
		optionsPanelScript.Show(show: false, currentElementType);
	}

	protected override void Start()
	{
		base.Start();
		boredAnimationsImage.sprite = (boredAnimations.isBoredAnimationPausedSetting ? boredAnimationsOffSprite : boredAnimationsOnSprite);
		menu.OnCategoryButtonClick(menu.categories.First((MenuVertical.Category x) => x.linkId == "body"));
		ShowBodyPanel();
	}

	private void Update()
	{
		if (DeveloperToolsEnabled && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				SwitchHumanDefinitions();
			}
			if (Input.GetKeyDown(KeyCode.O))
			{
				SwitchAvailableOptions();
			}
		}
	}

	public void OnToggleBoredAnimationsClick()
	{
		bool flag = !boredAnimations.isBoredAnimationPausedSetting;
		boredAnimations.isBoredAnimationPausedSetting = flag;
		boredAnimationsImage.sprite = (flag ? boredAnimationsOffSprite : boredAnimationsOnSprite);
	}

	private void SwitchHumanDefinitions()
	{
		if (appearanceSetter == humanDefinitionHigh)
		{
			appearanceSetter = humanDefinitionLow;
			humanDefinitionHigh.gameObject.SetActive(value: false);
			humanDefinitionLow.transform.eulerAngles = humanDefinitionHigh.transform.eulerAngles;
			humanDefinitionLow.SetAppearance(humanDefinitionHigh.data);
			humanDefinitionLow.gameObject.SetActive(value: true);
		}
		else
		{
			appearanceSetter = humanDefinitionHigh;
			humanDefinitionLow.gameObject.SetActive(value: false);
			humanDefinitionHigh.transform.eulerAngles = humanDefinitionLow.transform.eulerAngles;
			humanDefinitionHigh.SetAppearance(humanDefinitionLow.data);
			humanDefinitionHigh.gameObject.SetActive(value: true);
		}
	}

	private void SwitchAvailableOptions()
	{
		if (_showingAllOptions)
		{
			tags = _defaultTags;
			menu.categories = _defaultCategories;
			menu.Reset();
			_showingAllOptions = false;
			return;
		}
		tags = ((AppearanceTag[])Enum.GetValues(typeof(AppearanceTag))).ToArray();
		List<MenuVertical.Category> list = new List<MenuVertical.Category>();
		AppearanceElementType[] getAppearanceElementTypes = AppearanceSetter.GetAppearanceElementTypes;
		foreach (AppearanceElementType appearanceElementType in getAppearanceElementTypes)
		{
			List<AppearanceElementVariant> elementVariants = appearanceSetter.GetElementVariants(appearanceElementType);
			if (elementVariants != null && elementVariants.Count > 1)
			{
				list.Add(new MenuVertical.Category
				{
					linkId = appearanceElementType.ToStringFast(),
					subCategories = new MenuVertical.SubCategory[0]
				});
			}
		}
		menu.categories = list.ToArray();
		menu.Reset();
		_showingAllOptions = true;
	}

	public override void Show(string elementTypeString)
	{
		if (elementTypeString.Equals("body", StringComparison.OrdinalIgnoreCase))
		{
			ShowBodyPanel();
		}
		else
		{
			base.Show(elementTypeString);
		}
	}

	protected override void ShowGridSelectionPanel()
	{
		base.ShowGridSelectionPanel();
		optionsPanel.SetActive(value: false);
		bodyPanel.SetActive(value: false);
	}

	protected override void ShowCurrentElement()
	{
		characterZoom.ZoomTo(currentElementType);
		BoredAnimations obj = boredAnimations;
		AppearanceElementType appearanceElementType = currentElementType;
		obj.pauseBoredAnimations = appearanceElementType == AppearanceElementType.Head || appearanceElementType == AppearanceElementType.Eyes || appearanceElementType == AppearanceElementType.Mouth || appearanceElementType == AppearanceElementType.Nose || appearanceElementType == AppearanceElementType.Hair || appearanceElementType == AppearanceElementType.HairAccessory || appearanceElementType == AppearanceElementType.HeadAccessory;
		base.ShowCurrentElement();
	}

	private void ShowBodyPanel()
	{
		bodyPanel.SetActive(value: true);
		gridSelectionPanel.SetActive(value: false);
		optionsPanel.SetActive(value: false);
		bodyCustomization.Show();
	}

	private void SetUpHbao()
	{
		if (!PlayerPrefs.HasKey(PlayerPref.hbaoQuality))
		{
			return;
		}
		volume.TryGet<HBAO>(out _hbao);
		if (!(_hbao == null))
		{
			switch ((Options.Quality)PlayerPrefSettings.hbaoQuality)
			{
			case Options.Quality.Low:
				_hbao.active = false;
				break;
			case Options.Quality.Medium:
				_hbao.active = true;
				_hbao.SetQuality(HBAO.Quality.Low);
				_hbao.SetAoRadius(0.5f);
				_hbao.intensity.overrideState = true;
				break;
			case Options.Quality.High:
				_hbao.active = true;
				_hbao.SetQuality(HBAO.Quality.High);
				_hbao.SetAoRadius(1f);
				_hbao.intensity.overrideState = true;
				break;
			}
		}
	}

	private void ToggleSlidersPanel()
	{
		optionsPanelScript.Show(!optionsPanelScript.isVisible, currentElementType);
	}

	private void ClosePanels()
	{
		elementPicker.gameObject.SetActive(value: false);
		bodyPicker.gameObject.SetActive(value: false);
		bodyValues.gameObject.SetActive(value: false);
		characterNamePicker.gameObject.SetActive(value: false);
		optionsPanelScript.Show(show: false, currentElementType);
		toggleSlidersButton.gameObject.SetActive(value: false);
	}

	private void RandomizeButtonClick()
	{
		if (bodyCustomization != null && bodyPanel.activeSelf)
		{
			bodyCustomization.RandomizeCurrentGender();
		}
		else
		{
			RandomizeCurrentElement();
		}
	}

	public void StartGame()
	{
		if (!characterNamePicker.HasANameSet)
		{
			ShowPlayerNameInput();
			return;
		}
		if (characterNamePicker.HasInvalidCharacters)
		{
			Notifications.ShowError("character_customization_notification_name_invalid_characters", "name_invalid_characters", trackOnSaveGame: false);
			return;
		}
		appearanceSetter.data.name = characterNamePicker.GetName();
		appearanceSetter.data.itemInHands = null;
		appearanceSetter.data.skills = new List<Skill>
		{
			new Skill
			{
				name = "ba:skill_customerservice",
				value = 50f
			}
		};
		SaveGameManager.Current.charactersData = new List<CharacterData> { appearanceSetter.data };
		string localization = LocalizorManager.GetLocalization("new_character_save_game", new
		{
			character = SaveGameManager.Current.charactersData.First().name
		});
		localization = FileSystemHelper.MakeValidFilename(localization).Trim();
		SaveGameManager.Current.SaveGameName = localization;
		base.gameObject.SetActive(value: false);
		intro.SetActive(value: true);
	}

	private void ShowPlayerNameInput()
	{
		Notifications.ShowError("character_customization_notification_name_required", "name_required", trackOnSaveGame: false);
		ShowBodyPanel();
	}

	public void MainMenu()
	{
		SceneManager.LoadScene("MainMenu");
	}
}
