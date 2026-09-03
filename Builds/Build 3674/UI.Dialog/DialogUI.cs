using System;
using CameraControllers;
using Dialogs;
using Entities;
using JimmysUnityUtilities;
using UI.ItemPanel;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class DialogUI : DialogController
{
	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private Image[] imagesToHide;

	[HideInInspector]
	public bool isPanelOpen;

	[HideInInspector]
	public ThirdPersonCharacter npcInDialog;

	private NavigationBlocker _navigationBlocker;

	private Action _onStopDialog;

	private bool _reopenItemPanelAfterDialog;

	private void Start()
	{
		panel.SetActive(value: false);
		inputTemplate.gameObject.SetActive(value: false);
		textTemplate.gameObject.SetActive(value: false);
		inputTemplate.alpha = 0f;
		textTemplate.alpha = 0f;
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (isPanelOpen)
			{
				ChangeVisibility(!isOpen);
			}
		});
	}

	public void ShowDialog(CallDialogType callDialogType, NavigationBlocker navigationBlocker, Contact dialogContact = null, Action onStopDialog = null, ThirdPersonCharacter npc = null)
	{
		DialogController.current = this;
		contact = dialogContact;
		_onStopDialog = onStopDialog;
		npcInDialog = npc;
		ChangeVisibility(visible: true);
		PedestrianCam.blockCameraZoom = true;
		isPanelOpen = true;
		dialog = CallDialogFactory.GetDialog(callDialogType);
		_navigationBlocker = navigationBlocker;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(_navigationBlocker);
		InstanceBehavior<GameManager>.Instance.playerController.Character.HideHandContent();
		if (ItemPanelUI.IsVisible)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
			_reopenItemPanelAfterDialog = true;
		}
	}

	public void OnConfirm()
	{
		ConfirmCurrentEntry();
	}

	public void OnSecondOption()
	{
		SecondOptionCurrentEntry();
	}

	public override void FinishDialog()
	{
		StopDialog();
	}

	public override void CancelDialog()
	{
		base.CancelDialog();
		StopDialog();
	}

	private void StopDialog()
	{
		ResetEntriesComponents();
		DialogController.current = null;
		dialog = null;
		contact = null;
		npcInDialog = null;
		ChangeVisibility(visible: false);
		PedestrianCam.blockCameraZoom = false;
		isPanelOpen = false;
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(_navigationBlocker);
		_onStopDialog?.Invoke();
		_onStopDialog = null;
		InstanceBehavior<GameManager>.Instance.playerController.Character.ShowHandContent();
		if (_reopenItemPanelAfterDialog)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
			_reopenItemPanelAfterDialog = false;
		}
	}

	private void ChangeVisibility(bool visible)
	{
		panel.SetActive(visible);
	}

	public void HidePanel(bool hide)
	{
		Image[] array = imagesToHide;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetAlpha((!hide) ? 1 : 0);
		}
	}
}
