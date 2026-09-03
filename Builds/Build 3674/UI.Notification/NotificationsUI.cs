using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using Localizor;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Notification;

public class NotificationsUI : MonoBehaviour
{
	public Transform container;

	public CanvasGroup successTemplate;

	public CanvasGroup errorTemplate;

	public CanvasGroup infoTemplate;

	public CanvasGroup warningTemplate;

	[SerializeField]
	private bool hideTimestamp;

	private void Start()
	{
		successTemplate.alpha = 0f;
		errorTemplate.alpha = 0f;
		container.gameObject.SetActive(value: true);
		successTemplate.gameObject.SetActive(value: false);
		errorTemplate.gameObject.SetActive(value: false);
		infoTemplate.gameObject.SetActive(value: false);
		warningTemplate.gameObject.SetActive(value: false);
		Notifications.onShow = (Action<NotificationType, string, Dictionary<string, string>, float, string, Action, bool, bool>)Delegate.Combine(Notifications.onShow, new Action<NotificationType, string, Dictionary<string, string>, float, string, Action, bool, bool>(ShowNotification));
	}

	private void OnDestroy()
	{
		Notifications.onShow = (Action<NotificationType, string, Dictionary<string, string>, float, string, Action, bool, bool>)Delegate.Remove(Notifications.onShow, new Action<NotificationType, string, Dictionary<string, string>, float, string, Action, bool, bool>(ShowNotification));
	}

	private void ShowNotification(NotificationType notificationType, string headerKey, Dictionary<string, string> notificationData, float secondsToShow, string duplicateIdentifier, Action onClickAction, bool notificationSound, bool trackOnSaveGame)
	{
		CanvasGroup canvasGroup = notificationType switch
		{
			NotificationType.Warning => UnityEngine.Object.Instantiate(warningTemplate, container), 
			NotificationType.Info => UnityEngine.Object.Instantiate(infoTemplate, container), 
			NotificationType.Error => UnityEngine.Object.Instantiate(errorTemplate, container), 
			_ => UnityEngine.Object.Instantiate(successTemplate, container), 
		};
		Notification notification = new Notification
		{
			type = notificationType,
			key = headerKey,
			notificationData = notificationData,
			date = TimeHelper.Now()
		};
		canvasGroup.transform.GetLanguageChangeEventByName("Text").SetData(notification.key.Localize(notification.notificationData));
		if (!hideTimestamp && SaveGameManager.Current != null)
		{
			canvasGroup.transform.GetLanguageChangeEventByName("Text/Timestamp").SetData("timestamp_full".Localize(new
			{
				day = SaveGameManager.Current.Day,
				time = SaveGameManager.Current.Hour.GetFormattedTime(SaveGameManager.Current.Minute)
			}));
		}
		PlayNotification(canvasGroup, duplicateIdentifier, secondsToShow, notificationType, onClickAction, notification, trackOnSaveGame, notificationSound);
	}

	private void PlayNotificationSound(NotificationType type)
	{
		switch (type)
		{
		case NotificationType.Success:
			UiSoundHelper.Play(UiSound.NotificationSuccess);
			break;
		case NotificationType.Warning:
			UiSoundHelper.Play(UiSound.NotificationWarning);
			break;
		case NotificationType.Error:
			UiSoundHelper.Play(UiSound.NotificationError);
			break;
		case NotificationType.Info:
			UiSoundHelper.Play(UiSound.NotificationInfo);
			break;
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
	}

	private void PlayNotification(CanvasGroup entry, string duplicateIdentifier, float secondsToShow, NotificationType notificationType, Action onClickAction, Notification notification, bool trackOnSaveGame, bool notificationSound = true)
	{
		if (duplicateIdentifier != null)
		{
			if ((bool)entry.transform.parent.Find(duplicateIdentifier))
			{
				return;
			}
			entry.name = duplicateIdentifier;
		}
		if (trackOnSaveGame && SaveGameManager.Current != null)
		{
			SaveGameManager.Current.notifications.Enqueue(notification);
		}
		if (notificationSound)
		{
			PlayNotificationSound(notificationType);
		}
		entry.gameObject.SetActive(value: true);
		Coroutine coroutine = StartCoroutine(NotificationCoroutine(entry, secondsToShow));
		entry.GetComponent<Button>().onClick.AddListener(delegate
		{
			onClickAction?.Invoke();
			StopCoroutine(coroutine);
			StartCoroutine(DestroyNotificationCoroutine(entry, 0.1f));
		});
	}

	private IEnumerator NotificationCoroutine(CanvasGroup entry, float secondsToShow)
	{
		yield return entry.DOFade(1f, 0.5f).SetUpdate(isIndependentUpdate: true).SetLink(entry.gameObject)
			.WaitForCompletion();
		yield return new WaitForSecondsRealtime(secondsToShow);
		yield return DestroyNotificationCoroutine(entry);
	}

	private IEnumerator DestroyNotificationCoroutine(CanvasGroup entry, float fadeOutTime = 0.5f)
	{
		if (!(entry == null))
		{
			TweenerCore<float, float, FloatOptions> tweenerCore = entry.DOFade(0f, fadeOutTime).SetLink(entry.gameObject).SetUpdate(isIndependentUpdate: true);
			yield return tweenerCore.onComplete = (TweenCallback)Delegate.Combine(tweenerCore.onComplete, (TweenCallback)delegate
			{
				UnityEngine.Object.Destroy(entry.gameObject);
			});
		}
	}
}
