using System;
using System.Collections;
using DG.Tweening;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Characters.EmojiSystem;

public class CharacterEmojiExpression : MonoBehaviour
{
	private const float EmojiYOffset = 0.6f;

	private const float FadeInDuration = 0.3f;

	private const float FadeOutDuration = 0.3f;

	public Transform inWorldTarget;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image icon;

	[SerializeField]
	private Image modifier;

	[SerializeField]
	private TextLocalizationComponent textLabel;

	private Vector3 _smoothVelocity;

	private readonly WaitForSeconds _fadeOutWaitForSeconds = new WaitForSeconds(0.3f);

	private readonly WaitForSecondsRealtime _fadeOutWaitForSecondsRealtime = new WaitForSecondsRealtime(0.3f);

	private Vector3 GetPositionInWorld()
	{
		return GameManager.GetMainCamera().WorldToScreenPoint(inWorldTarget.position) + Vector3.up * 0.6f;
	}

	private void Awake()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}

	private void OnCityMapToggle(bool toggle)
	{
		if (toggle && base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
		}
		else if (!toggle && inWorldTarget != null)
		{
			base.gameObject.SetActive(value: true);
		}
	}

	private void LateUpdate()
	{
		if (inWorldTarget == null || !inWorldTarget.gameObject.activeInHierarchy)
		{
			Release();
		}
		if (base.gameObject.activeSelf)
		{
			SetPosition();
		}
	}

	private void OnDestroy()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}

	private void SetPosition()
	{
		base.transform.position = GetPositionInWorld();
	}

	public void SetTarget(Transform target)
	{
		inWorldTarget = target;
	}

	public void SetImages(Sprite backgroundSprite, Sprite iconSprite, Sprite modifierSprite)
	{
		background.sprite = backgroundSprite;
		icon.sprite = iconSprite;
		SetModifierImage(modifierSprite);
	}

	private void SetModifierImage(Sprite modifierSprite)
	{
		if (modifierSprite != null)
		{
			modifier.sprite = modifierSprite;
			modifier.SetAlpha(1f);
		}
		else
		{
			modifier.SetAlpha(0f);
		}
	}

	public IEnumerator Show(float secondsToShow, bool useUnscaledTime)
	{
		ResetVisuals();
		SetPosition();
		FadeScale(1f, 0.3f, Ease.OutBack);
		yield return WaitForEmojiDuration(secondsToShow, useUnscaledTime);
		if ((bool)this)
		{
			FadeScale(1.2f, 0.3f, Ease.Linear);
			FadeOutCanvasAlpha();
			yield return WaitForFadeOut(useUnscaledTime);
		}
	}

	private IEnumerator WaitForFadeOut(bool useUnscaledTime)
	{
		if (useUnscaledTime)
		{
			yield return _fadeOutWaitForSecondsRealtime;
		}
		else
		{
			yield return _fadeOutWaitForSeconds;
		}
	}

	private static IEnumerator WaitForEmojiDuration(float secondsToShow, bool useUnscaledTime)
	{
		if (useUnscaledTime)
		{
			yield return new WaitForSecondsRealtime(secondsToShow);
		}
		else
		{
			yield return new WaitForSeconds(secondsToShow);
		}
	}

	private void ResetVisuals()
	{
		base.transform.localScale = Vector3.zero;
		canvasGroup.alpha = 1f;
	}

	public void SetText(string localizationKey, object localizationArgs)
	{
		textLabel.SetData(LanguageChangeEventDataHolder.Create(localizationKey, localizationArgs));
	}

	public void Release()
	{
		if (textLabel != null)
		{
			CharacterEmojiSystem.characterEmojiWithTextPool.Release(this);
		}
		else
		{
			CharacterEmojiSystem.characterEmojisPool.Release(this);
		}
		inWorldTarget = null;
	}

	private void FadeOutCanvasAlpha()
	{
		canvasGroup.DOFade(0f, 0.3f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
	}

	private void FadeScale(float scaleTarget, float duration, Ease ease)
	{
		base.transform.DOScale(scaleTarget, duration).SetUpdate(isIndependentUpdate: true).SetEase(ease)
			.SetLink(base.gameObject);
	}
}
