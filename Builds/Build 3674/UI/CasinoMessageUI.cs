using System.Collections;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI;

public class CasinoMessageUI : MonoBehaviour
{
	public enum CasinoMessage
	{
		casino_message_welcome,
		casino_message_trip_over
	}

	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private Coroutine _hideMessageCoroutine;

	public void ShowMessage(CasinoMessage message, float time)
	{
		if (base.gameObject.activeInHierarchy)
		{
			if (_hideMessageCoroutine != null)
			{
				StopCoroutine(_hideMessageCoroutine);
			}
			label.Key = message.ToStringFast();
			canvasGroup.DOFade(1f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
			_hideMessageCoroutine = StartCoroutine(HideMessage(time));
		}
	}

	private IEnumerator HideMessage(float time)
	{
		yield return new WaitForSeconds(time);
		canvasGroup.DOFade(0f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
	}

	private void OnDisable()
	{
		canvasGroup.alpha = 0f;
	}
}
