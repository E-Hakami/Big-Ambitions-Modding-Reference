using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using Localizor;
using UnityEngine;

namespace UI.InteriorDesigner;

public static class FurnitureTagMatcher
{
	private const int TagCapacity = 50;

	public static Action showAllItems;

	public static Action showNoItems;

	public static Action<string> updateAutocompleteCallback;

	public static Action<HashSet<string>> filterByItemNamesCallback;

	private static HashSet<string> LastFilteredTags = new HashSet<string>();

	private static readonly HashSet<string> AllTags = new HashSet<string>(50);

	private static readonly HashSet<string> ActiveItemTags = new HashSet<string>();

	private static readonly List<(string Tag, int Score, string BestSynonym)> TagMatches = new List<(string, int, string)>();

	private static readonly HashSet<string> TempMatchedItems = new HashSet<string>();

	private static readonly Dictionary<string, List<string>> TagSynonymsDict = new Dictionary<string, List<string>>();

	private static Dictionary<string, HashSet<string>> ItemTagToItemNameDict;

	private static FurnitureCategoryToggle CurrentCategoryToggle;

	public static List<(string Tag, int Score, string BestSynonym)> GetCurrentTagMatches()
	{
		return TagMatches;
	}

	public static void Initialize(Dictionary<string, HashSet<string>> itemTagToItemNameDict, IReadOnlyList<string> visibleTags)
	{
		ItemTagToItemNameDict = itemTagToItemNameDict;
		LoadAllTags(visibleTags);
		LoadTagSynonyms();
		LastFilteredTags = new HashSet<string>(AllTags);
	}

	public static void ClearLocalizedData()
	{
		TagSynonymsDict.Clear();
	}

	public static void SetCurrentCategoryToggle(FurnitureCategoryToggle toggle)
	{
		CurrentCategoryToggle = toggle;
	}

	public static void ResetTagSearchState()
	{
		TagMatches.Clear();
		ActiveItemTags.Clear();
		LastFilteredTags.Clear();
		foreach (string allTag in AllTags)
		{
			LastFilteredTags.Add(allTag);
		}
	}

	public static void FilterByTag(string tagText, bool isExtension)
	{
		if (string.IsNullOrEmpty(tagText))
		{
			ResetTagSearchState();
			showAllItems?.Invoke();
			return;
		}
		FindMatchingTags(isExtension ? LastFilteredTags : AllTags, tagText);
		ActiveItemTags.Clear();
		for (int i = 0; i < TagMatches.Count; i++)
		{
			ActiveItemTags.Add(TagMatches[i].Tag);
		}
		if (ActiveItemTags.Count > 0)
		{
			ApplyTagFilters();
			return;
		}
		showNoItems?.Invoke();
		updateAutocompleteCallback?.Invoke(string.Empty);
	}

	public static void FilterItemsByTagSets(List<IDItemUiTemplateData> dataSource, HashSet<string> includedSet, HashSet<string> excludedSet, ref List<IDItemUiTemplateData> visibleData)
	{
		for (int i = 0; i < dataSource.Count; i++)
		{
			Item byName = ItemsGetter.GetByName(dataSource[i].itemName);
			if ((GameManager.IsDevMode || !byName.HasTag("ba:itemtag_dev")) && ItemHasIncludedTag(byName, includedSet) && !ItemHasExcludedTag(byName, excludedSet))
			{
				visibleData.Add(dataSource[i]);
			}
		}
	}

	public static string GetBestAutocompleteWord(string searchText)
	{
		if (string.IsNullOrEmpty(searchText) || TagMatches.Count == 0)
		{
			return string.Empty;
		}
		int num = int.MaxValue;
		int num2 = -1;
		for (int i = 0; i < TagMatches.Count; i++)
		{
			(string, int, string) tuple = TagMatches[i];
			if (tuple.Item3.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) && tuple.Item2 < num)
			{
				num = tuple.Item2;
				num2 = i;
			}
		}
		if (num2 < 0)
		{
			return string.Empty;
		}
		return TagMatches[num2].BestSynonym;
	}

	public static void ApplyTagAutocomplete(string autocompleteWord, out string matchedTag)
	{
		matchedTag = null;
		(string, int, string) tuple = (null, int.MaxValue, null);
		for (int i = 0; i < TagMatches.Count; i++)
		{
			(string, int, string) tuple2 = TagMatches[i];
			if (string.Equals(tuple2.Item3, autocompleteWord, StringComparison.OrdinalIgnoreCase))
			{
				tuple = tuple2;
				break;
			}
		}
		if (!string.IsNullOrEmpty(tuple.Item1))
		{
			(matchedTag, _, _) = tuple;
			ActiveItemTags.Clear();
			ActiveItemTags.Add(tuple.Item1);
			TempMatchedItems.Clear();
			GetItemsByTags(ActiveItemTags, TempMatchedItems);
			filterByItemNamesCallback?.Invoke(TempMatchedItems);
			LastFilteredTags.Clear();
			LastFilteredTags.Add(tuple.Item1);
		}
	}

	private static void LoadAllTags(IReadOnlyList<string> visibleTags)
	{
		AllTags.Clear();
		for (int i = 0; i < visibleTags.Count; i++)
		{
			string text = visibleTags[i];
			if (!string.IsNullOrWhiteSpace(text))
			{
				AllTags.Add(text);
			}
		}
		if (!GameManager.IsDevMode)
		{
			AllTags.Remove("ba:itemtag_dev");
		}
	}

	private static void LoadTagSynonyms()
	{
		TagSynonymsDict.Clear();
		foreach (string allTag in AllTags)
		{
			string text = allTag.GetLocalization();
			if (string.IsNullOrWhiteSpace(text) || string.Equals(text, allTag, StringComparison.Ordinal))
			{
				text = allTag.Replace("ba:itemtag_", string.Empty);
			}
			text = text.ToLowerInvariant().Replace(" ", string.Empty);
			List<string> list;
			if (text.Contains('|'))
			{
				string[] array = text.Split('|');
				list = new List<string>(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					list.Add(array[i].Trim());
				}
			}
			else
			{
				list = new List<string>(1) { text };
			}
			TagSynonymsDict[allTag] = list;
		}
	}

	private static void ApplyTagFilters()
	{
		TempMatchedItems.Clear();
		GetItemsByTags(ActiveItemTags, TempMatchedItems);
		filterByItemNamesCallback?.Invoke(TempMatchedItems);
		LastFilteredTags.Clear();
		foreach (string activeItemTag in ActiveItemTags)
		{
			LastFilteredTags.Add(activeItemTag);
		}
	}

	private static void FindMatchingTags(HashSet<string> tagsToSearch, string searchText)
	{
		TagMatches.Clear();
		foreach (string item2 in tagsToSearch)
		{
			var (num, item) = ScoreTagMatch(item2, searchText);
			if (num >= 0)
			{
				TagMatches.Add((item2, num, item));
			}
		}
		if (TagMatches.Count > 1)
		{
			TagMatches.Sort(((string Tag, int Score, string BestSynonym) a, (string Tag, int Score, string BestSynonym) b) => a.Score.CompareTo(b.Score));
		}
	}

	private static (int Score, string BestSynonym) ScoreTagMatch(string itemTag, string searchText)
	{
		if (!TagSynonymsDict.TryGetValue(itemTag, out var value))
		{
			return (Score: -1, BestSynonym: null);
		}
		int num = int.MaxValue;
		string item = null;
		for (int i = 0; i < value.Count; i++)
		{
			string text = value[i];
			int num2 = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
			if (num2 >= 0)
			{
				int num3 = text.Length - searchText.Length;
				int num4 = ((num2 != 0) ? (num3 + num2 * 100) : num3);
				if (num4 < num)
				{
					num = num4;
					item = text;
				}
			}
		}
		if (num >= int.MaxValue)
		{
			return (Score: -1, BestSynonym: null);
		}
		return (Score: num, BestSynonym: item);
	}

	private static void GetItemsByTags(HashSet<string> tags, HashSet<string> resultSet)
	{
		foreach (string tag in tags)
		{
			if (!ItemTagToItemNameDict.TryGetValue(tag, out var value))
			{
				continue;
			}
			foreach (string item in value)
			{
				resultSet.Add(item);
			}
		}
	}

	private static bool ItemHasIncludedTag(Item item, HashSet<string> includedSet)
	{
		if (includedSet == null || includedSet.Count == 0)
		{
			return true;
		}
		foreach (string item2 in includedSet)
		{
			if (item.HasTag(item2))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ItemHasExcludedTag(Item item, HashSet<string> excludedSet)
	{
		if (excludedSet == null || excludedSet.Count == 0)
		{
			return false;
		}
		foreach (string item2 in excludedSet)
		{
			if (item.HasTag(item2))
			{
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LastFilteredTags.Clear();
		AllTags.Clear();
		ActiveItemTags.Clear();
		TagMatches.Clear();
		TempMatchedItems.Clear();
		TagSynonymsDict.Clear();
		ItemTagToItemNameDict = null;
		CurrentCategoryToggle = null;
		showAllItems = null;
		showNoItems = null;
		updateAutocompleteCallback = null;
		filterByItemNamesCallback = null;
	}
}
