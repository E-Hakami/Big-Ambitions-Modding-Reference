using System;
using System.Collections;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using CameraControllers;
using Character.Customization;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCharacterClothesUI : CharacterCustomizer
{
	[SerializeField]
	private MenuVertical.Category[] regularClothesCategory;

	[SerializeField]
	private MenuVertical.Category[] uniformClothesCategory;

	[SerializeField]
	private Button saveClothesChangesButton;

	[SerializeField]
	private Vector3 cameraPosition;

	[SerializeField]
	private UiHoverTarget hoverTarget;

	private UiHoverTarget _previousHoverTarget;

	private void Awake()
	{
		onAppearanceChange = (Action)Delegate.Combine(onAppearanceChange, (Action)delegate
		{
			saveClothesChangesButton.interactable = true;
		});
	}

	public void Show(AppearanceTag[] tagsToShow)
	{
		if (InstanceBehavior<GameManager>.Instance.employeeUniformPreview == null)
		{
			Debug.LogError("No employee uniform preview available");
			return;
		}
		tags = tagsToShow;
		if (tags.Contains(AppearanceTag.Uniform))
		{
			menu.categories = uniformClothesCategory;
		}
		else
		{
			menu.categories = regularClothesCategory;
		}
		menu.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.CharacterClothesUI);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		saveClothesChangesButton.interactable = false;
		PedestrianCam.blockCameraZoom = true;
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.SetCameraPosition(cameraPosition);
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Show();
		_previousHoverTarget = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.hoverTarget;
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.hoverTarget = hoverTarget;
		if ((object)appearanceSetter == null)
		{
			appearanceSetter = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.appearanceSetter;
		}
		appearanceSetter.SetAppearance(SaveGameManager.Current.charactersData.First().Copy());
		Show(AppearanceElementType.Torso);
		CoroutineUtility.Run(ShowAfterInitialization());
	}

	private IEnumerator ShowAfterInitialization()
	{
		yield return new WaitForEndOfFrame();
		base.gameObject.SetActive(value: true);
		yield return null;
		menu.OnCategoryButtonClick(menu.categories[0]);
	}

	public void SaveClothes()
	{
		SaveGameManager.Current.charactersData[0].elements = appearanceSetter.data.elements.Copy();
		InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.SetAppearance(SaveGameManager.Current.charactersData.First());
		PortraitGenerator.Create(SaveGameManager.Current.charactersData.First(), null, InstanceBehavior<UIs>.Instance.topBar.avatar);
		saveClothesChangesButton.interactable = false;
		Hide();
	}

	public void Hide()
	{
		if (HasUnsavedChanges())
		{
			LanguageChangeEventDataHolder bodyData = "change_character_clothes_unsaved_changes_warning".Localize();
			Action onConfirmAction = CloseChangeCharacterClothesUI;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			CloseChangeCharacterClothesUI();
		}
	}

	private void CloseChangeCharacterClothesUI()
	{
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.characterZoom.hoverTarget = _previousHoverTarget;
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Hide();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.CharacterClothesUI);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		PedestrianCam.blockCameraZoom = false;
		base.gameObject.SetActive(value: false);
	}

	private bool HasUnsavedChanges()
	{
		return saveClothesChangesButton.interactable;
	}

	protected override bool ShouldShowTags()
	{
		return false;
	}
}
