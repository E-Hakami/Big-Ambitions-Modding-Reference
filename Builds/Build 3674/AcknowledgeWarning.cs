using System;
using UnityEngine;

public static class AcknowledgeWarning
{
	private const string ConfirmKey = "common_agree";

	private const string PlayerPrefsKeyPrefix = "AcknowledgeWarning_";

	public static bool isOpen;

	public static Action<string, string, string, string, Action> onShow;

	public static void Show(string value, Action onConfirmAction, string headerKey, string bodyKey, string confirmKey = "common_agree")
	{
		if (isOpen)
		{
			Debug.LogWarning("AcknowledgeWarning is already open");
		}
		else if (IsAcknowledged(value))
		{
			onConfirmAction?.Invoke();
		}
		else if (onShow == null)
		{
			Debug.LogWarning("No AcknowledgeWarning found. onConfirmAction will be performed");
			onConfirmAction?.Invoke();
		}
		else
		{
			onShow(value, headerKey, bodyKey, confirmKey, onConfirmAction);
		}
	}

	private static bool IsAcknowledged(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return UnityEngine.PlayerPrefs.HasKey("AcknowledgeWarning_" + value);
		}
		return false;
	}

	public static void Acknowledge(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			UnityEngine.PlayerPrefs.SetInt("AcknowledgeWarning_" + value, 1);
			UnityEngine.PlayerPrefs.Save();
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isOpen = false;
		onShow = null;
	}
}
