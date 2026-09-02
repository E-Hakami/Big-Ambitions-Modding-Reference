// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.Mods.DropdownOption
using System;
using BigAmbitions.Mods;
using UnityEngine;

public sealed class DropdownOption : ModOption, IPersistableOption
{
	public string[] ChoiceKeys { get; }

	public int DefaultIndex { get; }

	public Action<int> OnValueChanged { get; }

	public DropdownOption(string id, string label, string[] choiceKeys, int defaultIndex = 0, Action<int> onValueChanged = null)
		: base(id, label)
	{
		if (choiceKeys == null || choiceKeys.Length == 0)
		{
			Debug.LogError("[ModOptions] DropdownOption '" + id + "' has no choices; rendering placeholder.");
			choiceKeys = new string[1] { string.Empty };
		}
		ChoiceKeys = choiceKeys;
		DefaultIndex = Mathf.Clamp(defaultIndex, 0, choiceKeys.Length - 1);
		OnValueChanged = onValueChanged;
	}
}
