using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace UI.Elements;

public class RangeSliderToLabel : MonoBehaviour
{
	public TextMeshProUGUI startLabel;

	public TextMeshProUGUI endLabel;

	private RangeSlider _slider;

	public RangeSliderToLabel(RangeSlider slider)
	{
		_slider = slider;
		SetLabels(slider.LowValue, slider.HighValue);
	}

	public void ValueChanged(float start, float end)
	{
		SetLabels(start, end);
	}

	private void SetLabels(float start, float end)
	{
		startLabel.text = ((int)start).GetFormattedTime() ?? "";
		endLabel.text = ((int)end).GetFormattedTime() ?? "";
	}
}
