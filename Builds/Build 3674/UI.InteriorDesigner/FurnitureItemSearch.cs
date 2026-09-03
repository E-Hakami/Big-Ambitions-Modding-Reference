using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Localizor;
using UnityEngine;

namespace UI.InteriorDesigner;

public class FurnitureItemSearch : MonoBehaviour
{
	private const int InitialCapacity = 1000;

	public Action<string> onAutocompleteCalculated;

	public Action<List<IDItemUiTemplateData>> onResultsFiltered;

	private readonly List<IDItemUiTemplateData> _allData = new List<IDItemUiTemplateData>(1000);

	private List<IDItemUiTemplateData> _visibleData = new List<IDItemUiTemplateData>(1000);

	private readonly Dictionary<string, IDItemUiTemplateData> _itemLookup = new Dictionary<string, IDItemUiTemplateData>(1000);

	private readonly Dictionary<string, string> _itemLocalizationDict = new Dictionary<string, string>(1000);

	private readonly Dictionary<string, HashSet<string>> _itemTagToItemNameDict = new Dictionary<string, HashSet<string>>();

	private readonly HashSet<string> _includedTags = new HashSet<string>();

	private readonly HashSet<string> _excludedTags = new HashSet<string>();

	private List<IDItemUiTemplateData> _lastFilteredData;

	private string _lastFilterText = string.Empty;

	private Action _showFavorites;

	private Func<bool> _isFavoriteItemsToggleOn;

	private void OnDestroy()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
	}

	private void OnLanguageChanged()
	{
		ClearLocalizationCache();
	}

	public void Initialize(Action showFavorites, Func<bool> isFavoriteItemsToggleOn)
	{
		_showFavorites = showFavorites;
		_isFavoriteItemsToggleOn = isFavoriteItemsToggleOn;
		onResultsFiltered = (Action<List<IDItemUiTemplateData>>)Delegate.Combine(onResultsFiltered, (Action<List<IDItemUiTemplateData>>)delegate(List<IDItemUiTemplateData> data)
		{
			_lastFilteredData = data;
		});
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
	}

	public void LoadItemData(List<string> itemNames, List<string> visibleTags, Action<IDItemTemplateBase, string> onItemSelected)
	{
		ClearItemData();
		EnsureCapacity(itemNames.Count);
		LoadItems(itemNames, visibleTags, onItemSelected);
		FurnitureTagMatcher.showAllItems = ShowAllItems;
		FurnitureTagMatcher.showNoItems = ShowNoItems;
		FurnitureTagMatcher.updateAutocompleteCallback = OnTagMatcherAutocompleteUpdate;
		FurnitureTagMatcher.filterByItemNamesCallback = FilterByItemNames;
		FurnitureTagMatcher.Initialize(_itemTagToItemNameDict, visibleTags);
		_lastFilteredData = _allData;
		_lastFilterText = string.Empty;
		ShowAllItems();
	}

	private void ClearItemData()
	{
		_allData.Clear();
		_itemLookup.Clear();
	}

	private void EnsureCapacity(int count)
	{
		if (_allData.Capacity < count)
		{
			_allData.Capacity = count;
		}
		if (_visibleData.Capacity < count)
		{
			_visibleData.Capacity = count;
		}
		if (_itemLookup.Count < count)
		{
			_itemLookup.EnsureCapacity(count);
		}
		if (_itemLocalizationDict.Count < count)
		{
			_itemLocalizationDict.EnsureCapacity(count);
		}
	}

	private void LoadItems(List<string> itemNames, List<string> visibleTags, Action<IDItemTemplateBase, string> onItemSelected)
	{
		for (int i = 0; i < itemNames.Count; i++)
		{
			string text = itemNames[i];
			Item byName = ItemsGetter.GetByName(text);
			IDItemUiTemplateData iDItemUiTemplateData = new IDItemUiTemplateData(text, byName.DefaultMarketPrice, onItemSelected);
			_allData.Add(iDItemUiTemplateData);
			_itemLookup[text] = iDItemUiTemplateData;
			LoadItemLocalization(text);
			LoadItemTags(text, byName, visibleTags);
		}
	}

	private void LoadItemLocalization(string itemName)
	{
		if (!_itemLocalizationDict.ContainsKey(itemName))
		{
			string text = (itemName.GetLocalization() ?? itemName).ToLowerInvariant().Replace(" ", string.Empty);
			if (GameManager.IsDevMode)
			{
				text = text + " | " + itemName;
			}
			_itemLocalizationDict[itemName] = text;
		}
	}

	private void LoadItemTags(string itemName, Item item, List<string> visibleTags)
	{
		for (int i = 0; i < visibleTags.Count; i++)
		{
			string key = visibleTags[i];
			if (item.HasTag(key))
			{
				if (!_itemTagToItemNameDict.TryGetValue(key, out var value))
				{
					value = new HashSet<string> { itemName };
					_itemTagToItemNameDict[key] = value;
				}
				else
				{
					value.Add(itemName);
				}
			}
		}
	}

	private void ClearLocalizationCache()
	{
		_itemLocalizationDict.Clear();
		_itemTagToItemNameDict.Clear();
		FurnitureTagMatcher.ClearLocalizedData();
	}

	public void ShowAllItems()
	{
		_visibleData.Clear();
		_visibleData.AddRange(_allData);
		onResultsFiltered?.Invoke(_visibleData);
	}

	public void ShowNoItems()
	{
		_visibleData.Clear();
		onResultsFiltered?.Invoke(_visibleData);
	}

	private void HandleEmptyFilter()
	{
		if (_isFavoriteItemsToggleOn())
		{
			_showFavorites();
		}
		else if (FurnitureCategoryToggle.CurrentActiveToggle != null)
		{
			FilterByTags(FurnitureCategoryToggle.CurrentActiveToggle.includedTags, FurnitureCategoryToggle.CurrentActiveToggle.excludedTags);
		}
		else
		{
			ShowAllItems();
		}
	}

	public void FilterByItemNames(HashSet<string> itemNames)
	{
		_visibleData.Clear();
		if (itemNames.Count == 0)
		{
			onResultsFiltered?.Invoke(_visibleData);
			return;
		}
		if (_visibleData.Capacity < itemNames.Count)
		{
			_visibleData.Capacity = itemNames.Count;
		}
		foreach (string itemName in itemNames)
		{
			if (_itemLookup.TryGetValue(itemName, out var value))
			{
				_visibleData.Add(value);
			}
		}
		onResultsFiltered?.Invoke(_visibleData);
	}

	public void FilterByTags(List<string> includedTags, List<string> excludedTags)
	{
		_visibleData.Clear();
		if (includedTags.Count == 0 && excludedTags.Count == 0)
		{
			ShowAllItems();
			return;
		}
		_includedTags.Clear();
		if (includedTags.Count > 0)
		{
			_includedTags.UnionWith(includedTags);
		}
		_excludedTags.Clear();
		if (excludedTags.Count > 0)
		{
			_excludedTags.UnionWith(excludedTags);
		}
		FurnitureTagMatcher.FilterItemsByTagSets(_allData, _includedTags, _excludedTags, ref _visibleData);
		onResultsFiltered?.Invoke(_visibleData);
	}

	public void FilterByText(string filterText)
	{
		string text = ((filterText == null) ? string.Empty : filterText.ToLowerInvariant().Replace(" ", string.Empty));
		bool isExtension = filterText != null && filterText.Length > _lastFilterText.Length && filterText.StartsWith(_lastFilterText, StringComparison.Ordinal);
		_lastFilterText = filterText;
		if (string.IsNullOrEmpty(filterText))
		{
			FurnitureTagMatcher.ResetTagSearchState();
			HandleEmptyFilter();
			return;
		}
		FurnitureTagMatcher.SetCurrentCategoryToggle(FurnitureCategoryToggle.CurrentActiveToggle);
		bool flag = text.Length > 0 && text[0] == '#';
		if (flag)
		{
			string text2 = text;
			FurnitureTagMatcher.FilterByTag(text2.Substring(1, text2.Length - 1), isExtension);
		}
		else
		{
			FilterByItemName(text, isExtension);
		}
		UpdateAutocomplete(filterText, text, flag);
	}

	private void UpdateAutocomplete(string filterText, string normalizedText, bool isTagSearch)
	{
		if (!string.IsNullOrEmpty(filterText) && isTagSearch)
		{
			if (FurnitureTagMatcher.GetCurrentTagMatches().Count <= 0)
			{
				onAutocompleteCalculated?.Invoke(string.Empty);
				return;
			}
			string bestAutocompleteWord = FurnitureTagMatcher.GetBestAutocompleteWord(normalizedText.Substring(1, normalizedText.Length - 1));
			string obj = (string.IsNullOrEmpty(bestAutocompleteWord) ? string.Empty : ("#" + bestAutocompleteWord));
			onAutocompleteCalculated?.Invoke(obj);
		}
	}

	private void OnTagMatcherAutocompleteUpdate(string autocomplete)
	{
		onAutocompleteCalculated?.Invoke(autocomplete);
	}

	private void FilterByItemName(string nameText, bool isExtension)
	{
		List<IDItemUiTemplateData> list = (isExtension ? _lastFilteredData : _allData);
		_visibleData.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			IDItemUiTemplateData iDItemUiTemplateData = list[i];
			if (_itemLocalizationDict[iDItemUiTemplateData.itemName].IndexOf(nameText, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				_visibleData.Add(iDItemUiTemplateData);
			}
		}
		onResultsFiltered?.Invoke(_visibleData);
	}

	public void ApplyAutocomplete(string autocompleteWord)
	{
		if (string.IsNullOrEmpty(autocompleteWord))
		{
			return;
		}
		string lastFilterText = _lastFilterText;
		if (!string.IsNullOrEmpty(lastFilterText) && lastFilterText.ToLowerInvariant().Replace(" ", string.Empty).StartsWith("#", StringComparison.Ordinal))
		{
			FurnitureTagMatcher.ApplyTagAutocomplete(autocompleteWord, out var matchedTag);
			if (!string.IsNullOrEmpty(matchedTag))
			{
				_lastFilterText = "#" + autocompleteWord;
			}
		}
		else
		{
			ApplyTextAutocomplete(autocompleteWord, isTagSearch: false);
		}
		onAutocompleteCalculated?.Invoke(string.Empty);
	}

	private void ApplyTextAutocomplete(string autocompleteWord, bool isTagSearch)
	{
		string text = autocompleteWord.Replace(" ", string.Empty);
		if (isTagSearch)
		{
			FurnitureTagMatcher.FilterByTag(text, isExtension: false);
		}
		else
		{
			FilterByItemName(text, isExtension: false);
		}
		_lastFilterText = autocompleteWord;
	}
}
