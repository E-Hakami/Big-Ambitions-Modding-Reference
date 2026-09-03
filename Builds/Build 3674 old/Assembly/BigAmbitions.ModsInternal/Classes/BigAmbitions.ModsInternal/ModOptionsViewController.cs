// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModOptionsViewController
using System.Collections.Generic;
using BigAmbitions.Mods;
using UnityEngine;

public sealed class ModOptionsViewController : MonoBehaviour
{
	[SerializeField]
	private Transform contentRoot;

	[SerializeField]
	private GameObject modOptionsHeaderPrefab;

	[SerializeField]
	private GameObject modOptionsSliderPrefab;

	[SerializeField]
	private GameObject modOptionsTogglePrefab;

	[SerializeField]
	private GameObject modOptionsDropdownPrefab;

	[SerializeField]
	private GameObject modOptionsButtonPrefab;

	[SerializeField]
	private GameObject modOptionsSplitterPrefab;

	private void OnEnable()
	{
		OptionsService.OnChanged += Rebuild;
		OptionsService.OnReset += ResetToDefaults;
		Rebuild();
	}

	private void OnDisable()
	{
		OptionsService.OnChanged -= Rebuild;
		OptionsService.OnReset -= ResetToDefaults;
	}

	private void Rebuild()
	{
		for (int num = contentRoot.childCount - 1; num >= 0; num--)
		{
			GameObject obj = contentRoot.GetChild(num).gameObject;
			obj.SetActive(value: false);
			Object.Destroy(obj);
		}
		foreach (var (modId, modOptions2) in OptionsService.RegisteredEntries)
		{
			foreach (ModOption option in modOptions2.Options)
			{
				SpawnOption(option, modId);
			}
		}
	}

	private void ResetToDefaults()
	{
		foreach (var (modId, modOptions2) in OptionsService.RegisteredEntries)
		{
			ClearOptionPrefs(modId, modOptions2.Options);
		}
		Rebuild();
	}

	private static void ClearOptionPrefs(string modId, IEnumerable<ModOption> options)
	{
		if (string.IsNullOrEmpty(modId) || options == null)
		{
			return;
		}
		foreach (ModOption option in options)
		{
			if (option is IPersistableOption persistableOption && !string.IsNullOrEmpty(persistableOption.Id))
			{
				PlayerPrefs.DeleteKey("m:" + modId + ":" + persistableOption.Id);
			}
		}
	}

	private void SpawnOption(ModOption option, string modId)
	{
		GameObject gameObject = ResolvePrefab(option);
		IModOptionsControl component;
		if (gameObject == null)
		{
			option.SpawnUi(contentRoot, modId);
		}
		else if (Object.Instantiate(gameObject, contentRoot).TryGetComponent<IModOptionsControl>(out component))
		{
			component.Initialize(option);
		}
	}

	private GameObject ResolvePrefab(ModOption option)
	{
		if (!(option is HeaderOption))
		{
			if (!(option is SliderOption))
			{
				if (!(option is ToggleOption))
				{
					if (!(option is DropdownOption))
					{
						if (!(option is ButtonOption))
						{
							if (option is SplitterOption)
							{
								return modOptionsSplitterPrefab;
							}
							return null;
						}
						return modOptionsButtonPrefab;
					}
					return modOptionsDropdownPrefab;
				}
				return modOptionsTogglePrefab;
			}
			return modOptionsSliderPrefab;
		}
		return modOptionsHeaderPrefab;
	}
}
