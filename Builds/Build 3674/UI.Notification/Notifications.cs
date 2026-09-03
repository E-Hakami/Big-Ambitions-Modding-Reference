using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Notification;

public static class Notifications
{
	public static Action<NotificationType, string, Dictionary<string, string>, float, string, Action, bool, bool> onShow;

	public static void ShowError(string headerKey, string duplicateIdentifier = null, bool trackOnSaveGame = true)
	{
		Show(NotificationType.Error, headerKey, null, 4f, duplicateIdentifier, null, notificationSound: true, trackOnSaveGame);
	}

	public static void ShowInsufficientEnergy()
	{
		Show(NotificationType.Error, "notificationui_notification_insufficient_energy");
	}

	public static void ShowInsufficientMoney()
	{
		Show(NotificationType.Error, "notificationui_notification_insufficient_money");
	}

	public static void Show(NotificationType notificationType, string headerKey, Dictionary<string, string> notificationData = null, float secondsToShow = 4f, string duplicateIdentifier = null, Action onClickAction = null, bool notificationSound = true, bool trackOnSaveGame = true)
	{
		if (onShow == null)
		{
			Debug.LogWarning("No Notifications found");
		}
		else
		{
			onShow(notificationType, headerKey, notificationData, secondsToShow, duplicateIdentifier, onClickAction, notificationSound, trackOnSaveGame);
		}
	}
}
