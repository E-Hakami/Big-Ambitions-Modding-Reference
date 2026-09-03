using System;
using BlurShadersPro.HDRP;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Player;

public class BlurEffect : MonoBehaviour
{
	private const int BlurEndValueToPreventWhiteFlashIssue = 7;

	private static Action OnEnable;

	private static Action OnDisable;

	[SerializeField]
	private Volume blurVolume;

	[SerializeField]
	private int blurStrength = 100;

	[SerializeField]
	[Range(0f, 1f)]
	private float blurFadeDuration = 0.3f;

	private Blur _blurPass;

	private Tweener _blurTween;

	private void Awake()
	{
		blurVolume.enabled = false;
		blurVolume.profile.TryGet<Blur>(out _blurPass);
		OnEnable = (Action)Delegate.Combine(OnEnable, new Action(OnEnableBlur));
		OnDisable = (Action)Delegate.Combine(OnDisable, new Action(OnDisableBlur));
	}

	public static void Enable()
	{
		OnEnable?.Invoke();
	}

	public static void Disable()
	{
		OnDisable?.Invoke();
	}

	private void OnEnableBlur()
	{
		_blurTween.Kill();
		blurVolume.enabled = true;
		_blurTween = DOTween.To(delegate(float x)
		{
			_blurPass.strength.value = (int)x;
		}, _blurPass.strength.value, blurStrength, blurFadeDuration).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
	}

	private void OnDisableBlur()
	{
		_blurTween.Kill();
		_blurTween = DOTween.To(delegate(float x)
		{
			_blurPass.strength.value = (int)x;
		}, _blurPass.strength.value, 7f, blurFadeDuration).OnComplete(delegate
		{
			blurVolume.enabled = false;
		}).SetUpdate(isIndependentUpdate: true)
			.SetLink(base.gameObject);
	}

	private void OnDestroy()
	{
		OnEnable = (Action)Delegate.Remove(OnEnable, new Action(OnEnableBlur));
		OnDisable = (Action)Delegate.Remove(OnDisable, new Action(OnDisableBlur));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		OnEnable = null;
		OnDisable = null;
	}
}
