using System;
using System.Collections.Generic;
using JimmysUnityUtilities;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Shared;

public abstract class BaseFilterController<TModel> : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField searchField;

	[SerializeField]
	private Button clearFiltersButton;

	[SerializeField]
	private GameObject togglePrefab;

	[SerializeField]
	private BaseSortToggle<TModel>[] sortToggles;

	[SerializeField]
	private FilterToggleGroupParent[] toggleGroupParents;

	private readonly List<BaseFilterToggle<TModel>> _filterToggles = new List<BaseFilterToggle<TModel>>();

	private readonly Dictionary<FilterToggleGroup, CollapsibleFilterCategory> _groupCategories = new Dictionary<FilterToggleGroup, CollapsibleFilterCategory>();

	private UnityAction _onFilterChanged;

	private UnityAction<string> _onSearchChanged;

	private int _currentSortToggleIndex = -1;

	public int ActiveFilterCount
	{
		get
		{
			int num = 0;
			foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
			{
				if (filterToggle.IsOn)
				{
					num++;
				}
			}
			if (!searchField.text.IsNullOrEmpty())
			{
				num++;
			}
			return num;
		}
	}

	private void OnEnable()
	{
		KeyboardInputHelper.Configure(searchField);
		if (_onSearchChanged != null)
		{
			searchField.onValueChanged.AddListener(_onSearchChanged);
		}
	}

	private void OnDisable()
	{
		if (_onSearchChanged != null)
		{
			searchField.onValueChanged.RemoveListener(_onSearchChanged);
		}
	}

	public void SetUp(UnityAction onFilterChanged)
	{
		if (_filterToggles.IsEmpty())
		{
			CreateToggles();
		}
		_onFilterChanged = onFilterChanged;
		_onSearchChanged = delegate
		{
			_onFilterChanged?.Invoke();
		};
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			filterToggle.SetUp(OnFilterToggleChanged);
		}
		for (int num = 0; num < sortToggles.Length; num++)
		{
			sortToggles[num].SetUp(num, delegate(int idx)
			{
				OnSortToggled(idx, onFilterChanged);
			});
		}
		SetUpGroupToggles();
		RefreshGroupToggles();
	}

	public void RebuildToggles()
	{
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			if ((bool)filterToggle)
			{
				UnityEngine.Object.Destroy(filterToggle.gameObject);
			}
		}
		_filterToggles.Clear();
		CreateToggles();
		foreach (BaseFilterToggle<TModel> filterToggle2 in _filterToggles)
		{
			filterToggle2.SetUp(OnFilterToggleChanged);
		}
		RefreshGroupToggles();
	}

	public void ApplyFilters(ref List<TModel> items)
	{
		ApplyToggleFilters(ref items);
		ApplySearchFilter(ref items);
		clearFiltersButton.interactable = HasActiveFilter() || !string.IsNullOrEmpty(searchField.text);
	}

	public void SortItems(ref List<TModel> items, string context = null)
	{
		if (HasActiveSortToggle())
		{
			BaseSortToggle<TModel>[] array = sortToggles;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Sort(ref items, context);
			}
		}
	}

	public void OnClearFilters()
	{
		searchField.SetTextWithoutNotify(string.Empty);
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			filterToggle.SetToggleWithoutNotify(isOn: false);
		}
		OnFilterToggleChanged();
	}

	protected abstract void CreateToggles();

	protected abstract IEnumerable<string> GetSearchableText(TModel item);

	protected void CreateToggle<T>(Action<T> configure, FilterToggleGroup group) where T : BaseFilterToggle<TModel>
	{
		T orAddComponent = UnityEngine.Object.Instantiate(togglePrefab, GetParentForGroup(group)).GetOrAddComponent<T>();
		orAddComponent.Initialize(group);
		configure(orAddComponent);
		_filterToggles.Add(orAddComponent);
	}

	private void ApplyToggleFilters(ref List<TModel> items)
	{
		HashSet<FilterToggleGroup> hashSet = new HashSet<FilterToggleGroup>();
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			if (filterToggle.IsOn)
			{
				hashSet.Add(filterToggle.Group);
			}
		}
		foreach (FilterToggleGroup item in hashSet)
		{
			for (int num = items.Count - 1; num >= 0; num--)
			{
				bool flag = false;
				foreach (BaseFilterToggle<TModel> filterToggle2 in _filterToggles)
				{
					if (filterToggle2.Group == item && filterToggle2.IsOn && filterToggle2.PassesFilter(items[num]))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					items.RemoveAt(num);
				}
			}
		}
	}

	private void ApplySearchFilter(ref List<TModel> items)
	{
		string text = searchField.text;
		if (text.IsNullOrEmpty())
		{
			return;
		}
		for (int num = items.Count - 1; num >= 0; num--)
		{
			bool flag = false;
			foreach (string item in GetSearchableText(items[num]))
			{
				if (!item.IsNullOrEmpty() && item.Contains(text, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				items.RemoveAt(num);
			}
		}
	}

	private Transform GetParentForGroup(FilterToggleGroup group)
	{
		FilterToggleGroupParent[] array = toggleGroupParents;
		for (int i = 0; i < array.Length; i++)
		{
			FilterToggleGroupParent filterToggleGroupParent = array[i];
			if (filterToggleGroupParent.group == group)
			{
				return filterToggleGroupParent.parent;
			}
		}
		return base.transform;
	}

	private void SetUpGroupToggles()
	{
		_groupCategories.Clear();
		FilterToggleGroupParent[] array = toggleGroupParents;
		for (int i = 0; i < array.Length; i++)
		{
			FilterToggleGroupParent filterToggleGroupParent = array[i];
			CollapsibleFilterCategory componentInParent = filterToggleGroupParent.parent.GetComponentInParent<CollapsibleFilterCategory>(includeInactive: true);
			if ((bool)componentInParent)
			{
				_groupCategories[filterToggleGroupParent.group] = componentInParent;
			}
			else
			{
				Debug.LogError("No CollapsibleFilterCategory above " + filterToggleGroupParent.parent.name + ".", filterToggleGroupParent.parent);
			}
		}
		foreach (KeyValuePair<FilterToggleGroup, CollapsibleFilterCategory> groupCategory in _groupCategories)
		{
			FilterToggleGroup group = groupCategory.Key;
			groupCategory.Value.SetUpToggleAll(delegate(bool isOn)
			{
				ToggleGroup(group, isOn);
			});
		}
	}

	private void ToggleGroup(FilterToggleGroup group, bool isOn)
	{
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			if (filterToggle.Group == group)
			{
				filterToggle.SetToggleWithoutNotify(isOn);
			}
		}
		OnFilterToggleChanged();
	}

	private void OnFilterToggleChanged()
	{
		RefreshGroupToggles();
		_onFilterChanged?.Invoke();
	}

	private void RefreshGroupToggles()
	{
		foreach (KeyValuePair<FilterToggleGroup, CollapsibleFilterCategory> groupCategory in _groupCategories)
		{
			groupCategory.Value.SetToggleAllWithoutNotify(HasActiveFilterInGroup(groupCategory.Key));
		}
	}

	private bool HasActiveFilterInGroup(FilterToggleGroup group)
	{
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			if (filterToggle.Group == group && filterToggle.IsOn)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasActiveFilter()
	{
		foreach (BaseFilterToggle<TModel> filterToggle in _filterToggles)
		{
			if (filterToggle.IsOn)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasActiveSortToggle()
	{
		BaseSortToggle<TModel>[] array = sortToggles;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].IsOn)
			{
				return true;
			}
		}
		return false;
	}

	private void OnSortToggled(int toggleIndex, UnityAction onFilterChanged)
	{
		if (toggleIndex != _currentSortToggleIndex)
		{
			for (int i = 0; i < sortToggles.Length; i++)
			{
				if (sortToggles[i].IsOn && i != toggleIndex)
				{
					sortToggles[i].SetState(0, updateState: false);
				}
			}
			_currentSortToggleIndex = toggleIndex;
		}
		onFilterChanged?.Invoke();
	}
}
