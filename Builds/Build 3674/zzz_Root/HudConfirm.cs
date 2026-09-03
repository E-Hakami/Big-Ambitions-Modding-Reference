using System;
using BigAmbitions.InputSystem;
using Localizor.LanguageChangeEvent;
using UnityEngine;

public static class HudConfirm
{
	public static bool isOpen;

	public static Action onClose;

	public static Action onConfirm;

	public static Action<LanguageChangeEventDataHolder, LanguageChangeEventDataHolder, string, string, Action, Action> onShow;

	public static void Show(LanguageChangeEventDataHolder headerData = default(LanguageChangeEventDataHolder), LanguageChangeEventDataHolder bodyData = default(LanguageChangeEventDataHolder), Action onConfirmAction = null, Action onCancelAction = null, string confirmKey = null, string cancelKey = null, bool allowConfirmationSkip = true)
	{
		if (isOpen)
		{
			Debug.LogWarning("HudConfirm is already open");
		}
		else if (allowConfirmationSkip && PlayerAction.PerformActionWithoutConfirm.Pressing())
		{
			onConfirmAction?.Invoke();
		}
		else if (onShow == null)
		{
			Debug.LogWarning("No HudConfirm found. onConfirmAction will be performed");
			onConfirmAction?.Invoke();
		}
		else
		{
			onShow(headerData, bodyData, confirmKey, cancelKey, onConfirmAction, onCancelAction);
		}
	}

	public static void Show(string headerKey = null, string bodyKey = null, Action onConfirmAction = null, Action onCancelAction = null, string confirmKey = null, string cancelKey = null, bool allowConfirmationSkip = true)
	{
		if (isOpen)
		{
			Debug.LogWarning("HudConfirm is already open");
			return;
		}
		LanguageChangeEventDataHolder headerData = (string.IsNullOrEmpty(headerKey) ? default(LanguageChangeEventDataHolder) : new LanguageChangeEventDataHolder
		{
			Key = headerKey
		});
		LanguageChangeEventDataHolder bodyData = (string.IsNullOrEmpty(bodyKey) ? default(LanguageChangeEventDataHolder) : new LanguageChangeEventDataHolder
		{
			Key = bodyKey
		});
		Show(headerData, bodyData, onConfirmAction, onCancelAction, confirmKey, cancelKey, allowConfirmationSkip);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isOpen = false;
		onClose = null;
		onConfirm = null;
		onShow = null;
	}
}
