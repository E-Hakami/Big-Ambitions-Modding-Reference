using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BigAmbitions.ModsInternal;
using Helpers;
using Localizor.LanguageChangeEvent;
using Steamworks;
using UI.Components;
using UnityEngine;

namespace BigAmbitions;

public class ModsPanel : MonoBehaviour
{
	private const int ModsPerRow = 2;

	private const string ForumLink = "https://community.bigambitionsgame.com/";

	private const string GitHubLink = "https://github.com/hovgaardgames/bigambitions";

	private static readonly string SteamWorkshopModsLink = "https://steamcommunity.com/workshop/browse/?appid=1331550&requiredtags[]=mod";

	[SerializeField]
	private ModsRow modRowTemplate;

	[SerializeField]
	private RectTransform rowsParent;

	[SerializeField]
	private RectTransform spinnerTarget;

	[SerializeField]
	private InputField searchBar;

	[SerializeField]
	private TextLocalizationComponent noModsText;

	[SerializeField]
	private string helpSlug;

	private readonly List<string> _allModTitlesLower = new List<string>();

	private readonly List<ModInfo> _filteredMods = new List<ModInfo>();

	private readonly List<ModsRow> _shownModRows = new List<ModsRow>();

	private List<ModInfo> _allMods = new List<ModInfo>();

	private string _currentSearch = "";

	private bool _isRefreshing;

	private Coroutine _refreshCoroutine;

	private void OnEnable()
	{
		if (searchBar != null && searchBar.tmpInputField != null)
		{
			searchBar.tmpInputField.onValueChanged.AddListener(OnSearchChanged);
		}
		Refresh();
	}

	private void OnDisable()
	{
		if (_isRefreshing)
		{
			if (_refreshCoroutine != null)
			{
				StopCoroutine(_refreshCoroutine);
			}
			LoadingSpinner.Hide();
			_isRefreshing = false;
		}
		if (searchBar != null && searchBar.tmpInputField != null)
		{
			searchBar.tmpInputField.onValueChanged.RemoveListener(OnSearchChanged);
		}
	}

	public void OpenModsOnSteam()
	{
		SteamFriends.OpenWebOverlay(SteamWorkshopModsLink);
	}

	public void Refresh()
	{
		if (!_isRefreshing)
		{
			_refreshCoroutine = StartCoroutine(RefreshCoroutine());
		}
	}

	public void OpenForum()
	{
		Application.OpenURL("https://community.bigambitionsgame.com/");
	}

	public void OpenGitHub()
	{
		Application.OpenURL("https://github.com/hovgaardgames/bigambitions");
	}

	private IEnumerator RefreshCoroutine()
	{
		if (!_isRefreshing)
		{
			_isRefreshing = true;
			LoadingSpinner.Show(spinnerTarget, 0.5f);
			ModThumbnailLoader.ClearCache();
			Task<List<ModInfo>> loadTask = ModDiscoveryRegistry.GetAllModsAsync();
			while (!loadTask.IsCompleted)
			{
				yield return null;
			}
			try
			{
				_allMods = loadTask.Result ?? new List<ModInfo>();
			}
			catch
			{
				_allMods = new List<ModInfo>();
			}
			noModsText.gameObject.SetActive(_allMods.Count == 0);
			if (noModsText.gameObject.activeSelf)
			{
				noModsText.Key = ((!SteamHelper.IsConnectedToSteam()) ? "blueprint_list_no_results_connection" : "main_menu_mods_no_mods_subscribed");
			}
			RefreshActiveModEnums();
			RebuildTitleCache();
			ApplyFilterAndDraw();
			LoadingSpinner.Hide();
			_isRefreshing = false;
		}
	}

	private void OnSearchChanged(string searchText)
	{
		_currentSearch = searchText ?? "";
		ApplyFilterAndDraw();
	}

	private void RebuildTitleCache()
	{
		_allModTitlesLower.Clear();
		_allModTitlesLower.Capacity = Mathf.Max(_allModTitlesLower.Capacity, _allMods.Count);
		for (int i = 0; i < _allMods.Count; i++)
		{
			string text = _allMods[i]?.title ?? "";
			_allModTitlesLower.Add(text.ToLowerInvariant());
		}
	}

	private void ApplyFilterAndDraw()
	{
		string currentSearch = _currentSearch;
		if (string.IsNullOrWhiteSpace(currentSearch))
		{
			DrawRows(_allMods);
			return;
		}
		string value = currentSearch.ToLowerInvariant();
		_filteredMods.Clear();
		for (int i = 0; i < _allMods.Count; i++)
		{
			if (_allModTitlesLower[i].IndexOf(value, StringComparison.Ordinal) >= 0)
			{
				_filteredMods.Add(_allMods[i]);
			}
		}
		DrawRows(_filteredMods);
	}

	private void DrawRows(List<ModInfo> mods)
	{
		_shownModRows.Clear();
		for (int num = rowsParent.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(rowsParent.GetChild(num).gameObject);
		}
		rowsParent.gameObject.SetActive(value: true);
		for (int i = 0; i < mods.Count; i += 2)
		{
			ModsRow modsRow = UnityEngine.Object.Instantiate(modRowTemplate, rowsParent);
			List<ModInfo> list = new List<ModInfo>(2) { mods[i] };
			if (i + 1 < mods.Count)
			{
				list.Add(mods[i + 1]);
			}
			modsRow.Setup(list, UpdateConflicts);
			_shownModRows.Add(modsRow);
		}
	}

	private void UpdateConflicts()
	{
		RefreshActiveModEnums();
		foreach (ModsRow shownModRow in _shownModRows)
		{
			shownModRow.UpdateConflicts();
		}
	}

	private void RefreshActiveModEnums()
	{
		ModEnumDefinitions.Clear();
		foreach (ModInfo allMod in _allMods)
		{
			bool isActive = allMod.steamItemId == 0L || ModManifest.Contains(allMod.steamItemId);
			ModEnumDefinitions.RegisterModEnums(allMod, isActive);
		}
	}
}
