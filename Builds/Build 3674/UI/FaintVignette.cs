using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class FaintVignette : MonoBehaviour
{
	[SerializeField]
	private Image vignetteImage;

	[SerializeField]
	private AnimationCurve blinkCurve;

	[SerializeField]
	private float blinkDuration = 1f;

	[SerializeField]
	private float blinkInterval = 5f;

	private float _timer;

	private void OnEnable()
	{
		_timer = 0f;
		Color color = vignetteImage.color;
		color.a = blinkCurve.Evaluate(0f);
		vignetteImage.color = color;
	}

	private void Update()
	{
		_timer = (_timer + Time.deltaTime) % (blinkDuration + blinkInterval);
		Color color = vignetteImage.color;
		color.a = ((_timer < blinkDuration) ? blinkCurve.Evaluate(_timer / blinkDuration) : 0f);
		vignetteImage.color = color;
	}
}
