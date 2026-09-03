using System;
using System.Collections;
using DG.Tweening;
using JimmysUnityUtilities;
using UnityEngine;

namespace UI.Dialog;

public class RouletteUI : MonoBehaviour
{
	[SerializeField]
	private Transform image;

	public AnimationCurve timeSpeedCurve;

	private Func<bool> _isDialogActive;

	public void Spin(float duration, Func<bool> isDialogActive)
	{
		_isDialogActive = isDialogActive;
		CoroutineUtility.Run(SpinAnimation(duration));
	}

	private IEnumerator SpinAnimation(float duration)
	{
		float timeElapsed = 0f;
		while (timeElapsed < duration)
		{
			float num = timeSpeedCurve.Evaluate(timeElapsed / duration) * 50f;
			timeElapsed += 0.1f;
			image.DORotate(image.eulerAngles + Vector3.forward * num, 0.1f).SetLink(base.gameObject);
			yield return new WaitForSeconds(0.1f);
			if (!IsDialogActive())
			{
				break;
			}
		}
	}

	private bool IsDialogActive()
	{
		if (_isDialogActive != null)
		{
			return _isDialogActive();
		}
		return true;
	}
}
