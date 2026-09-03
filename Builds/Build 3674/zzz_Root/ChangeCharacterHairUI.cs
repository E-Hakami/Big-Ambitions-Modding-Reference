using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.GameAnalytics;
using CameraControllers;
using Character.Customization;
using Controllers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Purchase;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCharacterHairUI : CharacterCustomizer
{
	[SerializeField]
	private Button changeHairButton;

	[SerializeField]
	private TextLocalizationComponent changeHairButtonLabel;

	[SerializeField]
	private Vector3 cameraPosition;

	public Action<Action> onHairChangeRequest;

	public Action<TextLocalizationComponent> onChangeHairButtonUpdate;

	private List<AppearanceElementData> _newCharacterAppearance;

	private HairdresserChairController _attachedHairdresserChair;

	private void Awake()
	{
		onAppearanceChange = (Action)Delegate.Combine(onAppearanceChange, (Action)delegate
		{
			changeHairButton.interactable = HasColorChanged(AppearanceElementType.Hair) || HasVariantChanged(AppearanceElementType.Hair) || HasColorChanged(AppearanceElementType.Beard) || HasColorChanged(AppearanceElementType.Eyebrows) || HasVariantChanged(AppearanceElementType.Eyebrows) || HasVariantChanged(AppearanceElementType.Beard);
			onChangeHairButtonUpdate(changeHairButtonLabel);
		});
	}

	public void Show(HairdresserChairController attachedHairdresserChair)
	{
		menu.Reset();
		_attachedHairdresserChair = attachedHairdresserChair;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.CharacterHairUI);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		changeHairButton.interactable = false;
		PedestrianCam.blockCameraZoom = true;
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.SetCameraPosition(cameraPosition);
		InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Show();
		if ((object)appearanceSetter == null)
		{
			appearanceSetter = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.appearanceSetter;
		}
		appearanceSetter.SetAppearance(SaveGameManager.Current.charactersData.First().Copy());
		Show(AppearanceElementType.Hair);
		onChangeHairButtonUpdate(changeHairButtonLabel);
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			base.gameObject.SetActive(value: true);
		}, 3);
	}

	public void ChangeHair()
	{
		onHairChangeRequest?.Invoke(UpdateCharacterHairWithChanges);
		_newCharacterAppearance = appearanceSetter.data.elements.Copy();
		changeHairButton.interactable = false;
		Hide();
		GameAnalytics.TrackChangeHaircut();
	}

	private void UpdateCharacterHairWithChanges()
	{
		SaveGameManager.Current.charactersData[0].elements = _newCharacterAppearance;
		InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.SetAppearance(SaveGameManager.Current.charactersData.First());
		PortraitGenerator.Create(SaveGameManager.Current.charactersData.First(), null, InstanceBehavior<UIs>.Instance.topBar.avatar);
	}

	public void Hide()
	{
		if (HasUnsavedChanges())
		{
			LanguageChangeEventDataHolder bodyData = "change_character_hair_not_paid_for_warning".Localize();
			Action onConfirmAction = CloseChangeCharacterHairUI;
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
		}
		else
		{
			CloseChangeCharacterHairUI();
		}
		void CloseChangeCharacterHairUI()
		{
			_attachedHairdresserChair?.UnsubscribeToChangeHairEvents();
			_attachedHairdresserChair = null;
			InstanceBehavior<GameManager>.Instance.employeeUniformPreview.Hide();
			InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.CharacterHairUI);
			if (PurchaseUI.IsPanelOpen)
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.PurchaseUI);
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
			PedestrianCam.blockCameraZoom = false;
			base.gameObject.SetActive(value: false);
		}
	}

	private bool HasUnsavedChanges()
	{
		return changeHairButton.interactable;
	}
}
