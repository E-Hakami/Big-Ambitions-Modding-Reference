using System.Collections.Generic;
using BigAmbitions.Mods;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;

namespace BigAmbitions.ModsInternal;

public sealed class ModOptionsDropdownControl : MonoBehaviour, IModOptionsControl
{
	[SerializeField]
	private Dropdown dropdown;

	[SerializeField]
	private TextLocalizationComponent label;

	public void Initialize(ModOption option)
	{
		DropdownOption data = (DropdownOption)option;
		label.Key = data.Label;
		int num = ModOptionPrefs.Load(data.ModId, data.Id, data.DefaultIndex);
		int num2 = ((num >= 0 && num < data.ChoiceKeys.Length) ? num : data.DefaultIndex);
		dropdown.onOptionSelected.RemoveAllListeners();
		dropdown.SetOptions(new List<string>(data.ChoiceKeys), localize: true, num2);
		dropdown.onOptionSelected.AddListener(delegate(int value)
		{
			ModOptionPrefs.Save(data.ModId, data.Id, value);
			data.OnValueChanged?.Invoke(value);
		});
		data.OnValueChanged?.Invoke(num2);
	}
}
