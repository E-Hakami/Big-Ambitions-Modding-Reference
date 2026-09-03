using System;
using System.Collections.Generic;
using AwesomeCharts;
using UI.Smartphone.Apps.BizMan;
using UI.Smartphone.Apps.Rivals;
using UnityEngine;

namespace Extensions;

public class ChartController : MonoBehaviour
{
	public LineChart chart;

	public List<FilterOption> filterOptions;

	[SerializeField]
	private ChartFilterEntry filterEntryTemplate;

	[SerializeField]
	private Transform filterEntryContainer;

	[Header("Optional")]
	[SerializeField]
	private BizManInsight bizManInsight;

	[SerializeField]
	private SelectedRivalUI selectedRivalUI;

	private bool _initialized;

	private readonly Dictionary<string, ChartFilterEntry> _instantiatedFilters = new Dictionary<string, ChartFilterEntry>();

	private void Awake()
	{
		filterEntryTemplate.gameObject.SetActive(value: false);
	}

	private void InitializeIfNeeded()
	{
		if (_initialized)
		{
			return;
		}
		foreach (FilterOption filterOption in filterOptions)
		{
			CreateNewEntry(filterOption);
		}
		_initialized = true;
	}

	public void CreateNewEntry(FilterOption filterOption)
	{
		ChartFilterEntry chartFilterEntry = UnityEngine.Object.Instantiate(filterEntryTemplate, filterEntryContainer);
		chartFilterEntry.Initialize(filterOption, SetFilter);
		_instantiatedFilters.Add(filterOption.value, chartFilterEntry);
	}

	public void SetFilter(string filterName)
	{
		InitializeIfNeeded();
		foreach (var (text2, chartFilterEntry2) in _instantiatedFilters)
		{
			chartFilterEntry2.SetSelected(text2.Equals(filterName, StringComparison.OrdinalIgnoreCase));
		}
		bizManInsight?.FilterChanged(filterName);
		selectedRivalUI?.FilterChanged(filterName);
	}
}
