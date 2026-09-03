// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.ModOptions
using System;
using System.Collections.Generic;
using BigAmbitions.Mods;

public sealed class ModOptions
{
	private readonly List<ModOption> _options = new List<ModOption>();

	public IReadOnlyList<ModOption> Options => _options;

	public ModOptions AddHeader(string label)
	{
		return AddCustom(new HeaderOption(label));
	}

	public ModOptions AddSplitter()
	{
		return AddCustom(new SplitterOption());
	}

	public ModOptions AddButton(string label, Action onClick = null)
	{
		return AddCustom(new ButtonOption(label, onClick));
	}

	public ModOptions AddToggle(string id, string label, bool defaultValue, Action<bool> onValueChanged = null)
	{
		return AddCustom(new ToggleOption(id, label, defaultValue, onValueChanged));
	}

	public ModOptions AddSlider(string id, string label, int min, int max, int defaultValue, Action<int> onValueChanged = null, string valueLabelKey = null)
	{
		return AddCustom(new SliderOption(id, label, min, max, defaultValue, onValueChanged, valueLabelKey));
	}

	public ModOptions AddDropdown(string id, string label, string[] choiceKeys, int defaultIndex = 0, Action<int> onValueChanged = null)
	{
		return AddCustom(new DropdownOption(id, label, choiceKeys, defaultIndex, onValueChanged));
	}

	public ModOptions AddCustom(ModOption option)
	{
		_options.Add(option);
		return this;
	}
}
