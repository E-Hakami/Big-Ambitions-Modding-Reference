using System.Collections.Generic;
using BigAmbitions.InputSystem;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace City.CityMap;

public class CityMapFilterCategory : MonoBehaviour
{
	private static readonly Quaternion CollapsedRotation = Quaternion.Euler(0f, 0f, 180f);

	private static readonly Quaternion ExpandedRotation = Quaternion.Euler(0f, 0f, 90f);

	[SerializeField]
	private TextLocalizationComponent label;

	[SerializeField]
	private Button collapse;

	[SerializeField]
	private Toggle toggleAll;

	private string _categoryName;

	private string _searchText;

	private bool _restoreCollapsedAfterSearch;

	private bool _collapseChangedDuringSearch;

	private readonly List<CityMapFilter> _filters = new List<CityMapFilter>();

	public bool IsCollapsed { get; private set; }

	public Toggle ToggleAll => toggleAll;

	public void SetUp(string categoryNameKey)
	{
		_categoryName = categoryNameKey;
		label.Key = categoryNameKey;
		GameInstance current = SaveGameManager.Current;
		if (current.CollapsedCitymapFilterCategories == null)
		{
			current.CollapsedCitymapFilterCategories = new List<string>();
		}
		SetCollapsedState(SaveGameManager.Current.CollapsedCitymapFilterCategories.Contains(_categoryName), animate: false);
	}

	public void OnCollapseClick()
	{
		PlayerAction.Click.Reset();
		if (!string.IsNullOrWhiteSpace(_searchText))
		{
			_collapseChangedDuringSearch = true;
			_restoreCollapsedAfterSearch = false;
		}
		SetCollapsedState(!IsCollapsed);
		if (IsCollapsed)
		{
			if (!SaveGameManager.Current.CollapsedCitymapFilterCategories.Contains(_categoryName))
			{
				SaveGameManager.Current.CollapsedCitymapFilterCategories.Add(_categoryName);
			}
		}
		else
		{
			SaveGameManager.Current.CollapsedCitymapFilterCategories.RemoveAll((string x) => x == _categoryName);
		}
	}

	private void SetCollapsedState(bool collapsed, bool animate = true)
	{
		PlayerAction.Click.Reset();
		IsCollapsed = collapsed;
		Quaternion quaternion = (IsCollapsed ? CollapsedRotation : ExpandedRotation);
		if (animate)
		{
			collapse.transform.DORotateQuaternion(quaternion, 0.2f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		}
		else
		{
			collapse.transform.rotation = quaternion;
		}
		UpdateFilterVisibility();
	}

	public void OnToggleAllClick(bool isOn)
	{
		foreach (CityMapFilter filter in _filters)
		{
			filter.Toggle.isOn = isOn;
		}
	}

	public void UpdateToggleAllState()
	{
		bool isOnWithoutNotify = false;
		foreach (CityMapFilter filter in _filters)
		{
			if (filter.IsAvailable() && filter.Toggle.isOn)
			{
				isOnWithoutNotify = true;
				break;
			}
		}
		toggleAll.SetIsOnWithoutNotify(isOnWithoutNotify);
	}

	public void AddFilter(CityMapFilter filter)
	{
		_filters.Add(filter);
		filter.category = this;
		UpdateFilterVisibility();
	}

	public void ApplySearch(string searchText)
	{
		bool flag = !string.IsNullOrWhiteSpace(_searchText);
		_searchText = searchText;
		if (string.IsNullOrWhiteSpace(_searchText))
		{
			if (_restoreCollapsedAfterSearch)
			{
				SetCollapsedState(collapsed: true, animate: false);
			}
			else
			{
				UpdateFilterVisibility();
			}
			_restoreCollapsedAfterSearch = false;
			_collapseChangedDuringSearch = false;
			return;
		}
		if (!flag)
		{
			_restoreCollapsedAfterSearch = false;
			_collapseChangedDuringSearch = false;
		}
		if (!_collapseChangedDuringSearch && IsCollapsed && HasMatchingFilter())
		{
			_restoreCollapsedAfterSearch = true;
			SetCollapsedState(collapsed: false, animate: false);
		}
		else
		{
			UpdateFilterVisibility();
		}
	}

	private bool HasMatchingFilter()
	{
		foreach (CityMapFilter filter in _filters)
		{
			if (filter.IsAvailable() && filter.MatchesSearch(_searchText))
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateFilterVisibility()
	{
		bool flag = !string.IsNullOrWhiteSpace(_searchText);
		bool flag2 = false;
		foreach (CityMapFilter filter in _filters)
		{
			bool flag3 = !flag || filter.MatchesSearch(_searchText);
			bool flag4 = filter.IsAvailable() & flag3;
			flag2 |= flag4;
			filter.gameObject.SetActive(flag4 && !IsCollapsed);
		}
		base.gameObject.SetActive(!flag | flag2);
	}
}
