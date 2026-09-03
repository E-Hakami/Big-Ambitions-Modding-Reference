// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.ToggleOption
using System;
using BigAmbitions.Mods;

public sealed class ToggleOption : ModOption, IPersistableOption
{
	public bool DefaultValue { get; }

	public Action<bool> OnValueChanged { get; }

	public ToggleOption(string id, string label, bool defaultValue, Action<bool> onValueChanged = null)
		: base(id, label)
	{
		DefaultValue = defaultValue;
		OnValueChanged = onValueChanged;
	}
}
