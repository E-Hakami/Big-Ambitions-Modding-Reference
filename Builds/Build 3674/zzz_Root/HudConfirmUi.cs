using System;
using Localizor.LanguageChangeEvent;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.EventSystems;

public class HudConfirmUi : MonoBehaviour
{
	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private RectTransform panel;

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private TextLocalizationComponent bodyLabel;

	[SerializeField]
	private TextLocalizationComponent confirmLabel;

	[SerializeField]
	private TextLocalizationComponent cancelLabel;

	[Header("Conditions")]
	[SerializeField]
	private bool showInFullMenu;

	private Action _onConfirmAction;

	private Action _onCancelAction;

	private void Start()
	{
		container.gameObject.SetActive(value: false);
		HudConfirm.onShow = (Action<LanguageChangeEventDataHolder, LanguageChangeEventDataHolder, string, string, Action, Action>)Delegate.Combine(HudConfirm.onShow, new Action<LanguageChangeEventDataHolder, LanguageChangeEventDataHolder, string, string, Action, Action>(Show));
		HudConfirm.onClose = (Action)Delegate.Combine(HudConfirm.onClose, new Action(ClickCancel));
		HudConfirm.onConfirm = (Action)Delegate.Combine(HudConfirm.onConfirm, new Action(ClickConfirm));
	}

	private bool ShouldShow()
	{
		if (FullMenu.IsOpen)
		{
			return showInFullMenu;
		}
		return !showInFullMenu;
	}

	private void Show(LanguageChangeEventDataHolder headerData, LanguageChangeEventDataHolder bodyData, string confirmKey, string cancelKey, Action onConfirmAction, Action onCancelAction)
	{
		if (ShouldShow())
		{
			if (string.IsNullOrEmpty(headerData.Key))
			{
				headerData.Key = "hud_confirm_are_you_sure";
			}
			if (string.IsNullOrEmpty(confirmKey))
			{
				confirmKey = "common_confirm";
			}
			if (string.IsNullOrEmpty(cancelKey))
			{
				cancelKey = "common_cancel";
			}
			headerLabel.SetData(headerData);
			bodyLabel.SetData(bodyData);
			confirmLabel.Key = confirmKey;
			cancelLabel.Key = cancelKey;
			_onConfirmAction = onConfirmAction;
			_onCancelAction = onCancelAction;
			EventSystem.current?.SetSelectedGameObject(null);
			container.gameObject.SetActive(value: true);
			HudConfirm.isOpen = true;
		}
	}

	public void ClickCancel()
	{
		if (HudConfirm.isOpen && ShouldShow())
		{
			_onCancelAction?.Invoke();
			_onCancelAction = null;
			_onConfirmAction = null;
			container.gameObject.SetActive(value: false);
			HudConfirm.isOpen = false;
		}
	}

	public void ClickConfirm()
	{
		if (HudConfirm.isOpen && ShouldShow())
		{
			container.gameObject.SetActive(value: false);
			HudConfirm.isOpen = false;
			_onConfirmAction?.Invoke();
		}
	}

	private void OnDestroy()
	{
		HudConfirm.isOpen = false;
		HudConfirm.onShow = (Action<LanguageChangeEventDataHolder, LanguageChangeEventDataHolder, string, string, Action, Action>)Delegate.Remove(HudConfirm.onShow, new Action<LanguageChangeEventDataHolder, LanguageChangeEventDataHolder, string, string, Action, Action>(Show));
		HudConfirm.onClose = (Action)Delegate.Remove(HudConfirm.onClose, new Action(ClickCancel));
		HudConfirm.onConfirm = (Action)Delegate.Remove(HudConfirm.onConfirm, new Action(ClickConfirm));
	}
}
