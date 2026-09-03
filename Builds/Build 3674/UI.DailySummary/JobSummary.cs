using System.Collections;
using DG.Tweening;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.DailySummary;

public abstract class JobSummary : MonoBehaviour
{
	private const float FadeInDuration = 0.5f;

	private const string TipsKey = "delivery_job_tips";

	private const string TipsFastDeliveryKey = "delivery_job_tips_fast_delivery";

	[SerializeField]
	private TextLocalizationComponent tipsCaptionLabel;

	[SerializeField]
	private TextMeshProUGUI tipsLabel;

	[SerializeField]
	private CanvasGroup[] canvasGroups;

	[SerializeField]
	private CanvasGroup tipsCanvasGroup;

	private void Awake()
	{
		CanvasGroup[] array = canvasGroups;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].alpha = 0f;
		}
	}

	protected void SetTipsRow(float tips, bool wasFastDelivery)
	{
		tipsCanvasGroup.gameObject.SetActive(tips > 0f);
		if (!(tips <= 0f))
		{
			string key = (wasFastDelivery ? "delivery_job_tips_fast_delivery" : "delivery_job_tips");
			tipsCaptionLabel.SetData(key.Localize());
			tipsLabel.text = tips.ToCurrencyFormat();
		}
	}

	protected IEnumerator FadeInRows()
	{
		CanvasGroup[] array = canvasGroups;
		foreach (CanvasGroup canvasGroup in array)
		{
			if (canvasGroup.gameObject.activeSelf)
			{
				yield return canvasGroup.DOFade(1f, 0.5f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true)
					.WaitForCompletion();
			}
		}
	}

	public void OnClickClose()
	{
		Object.Destroy(base.gameObject);
	}
}
