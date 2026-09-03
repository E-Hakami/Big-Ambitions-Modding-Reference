using System;
using System.Collections.Generic;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class ControlsHintsUI : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent headerLocalization;

	[SerializeField]
	private ControllersHintProviderEntry hintProviderTemplate;

	[SerializeField]
	private Transform container;

	private readonly List<ControllersHintProviderEntry> _entries = new List<ControllersHintProviderEntry>();

	private string _headerKey;

	private void Awake()
	{
		hintProviderTemplate.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(RefreshHeader));
		RefreshHeader();
	}

	private void OnDisable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(RefreshHeader));
	}

	public void ShowHints(IReadOnlyList<IControlsHintProvider> providers)
	{
		for (int i = 0; i < providers.Count; i++)
		{
			ControllersHintProviderEntry entry = GetEntry(i);
			entry.SetProvider(providers[i], i > 0);
			entry.gameObject.SetActive(value: true);
			entry.transform.SetAsLastSibling();
		}
		for (int j = providers.Count; j < _entries.Count; j++)
		{
			_entries[j].gameObject.SetActive(value: false);
		}
		_headerKey = ((providers.Count > 0) ? providers[0].HeaderKey : null);
		RefreshHeader();
		base.gameObject.SetActive(providers.Count > 0);
	}

	private ControllersHintProviderEntry GetEntry(int index)
	{
		if (index < _entries.Count)
		{
			return _entries[index];
		}
		ControllersHintProviderEntry controllersHintProviderEntry = UnityEngine.Object.Instantiate(hintProviderTemplate, container);
		_entries.Add(controllersHintProviderEntry);
		return controllersHintProviderEntry;
	}

	private void RefreshHeader()
	{
		if (!string.IsNullOrEmpty(_headerKey))
		{
			headerLocalization.Key = _headerKey;
		}
	}
}
