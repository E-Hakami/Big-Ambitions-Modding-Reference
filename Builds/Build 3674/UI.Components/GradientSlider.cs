using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Components;

[RequireComponent(typeof(Slider))]
public class GradientSlider : MonoBehaviour
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private Image background;

	[SerializeField]
	private int width;

	[SerializeField]
	private int height;

	[Header("Gradient settings")]
	[SerializeField]
	private Gradient gradient;

	private void Start()
	{
		ApplyGradientToBackground();
	}

	public void OnValueChangedAddListener(UnityAction<float> action)
	{
		slider.onValueChanged.AddListener(action);
	}

	public Color GetColor()
	{
		return gradient.Evaluate(slider.normalizedValue);
	}

	[Button(null, EButtonEnableMode.Always)]
	private void ApplyGradientToBackground()
	{
		Texture2D texture2D = new Texture2D(width, height)
		{
			wrapMode = TextureWrapMode.Clamp,
			filterMode = FilterMode.Bilinear,
			anisoLevel = 9
		};
		for (int i = 0; i < width; i++)
		{
			float time = (float)i / ((float)width - 1f);
			Color color = gradient.Evaluate(time);
			for (int j = 0; j < height; j++)
			{
				texture2D.SetPixel(i, j, color);
			}
		}
		texture2D.Apply();
		background.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, width, height), Vector2.one * 0.5f);
		background.type = Image.Type.Simple;
	}

	public void RandomizeColor()
	{
		slider.value = Random.Range(slider.minValue, slider.maxValue);
	}

	public void SetFromColor(Color color)
	{
		float normalizedValue = 0f;
		float num = float.MaxValue;
		for (float num2 = 0f; num2 <= 1f; num2 += 0.01f)
		{
			float num3 = Vector4.SqrMagnitude(gradient.Evaluate(num2) - color);
			if (!(num3 >= num))
			{
				num = num3;
				normalizedValue = num2;
			}
		}
		slider.normalizedValue = normalizedValue;
	}
}
