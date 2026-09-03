using DG.Tweening;
using Extensions;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Elements;

public class ProgressBar : MonoBehaviour
{
	public Slider slider;

	public TextMeshProUGUI label;

	public Image sliderFillImage;

	public bool autoSetColors = true;

	[MinMaxSlider(0f, 100f)]
	[ShowIf("autoSetColors")]
	public Vector2 redRange = new Vector2(0f, 20f);

	[MinMaxSlider(0f, 100f)]
	[ShowIf("autoSetColors")]
	public Vector2 yellowRange = new Vector2(20f, 60f);

	[MinMaxSlider(0f, 100f)]
	[ShowIf("autoSetColors")]
	public Vector2 greenRange = new Vector2(60f, 100f);

	[SerializeField]
	private bool scrollBackground;

	[SerializeField]
	[ShowIf("scrollBackground")]
	public Image scrollerImage;

	[SerializeField]
	[ShowIf("scrollBackground")]
	private float scrollerSpeed = -24f;

	private void Update()
	{
		if (scrollBackground && (bool)scrollerImage && (bool)scrollerImage.sprite)
		{
			float num = scrollerImage.sprite.textureRect.width / scrollerImage.pixelsPerUnitMultiplier;
			float x = 0f - num + scrollerSpeed * Time.unscaledTime % num;
			scrollerImage.rectTransform.offsetMin = new Vector2(x, 0f);
		}
	}

	public void SetValue(float valueInPercent, bool showAnimation = false)
	{
		valueInPercent = Mathf.Clamp(valueInPercent, 0f, 100f);
		label.text = Mathf.RoundToInt(valueInPercent) + "%";
		if (showAnimation)
		{
			slider.DOValue(valueInPercent / 100f, 0.3f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		}
		else
		{
			slider.value = valueInPercent / 100f;
		}
		if (autoSetColors)
		{
			Color32 color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			if (valueInPercent.InRange(yellowRange.x, yellowRange.y))
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
			}
			if (valueInPercent.InRange(greenRange.x, greenRange.y))
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
			}
			if (valueInPercent.InRange(redRange.x, redRange.y))
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			}
			sliderFillImage.color = color;
			label.color = color;
		}
	}

	public void SetValue01(float value, bool showAnimation = false)
	{
		SetValue(value * 100f, showAnimation);
	}
}
