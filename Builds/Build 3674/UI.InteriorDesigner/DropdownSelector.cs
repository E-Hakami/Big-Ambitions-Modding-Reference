using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.InteriorDesigner;

public static class DropdownSelector
{
	public static bool isOpen;

	public static Action<List<string>, Action<string>, string, string> onShow;

	public static void Show(List<string> localizedOptions, Action<string> onConfirm, string headerKey = "dropdown_selector_header", string bodyKey = null)
	{
		if (onShow == null)
		{
			onConfirm?.Invoke(string.Empty);
			Debug.LogWarning("No DropdownSelector found. onConfirmAction will be performed");
		}
		else
		{
			onShow(localizedOptions, onConfirm, headerKey, bodyKey);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isOpen = false;
		onShow = null;
	}
}
