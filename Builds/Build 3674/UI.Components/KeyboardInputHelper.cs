using System;
using JimmysUnityUtilities;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Components;

public static class KeyboardInputHelper
{
	public static void Configure(TMP_InputField inputField, Action onSubmit = null, bool autoFocus = true, bool refocusAfterSubmit = true)
	{
		if ((bool)inputField)
		{
			if (!inputField.TryGetComponent<KeyboardInputHandler>(out var component))
			{
				component = inputField.gameObject.AddComponent<KeyboardInputHandler>();
			}
			component.Configure(onSubmit, autoFocus, refocusAfterSubmit);
		}
	}

	public static void FocusNextFrame(TMP_InputField inputField)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)inputField && inputField.isActiveAndEnabled && inputField.interactable)
			{
				inputField.Select();
				inputField.ActivateInputField();
			}
		});
	}

	public static void SelectNextFrame(Selectable selectable)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)selectable && selectable.isActiveAndEnabled && selectable.interactable)
			{
				EventSystem.current?.SetSelectedGameObject(selectable.gameObject);
			}
		});
	}
}
