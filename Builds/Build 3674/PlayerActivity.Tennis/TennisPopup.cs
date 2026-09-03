using System.Collections.Generic;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity.Tennis;

public class TennisPopup : MonoBehaviour
{
	private struct NotificationData
	{
		public Color color;

		public string key;
	}

	[SerializeField]
	private float transitionScale = 1.2f;

	[SerializeField]
	private float transitionDuration = 0.25f;

	[SerializeField]
	private float lingerDuration = 2f;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private TextLocalizationComponent mainText;

	[SerializeField]
	private Image backgroundImage;

	[SerializeField]
	private Color neutralColor;

	[SerializeField]
	private Color positiveColor;

	[SerializeField]
	private Color negativeColor;

	private readonly List<NotificationData> _pendingNotifications = new List<NotificationData>();

	private Sequence _sequence;

	public void ShowPopup(string key)
	{
		ShowPopup(neutralColor, key);
	}

	public void ShowPopup(bool isPositive, string key)
	{
		ShowPopup(isPositive ? positiveColor : negativeColor, key);
	}

	private void ShowPopup(Color color, string key, bool canEnqueue = true)
	{
		if (canEnqueue && base.gameObject.activeSelf)
		{
			_pendingNotifications.Add(new NotificationData
			{
				color = color,
				key = key
			});
			return;
		}
		mainText.Key = key;
		backgroundImage.color = color;
		base.gameObject.SetActive(value: true);
		_sequence?.Kill();
		base.transform.localScale = Vector3.one * transitionScale;
		canvasGroup.alpha = 0f;
		_sequence = DOTween.Sequence();
		_sequence.Append(base.transform.DOScale(Vector3.one, transitionDuration).SetEase(Ease.OutBack));
		_sequence.Join(canvasGroup.DOFade(1f, transitionDuration));
		_sequence.AppendInterval(lingerDuration);
		_sequence.Append(base.transform.DOScale(Vector3.one * transitionScale, transitionDuration));
		_sequence.Join(canvasGroup.DOFade(0f, transitionDuration));
		_sequence.OnComplete(OnCompleted);
		_sequence.SetUpdate(isIndependentUpdate: true);
	}

	private void OnCompleted()
	{
		_sequence = null;
		if (_pendingNotifications.Count == 0)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		NotificationData notificationData = _pendingNotifications[0];
		_pendingNotifications.RemoveAt(0);
		ShowPopup(notificationData.color, notificationData.key, canEnqueue: false);
	}
}
