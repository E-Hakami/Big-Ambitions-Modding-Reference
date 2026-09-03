using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using CameraControllers;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Character.Customization;

public class PlasticSurgeryUI : CharacterCustomizer
{
	public const int SurgeryHours = 2;

	[SerializeField]
	private PlasticSurgeryPricing pricing;

	[SerializeField]
	private Button toggleSlidersButton;

	[SerializeField]
	private GradientSlider skinColorGradientSlider;

	[SerializeField]
	private EyesColorPicker eyesColorPicker;

	[SerializeField]
	private GradientSlider eyesColorGradientSlider;

	[SerializeField]
	private BodyValues bodyValues;

	[SerializeField]
	private GameObject bodyPanel;

	[SerializeField]
	private GameObject optionsPanel;

	[SerializeField]
	private AppearanceBlendshapeOptions optionsPanelScript;

	[SerializeField]
	private GameObject colorPickerScrollRect;

	[SerializeField]
	private Button applyButton;

	[SerializeField]
	private TextLocalizationComponent applyButtonLabel;

	[SerializeField]
	private Vector3 cameraPosition;

	[SerializeField]
	private HoldClickButton zoomInButton;

	[SerializeField]
	private HoldClickButton zoomOutButton;

	[SerializeField]
	private Button randomizeButton;

	[SerializeField]
	private Button revertButton;

	[SerializeField]
	private Button automaticZoomButton;

	[SerializeField]
	private Image automaticZoomIcon;

	[SerializeField]
	private UiHoverTarget hoverTarget;

	private UiHoverTarget _previousHoverTarget;

	private float _playerPrice;

	private bool _idPriceInitialized;

	private bool _resetting;

	private void Awake()
	{
		eyesColorPicker.Initialize();
		onAppearanceChange = (Action)Delegate.Combine(onAppearanceChange, new Action(OnSelectionChange));
		onMenuSelected = (Action)Delegate.Combine(onMenuSelected, new Action(ClosePanels));
		BodyValues obj = bodyValues;
		obj.onAppearanceChange = (Action)Delegate.Combine(obj.onAppearanceChange, new Action(OnSelectionChange));
		applyButton.onClick.AddListener(BeginSurgery);
		toggleSlidersButton.onClick.AddListener(ToggleSlidersPanel);
		onSubCategoryChanged.AddListener(delegate(AppearanceElementType elementType)
		{
			bool flag = optionsPanelScript.HasOptionsForElement(elementType);
			toggleSlidersButton.gameObject.SetActive(flag);
			optionsPanelScript.Show(flag && optionsPanelScript.isVisible, elementType);
		});
		AppearanceBlendshapeOptions appearanceBlendshapeOptions = optionsPanelScript;
		appearanceBlendshapeOptions.onBlendshapeChange = (Action)Delegate.Combine(appearanceBlendshapeOptions.onBlendshapeChange, new Action(OnSelectionChange));
		AppearanceBlendshapeOptions appearanceBlendshapeOptions2 = optionsPanelScript;
		appearanceBlendshapeOptions2.onShown = (Action)Delegate.Combine(appearanceBlendshapeOptions2.onShown, (Action)delegate
		{
			if (currentElementType == AppearanceElementType.Eyes)
			{
				eyesColorGradientSlider.SetFromColor(appearanceSetter.data.eyesColor);
			}
		});
		AppearanceBlendshapeOptions appearanceBlendshapeOptions3 = optionsPanelScript;
		appearanceBlendshapeOptions3.onReset = (Action)Delegate.Combine(appearanceBlendshapeOptions3.onReset, (Action)delegate
		{
			CharacterData characterData = SaveGameManager.Current.charactersData.First();
			if (currentElementType == AppearanceElementType.Eyes)
			{
				eyesColorGradientSlider.SetFromColor(characterData.eyesColor);
			}
		});
		CharacterZoom zoom = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom;
		zoomInButton.onPointerDown.AddListener(delegate
		{
			zoom.SetZoomIn(set: true);
		});
		zoomInButton.onPointerUp.AddListener(delegate
		{
			zoom.SetZoomIn(set: false);
		});
		zoomOutButton.onPointerDown.AddListener(delegate
		{
			zoom.SetZoomOut(set: true);
		});
		zoomOutButton.onPointerUp.AddListener(delegate
		{
			zoom.SetZoomOut(set: false);
		});
		automaticZoomButton.onClick.AddListener(delegate
		{
			zoom.ToggleAutomaticZoom();
		});
		zoom.automaticZoomIcon = automaticZoomIcon;
		randomizeButton.onClick.AddListener(Randomize);
		revertButton.onClick.AddListener(RevertAppearance);
	}

	public void Show()
	{
		EmployeeUniformPreview employeeUniformPreview = InstanceBehavior<GameManager>.Instance.employeeUniformPreview;
		menu.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.PlasticSurgeryUI);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		PedestrianCam.blockCameraZoom = true;
		employeeUniformPreview.SetCameraPosition(cameraPosition);
		employeeUniformPreview.Show();
		_previousHoverTarget = employeeUniformPreview.characterZoom.hoverTarget;
		employeeUniformPreview.characterZoom.hoverTarget = hoverTarget;
		if ((object)appearanceSetter == null)
		{
			appearanceSetter = employeeUniformPreview.appearanceSetter;
		}
		ShowBodyPanel();
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			base.gameObject.SetActive(value: true);
			menu.OnCategoryButtonClick(menu.categories.First((MenuVertical.Category x) => x.linkId == "body"));
			ClosePanels();
			ShowBodyPanel();
			optionsPanelScript.Show(show: false, AppearanceElementType.Gender);
			RevertAppearance();
		}, 3);
	}

	private void RevertAppearance()
	{
		_resetting = true;
		CharacterData characterData = SaveGameManager.Current.charactersData.First();
		eyesColorGradientSlider.SetFromColor(characterData.eyesColor);
		skinColorGradientSlider.SetFromColor(characterData.color);
		bodyValues.ChangeStrengthSlider(characterData.strength);
		bodyValues.ChangeFatnessSlider(characterData.fatness);
		appearanceSetter.SetAppearance(characterData.Copy());
		foreach (AppearanceElementData element in appearanceSetter.data.elements)
		{
			List<AppearanceElementVariant> elementVariants = appearanceSetter.GetElementVariants(element.type, tags);
			if (elementVariants != null && elementVariants.Count != 0 && elementVariants.FirstOrDefault((AppearanceElementVariant x) => x.id == element.variantId) is AppearanceBlendshapeVariant variant && appearanceSetter.data.blendshapes != null)
			{
				appearanceSetter.Blendshapes.HandleAppearanceBlendshapeVariant(variant);
			}
		}
		optionsPanelScript.ResetToCurrentCharacter();
		_resetting = false;
		OnSelectionChange();
		if (!bodyPanel.activeSelf)
		{
			ShowCurrentElement();
		}
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
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.ZoomTo(currentElementType);
		base.ShowCurrentElement();
		colorPickerScrollRect.SetActive(appearanceColorPicker.gameObject.activeSelf);
	}

	private void ShowBodyPanel()
	{
		bodyPanel.SetActive(value: true);
		gridSelectionPanel.SetActive(value: false);
		optionsPanel.SetActive(value: false);
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.ResetZoom();
		appearanceColorPicker.Hide();
		optionsPanelScript.Show(show: false, currentElementType);
		colorPickerScrollRect.SetActive(value: false);
	}

	private void ToggleSlidersPanel()
	{
		optionsPanelScript.Show(!optionsPanelScript.isVisible, currentElementType);
	}

	private void ClosePanels()
	{
		elementPicker.gameObject.SetActive(value: false);
		bodyPanel.SetActive(value: false);
		optionsPanelScript.Show(show: false, currentElementType);
		toggleSlidersButton.gameObject.SetActive(value: false);
	}

	private void Randomize()
	{
		if (!bodyPanel.activeSelf)
		{
			RandomizeCurrentElement();
			return;
		}
		eyesColorGradientSlider.RandomizeColor();
		skinColorGradientSlider.RandomizeColor();
		bodyValues.ChangeStrengthSlider(UnityEngine.Random.value);
		bodyValues.ChangeFatnessSlider(UnityEngine.Random.value);
	}

	private void OnSelectionChange()
	{
		if (!_resetting)
		{
			applyButton.interactable = HasEyeColorChanged() || HasSkinColorChanged() || HaveBodyValuesChanged() || HasVariantChanged(AppearanceElementType.Eyes) || HasVariantChanged(AppearanceElementType.Mouth) || HasVariantChanged(AppearanceElementType.Nose) || HasVariantChanged(AppearanceElementType.Head) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Eyes) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Mouth) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Nose);
			revertButton.interactable = applyButton.interactable;
			float playerPrice = _playerPrice;
			_playerPrice = CalculatePrice();
			if (!_idPriceInitialized || !Mathf.Approximately(playerPrice, _playerPrice))
			{
				_idPriceInitialized = true;
				applyButtonLabel.SetData("dialog_doctor_proceed".Localize(new
				{
					price = _playerPrice.ToShortCurrencyFormat()
				}));
			}
		}
	}

	private float CalculatePrice()
	{
		float num = 0f;
		if (HasEyeColorChanged())
		{
			num += pricing.eyeColorPrice;
		}
		if (HasSkinColorChanged())
		{
			num += pricing.skinColorPrice;
		}
		if (HaveBodyValuesChanged())
		{
			num += pricing.bodyValuesPrice;
		}
		if (HasVariantChanged(AppearanceElementType.Eyes) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Eyes))
		{
			num += pricing.eyesVariantPrice;
		}
		if (HasVariantChanged(AppearanceElementType.Mouth) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Mouth))
		{
			num += pricing.mouthVariantPrice;
		}
		if (HasVariantChanged(AppearanceElementType.Nose) || optionsPanelScript.HaveBlendshapesChanged(AppearanceElementType.Nose))
		{
			num += pricing.noseVariantPrice;
		}
		if (HasVariantChanged(AppearanceElementType.Head))
		{
			num += pricing.headVariantPrice;
		}
		return num;
	}

	private void BeginSurgery()
	{
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_doctorappointment");
		if (GameManager.ChangeMoneySafe(0f - _playerPrice, transactionInfo, null, null, force: false, showNotification: true))
		{
			applyButton.interactable = false;
			Hide();
			GameAnalytics.TrackPlasticSurgery();
			InstanceBehavior<GameManager>.Instance.StartCoroutine(PerformOperation());
		}
	}

	private IEnumerator PerformOperation()
	{
		EnergyHelper.goingToHospital = true;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.DoctorOperation);
		yield return UiFader.Fade();
		try
		{
			ApplyChanges();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		InstanceBehavior<GameManager>.Instance.playerController.Character.ResetAnimator();
		InstanceBehavior<GameManager>.Instance.indoorCamera.GetComponent<PedestrianCam>().angle = -15f;
		InstanceBehavior<GameManager>.Instance.pedestrianCamera.GetComponent<PedestrianCam>().angle = -15f;
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.DoctorOperation);
		InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddHours(2);
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "hospital_operation_info");
		yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
		yield return UiFader.UnFade();
		EnergyHelper.goingToHospital = false;
	}

	private void ApplyChanges()
	{
		CharacterData characterData = SaveGameManager.Current.charactersData.First();
		CharacterData data = appearanceSetter.data;
		characterData.color = data.color;
		characterData.eyesColor = data.eyesColor;
		characterData.strength = data.strength;
		characterData.fatness = data.fatness;
		characterData.elements = data.elements.Copy();
		characterData.blendshapes = data.blendshapes.Copy();
		InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.SetAppearance(characterData);
		PortraitGenerator.Create(characterData, null, InstanceBehavior<UIs>.Instance.topBar.avatar);
	}

	public void Hide(bool confirmed = false)
	{
		if (applyButton.interactable && !confirmed)
		{
			HudConfirm.Show(null, "change_character_hair_not_paid_for_warning", delegate
			{
				Hide(confirmed: true);
			});
			return;
		}
		EmployeeUniformPreview employeeUniformPreview = InstanceBehavior<GameManager>.Instance.employeeUniformPreview;
		employeeUniformPreview.characterZoom.hoverTarget = _previousHoverTarget;
		employeeUniformPreview.Hide();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PlasticSurgeryUI);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		PedestrianCam.blockCameraZoom = false;
		base.gameObject.SetActive(value: false);
	}
}
