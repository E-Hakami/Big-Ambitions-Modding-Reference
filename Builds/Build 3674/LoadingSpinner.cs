using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSpinner : InstanceBehavior<LoadingSpinner>
{
	private const float FadeInDuration = 1f;

	private const float FadeOutDuration = 0.5f;

	public static bool isLoading;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private RectTransform canvasRectTransform;

	[SerializeField]
	private Animator spinnerAnimator;

	private Tween _targetTrackingTween;

	private RectTransform _trackedTarget;

	private bool _updateUntilHidden;

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private void Start()
	{
		ChangeCanvasAlpha(0f, 0f);
	}

	public static void Show(RectTransform target = null)
	{
		InstanceBehavior<LoadingSpinner>.Instance.ShowSpinner(target);
	}

	public static void Show(RectTransform target, float updateDuration)
	{
		InstanceBehavior<LoadingSpinner>.Instance.ShowSpinner(target, updateDuration);
	}

	public static void Hide()
	{
		InstanceBehavior<LoadingSpinner>.Instance.HideSpinner();
	}

	private void LateUpdate()
	{
		if (_updateUntilHidden && _trackedTarget != null)
		{
			MatchTarget(_trackedTarget);
		}
	}

	private void ShowSpinner(RectTransform target)
	{
		isLoading = true;
		canvasGroup.blocksRaycasts = true;
		StopTargetTracking();
		if (target != null)
		{
			MatchTarget(target);
		}
		else
		{
			ResetToFullScreen();
		}
		canvasGroup.DOKill();
		if (canvasGroup.alpha < 1f)
		{
			ChangeCanvasAlpha(1f, 1f);
		}
	}

	private void ShowSpinner(RectTransform target, float updateDuration)
	{
		isLoading = true;
		canvasGroup.blocksRaycasts = true;
		StopTargetTracking();
		if (target != null)
		{
			MatchTarget(target);
			if (updateDuration < 0f)
			{
				_trackedTarget = target;
				_updateUntilHidden = true;
			}
			else if (updateDuration > 0f)
			{
				_trackedTarget = target;
				_targetTrackingTween = DOVirtual.Float(0f, 1f, updateDuration, delegate
				{
					if (_trackedTarget != null)
					{
						MatchTarget(_trackedTarget);
					}
				}).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
					.OnComplete(delegate
					{
						if (_trackedTarget != null)
						{
							MatchTarget(_trackedTarget);
						}
						_targetTrackingTween = null;
						_trackedTarget = null;
					});
			}
		}
		else
		{
			ResetToFullScreen();
		}
		canvasGroup.DOKill();
		if (canvasGroup.alpha < 1f)
		{
			ChangeCanvasAlpha(1f, 1f);
		}
	}

	private void HideSpinner(bool fade = true)
	{
		StopTargetTracking();
		canvasGroup.DOKill();
		if (canvasGroup.alpha > 0f)
		{
			ChangeCanvasAlpha(0f, fade ? 0.5f : 0f);
		}
		isLoading = false;
		canvasGroup.blocksRaycasts = false;
	}

	private void StopTargetTracking()
	{
		_targetTrackingTween?.Kill();
		_targetTrackingTween = null;
		_trackedTarget = null;
		_updateUntilHidden = false;
	}

	private void MatchTarget(RectTransform target)
	{
		Vector3[] array = new Vector3[4];
		target.GetWorldCorners(array);
		Vector2[] array2 = new Vector2[4];
		for (int i = 0; i < 4; i++)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, RectTransformUtility.WorldToScreenPoint(null, array[i]), null, out array2[i]);
		}
		Rect rect = canvasRectTransform.rect;
		float x = array2[0].x - rect.xMin;
		float num = rect.xMax - array2[2].x;
		float num2 = rect.yMax - array2[1].y;
		float y = array2[0].y - rect.yMin;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = new Vector2(x, y);
		rectTransform.offsetMax = new Vector2(0f - num, 0f - num2);
		LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
	}

	private void ResetToFullScreen()
	{
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
	}

	private void ChangeCanvasAlpha(float alphaTarget, float maxDuration)
	{
		float duration = Mathf.Abs(canvasGroup.alpha - alphaTarget) * maxDuration;
		canvasGroup.DOFade(alphaTarget, duration).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
			.OnComplete(delegate
			{
				spinnerAnimator.enabled = alphaTarget > 0.5f;
			});
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isLoading = false;
	}
}
