using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.GameAnalytics;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Notification;

public class NotificationsListUI : MonoBehaviour
{
	[SerializeField]
	private NotificationListScrollerController listScrollerController;

	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private Button removeAllNotificationsButton;

	[SerializeField]
	private GameObject noNotificationsLabel;

	[SerializeField]
	private Sprite successBackground;

	[SerializeField]
	private Sprite warningBackground;

	[SerializeField]
	private Sprite infoBackground;

	[SerializeField]
	private Sprite errorBackground;

	[HideInInspector]
	public bool isVisible;

	private void Start()
	{
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool show)
		{
			if (show && isVisible)
			{
				Toggle(show: false);
			}
		});
	}

	public void Toggle()
	{
		Toggle(!isVisible);
	}

	public void Toggle(bool show)
	{
		if (show)
		{
			GameAnalytics.TrackOpenNotificationPanel();
			noNotificationsLabel.gameObject.SetActive(SaveGameManager.Current.notifications.Count == 0);
			removeAllNotificationsButton.interactable = SaveGameManager.Current.notifications.Count > 0;
			IEnumerable<Notification> notifications = SaveGameManager.Current.notifications.Reverse();
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				listScrollerController.LoadNotifications(notifications);
			});
		}
		panel.SetActive(show);
		isVisible = show;
	}

	public static void CleanOldNotifications()
	{
		while (SaveGameManager.Current.notifications.Count > 0 && SaveGameManager.Current.notifications.Peek().date.Day < SaveGameManager.Current.Day - SaveGameManager.Current.gameVariables.daysPerYear)
		{
			SaveGameManager.Current.notifications.Dequeue();
		}
	}

	public void RemoveAllNotifications()
	{
		LanguageChangeEventDataHolder bodyData = "notifications_remove_all_confirm".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			SaveGameManager.Current.notifications.Clear();
			Toggle(show: true);
		});
	}

	public Sprite GetBackground(NotificationType notificationType)
	{
		return notificationType switch
		{
			NotificationType.Error => errorBackground, 
			NotificationType.Info => infoBackground, 
			NotificationType.Success => successBackground, 
			NotificationType.Warning => warningBackground, 
			_ => infoBackground, 
		};
	}
}
