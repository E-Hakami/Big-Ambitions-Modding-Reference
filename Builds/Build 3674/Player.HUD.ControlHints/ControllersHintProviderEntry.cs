using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class ControllersHintProviderEntry : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent headerLocalizationComponent;

	[SerializeField]
	private GameObject headerContainer;

	[SerializeField]
	private SingleControlHintEntry template;

	[SerializeField]
	private Transform container;

	private readonly List<SingleControlHintEntry> _entries = new List<SingleControlHintEntry>();

	private string _headerKey;

	private void Awake()
	{
		template.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		RefreshHeader();
	}

	public void SetProvider(IControlsHintProvider provider, bool showHeader)
	{
		_headerKey = provider.HeaderKey;
		headerContainer.SetActive(showHeader);
		if (showHeader)
		{
			RefreshHeader();
		}
		IReadOnlyList<ControlsHint> hints = provider.Hints;
		for (int i = 0; i < hints.Count; i++)
		{
			SingleControlHintEntry entry = GetEntry(i);
			entry.SetHint(hints[i]);
			entry.gameObject.SetActive(value: true);
			entry.transform.SetAsLastSibling();
		}
		for (int j = hints.Count; j < _entries.Count; j++)
		{
			_entries[j].gameObject.SetActive(value: false);
		}
	}

	private SingleControlHintEntry GetEntry(int index)
	{
		if (index < _entries.Count)
		{
			return _entries[index];
		}
		SingleControlHintEntry singleControlHintEntry = Object.Instantiate(template, container);
		_entries.Add(singleControlHintEntry);
		return singleControlHintEntry;
	}

	private void RefreshHeader()
	{
		if (headerContainer.activeSelf && !string.IsNullOrEmpty(_headerKey))
		{
			headerLocalizationComponent.Key = _headerKey;
		}
	}
}
