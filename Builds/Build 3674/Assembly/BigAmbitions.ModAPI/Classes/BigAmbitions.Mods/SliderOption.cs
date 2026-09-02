// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.SliderOption
using System;
using BigAmbitions.Mods;

public sealed class SliderOption : ModOption, IPersistableOption
{
	public int Min { get; }

	public int Max { get; }

	public int DefaultValue { get; }

	public string ValueLabelKey { get; }

	public Action<int> OnValueChanged { get; }

	public SliderOption(string id, string label, int min, int max, int defaultValue, Action<int> onValueChanged = null, string valueLabelKey = null)
		: base(id, label)
	{
		Min = min;
		Max = max;
		DefaultValue = defaultValue;
		OnValueChanged = onValueChanged;
		ValueLabelKey = valueLabelKey;
	}
}
