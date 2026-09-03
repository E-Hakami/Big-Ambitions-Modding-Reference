using BigAmbitions.Mods;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace BigAmbitions.ModsInternal;

public sealed class ModOptionsHeaderControl : MonoBehaviour, IModOptionsControl
{
	[SerializeField]
	private TextLocalizationComponent label;

	public void Initialize(ModOption option)
	{
		label.Key = option.Label;
	}
}
