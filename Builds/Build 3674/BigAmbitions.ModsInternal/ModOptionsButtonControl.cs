using BigAmbitions.Mods;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions.ModsInternal;

public sealed class ModOptionsButtonControl : MonoBehaviour, IModOptionsControl
{
	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private Button button;

	public void Initialize(ModOption option)
	{
		ButtonOption buttonOption = (ButtonOption)option;
		label.Key = buttonOption.Label;
		button.onClick.RemoveAllListeners();
		if (buttonOption.OnClick != null)
		{
			button.onClick.AddListener(buttonOption.OnClick.Invoke);
		}
	}
}
