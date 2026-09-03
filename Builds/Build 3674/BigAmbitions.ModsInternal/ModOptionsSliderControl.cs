using BigAmbitions.Mods;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions.ModsInternal;

public sealed class ModOptionsSliderControl : MonoBehaviour, IModOptionsControl
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private TextLocalizationComponent valueLabel;

	public void Initialize(ModOption option)
	{
		SliderOption data = (SliderOption)option;
		label.Key = data.Label;
		slider.minValue = data.Min;
		slider.maxValue = data.Max;
		slider.wholeNumbers = true;
		slider.onValueChanged.RemoveAllListeners();
		slider.SetValueWithoutNotify(ModOptionPrefs.Load(data.ModId, data.Id, data.DefaultValue));
		int num = (int)slider.value;
		bool showValueLabel = !string.IsNullOrEmpty(data.ValueLabelKey);
		valueLabel.gameObject.SetActive(showValueLabel);
		if (showValueLabel)
		{
			valueLabel.Key = data.ValueLabelKey;
			valueLabel.Arguments = new
			{
				value = num
			};
		}
		slider.onValueChanged.AddListener(delegate(float rawValue)
		{
			int num2 = (int)rawValue;
			ModOptionPrefs.Save(data.ModId, data.Id, num2);
			if (showValueLabel)
			{
				valueLabel.Arguments = new
				{
					value = num2
				};
			}
			data.OnValueChanged?.Invoke(num2);
		});
		data.OnValueChanged?.Invoke(num);
	}
}
