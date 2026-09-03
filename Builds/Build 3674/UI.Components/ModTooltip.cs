using System;
using System.Collections.Generic;
using BigAmbitions.ModsInternal;
using Localizor.LanguageChangeEvent;
using Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public sealed class ModTooltip : TooltipTarget
{
	private static readonly LanguageChangeEventDataHolder HeaderData = new LanguageChangeEventDataHolder
	{
		Key = "mods_tooltip_header"
	};

	private static readonly LanguageChangeEventDataHolder NotEnabledData = new LanguageChangeEventDataHolder
	{
		Key = "mods_tooltip_not_enabled_warning"
	};

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private Color inactiveModColor;

	private readonly List<(string Label, Color Color)> _modLines = new List<(string, Color)>();

	private SaveGameManager.SaveGameStruct _saveGame;

	private bool _hasAnyInactiveMods;

	public void Setup(SaveGameManager.SaveGameStruct saveGame)
	{
		_saveGame = saveGame;
		base.gameObject.SetActive(saveGame.hasEverUsedMods);
		iconImage.color = Color.white;
		_modLines.Clear();
		if (!saveGame.hasEverUsedMods)
		{
			return;
		}
		_hasAnyInactiveMods = false;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (List<DiscoveredModEntry> value in ModDiscoveryRegistry.Entries.Values)
		{
			foreach (DiscoveredModEntry item2 in value)
			{
				if (!string.IsNullOrWhiteSpace(item2.ModId))
				{
					hashSet.Add(item2.ModId);
				}
			}
		}
		List<SaveGameManager.SaveGameStruct.ActiveModAtSave> activeModsAtLastSave = saveGame.activeModsAtLastSave;
		if (activeModsAtLastSave == null)
		{
			return;
		}
		for (int i = 0; i < activeModsAtLastSave.Count; i++)
		{
			SaveGameManager.SaveGameStruct.ActiveModAtSave activeModAtSave = activeModsAtLastSave[i];
			if (activeModAtSave != null && !string.IsNullOrWhiteSpace(activeModAtSave.modId))
			{
				bool flag = hashSet.Contains(activeModAtSave.modId) && IsEnabled(activeModAtSave.modId);
				string item = (string.IsNullOrWhiteSpace(activeModAtSave.modDisplayName) ? activeModAtSave.modId : activeModAtSave.modDisplayName);
				_modLines.Add((item, flag ? Color.white : inactiveModColor));
				if (!flag && !_hasAnyInactiveMods)
				{
					_hasAnyInactiveMods = true;
				}
			}
		}
		if (_hasAnyInactiveMods)
		{
			iconImage.color = inactiveModColor;
		}
	}

	public void Clear()
	{
		_saveGame = null;
		_hasAnyInactiveMods = false;
		_modLines.Clear();
		base.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		OnDiscoveryUpdated();
		ModDiscoveryRegistry.OnDiscoveryUpdated += OnDiscoveryUpdated;
	}

	private void OnDisable()
	{
		ModDiscoveryRegistry.OnDiscoveryUpdated -= OnDiscoveryUpdated;
	}

	private void OnDestroy()
	{
		ModDiscoveryRegistry.OnDiscoveryUpdated -= OnDiscoveryUpdated;
	}

	protected override void ShowTooltip()
	{
		TooltipSystem.AddHeader(HeaderData);
		if (_modLines.Count == 0)
		{
			return;
		}
		if (_hasAnyInactiveMods)
		{
			TooltipSystem.AddSplitter();
			TooltipSystem.AddLabel(NotEnabledData, inactiveModColor);
		}
		TooltipSystem.AddSplitter();
		foreach (var modLine in _modLines)
		{
			TooltipSystem.AddLabel(modLine.Label, modLine.Color);
		}
	}

	private static bool IsEnabled(string modId)
	{
		if (ulong.TryParse(modId, out var result))
		{
			return ModManifest.Contains(result);
		}
		return true;
	}

	private void OnDiscoveryUpdated()
	{
		if (_saveGame != null)
		{
			Setup(_saveGame);
		}
	}
}
