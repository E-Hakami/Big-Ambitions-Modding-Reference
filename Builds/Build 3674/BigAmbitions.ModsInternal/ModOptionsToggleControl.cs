using BigAmbitions.Mods;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions.ModsInternal;

public sealed class ModOptionsToggleControl : MonoBehaviour, IModOptionsControl
{
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private TextLocalizationComponent label;

	public void Initialize(ModOption option)
	{
		ToggleOption data = (ToggleOption)option;
		label.Key = data.Label;
		toggle.onValueChanged.RemoveAllListeners();
		toggle.SetIsOnWithoutNotify(ModOptionPrefs.Load(data.ModId, data.Id, data.DefaultValue));
		toggle.onValueChanged.AddListener(delegate(bool value)
		{
			ModOptionPrefs.Save(data.ModId, data.Id, value);
			data.OnValueChanged?.Invoke(value);
		});
		data.OnValueChanged?.Invoke(toggle.isOn);
	}
}
