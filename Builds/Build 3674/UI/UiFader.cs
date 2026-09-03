using System.Collections;
using DG.Tweening;
using Localizor;
using UnityEngine;

namespace UI;

public static class UiFader
{
	public static bool isFading;

	public static IEnumerator Fade(float duration = 0.4f, string infoKey = null)
	{
		isFading = true;
		yield return InstanceBehavior<UIs>.Instance.blackOverlay.DOFade(1f, duration).SetUpdate(isIndependentUpdate: true).WaitForCompletion();
		if (!string.IsNullOrEmpty(infoKey))
		{
			InstanceBehavior<UIs>.Instance.blackOverlayLabel.SetData(infoKey.Localize());
			InstanceBehavior<UIs>.Instance.blackOverlayLabel.gameObject.SetActive(value: true);
		}
	}

	public static IEnumerator UnFade(float duration = 0.4f)
	{
		InstanceBehavior<UIs>.Instance.blackOverlayLabel?.gameObject.SetActive(value: false);
		yield return InstanceBehavior<UIs>.Instance.blackOverlay.DOFade(0f, duration).SetUpdate(isIndependentUpdate: true).WaitForCompletion();
		isFading = false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isFading = false;
	}
}
