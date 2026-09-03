using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigAmbitions.GameAnalytics;
using Buildings;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Newtonsoft.Json;
using TMPro;
using UI;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions.HelpSystem;

public class HelpSystem : InstanceBehavior<HelpSystem>
{
	private const string DefaultPageSlug = "businesstypes-giftshop";

	public HelpPageRenderer helpPageRenderer;

	public HelpCategory helpCategoryTemplate;

	public GameObject container;

	public GameObject dimmer;

	public TMP_InputField searchField;

	public UnityEvent<string> currentSlugChanged = new UnityEvent<string>();

	private readonly Stack<string> _backHistory = new Stack<string>();

	private readonly Stack<string> _forwardHistory = new Stack<string>();

	private readonly List<HelpCategory> _generatedHelpCategories = new List<HelpCategory>();

	private List<HelpStructureGroupEntry> _categories;

	private bool _isInitialized;

	private bool _suppressClearForwardHistory;

	private string currentSlug;

	public static bool IsVisible { get; private set; }

	public string CurrentSlug => currentSlug;

	private void Start()
	{
		searchField.onValueChanged.AddListener(Search);
		currentSlugChanged.AddListener(OnCurrentSlugChanged);
		container.SetActive(value: false);
		dimmer.SetActive(value: false);
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
	}

	protected override void OnDestroy()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(OnLanguageChanged));
		base.OnDestroy();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Mouse3))
		{
			HistoryBack();
		}
		if (Input.GetKeyDown(KeyCode.Mouse4))
		{
			HistoryForward();
		}
	}

	private void Search(string query)
	{
		bool flag = string.IsNullOrWhiteSpace(query);
		foreach (HelpCategory generatedHelpCategory in _generatedHelpCategories)
		{
			bool active = false;
			foreach (HelpPageLink link in generatedHelpCategory.links)
			{
				if (flag)
				{
					link.gameObject.SetActive(value: true);
					active = true;
					continue;
				}
				string value = query.ToLowerInvariant();
				string text = link.rawTranslatedKey.ToLowerInvariant();
				if (text.Contains(value))
				{
					link.gameObject.SetActive(value: true);
					active = true;
					int num = text.IndexOf(value, StringComparison.Ordinal);
					string[] obj = new string[5]
					{
						link.rawTranslatedKey.Substring(0, num),
						"<b>",
						link.rawTranslatedKey.Substring(num, query.Length),
						"</b>",
						null
					};
					string rawTranslatedKey = link.rawTranslatedKey;
					int num2 = num + query.Length;
					obj[4] = rawTranslatedKey.Substring(num2, rawTranslatedKey.Length - num2);
					string text2 = string.Concat(obj);
					link.languageChangeEvent.TextContainer.text = text2;
				}
				else
				{
					link.gameObject.SetActive(value: false);
				}
			}
			generatedHelpCategory.gameObject.SetActive(active);
			generatedHelpCategory.SetOpenState(!flag);
		}
		if (flag && currentSlug != null)
		{
			OpenLink(currentSlug);
		}
	}

	[ConsoleMethod("ReloadHelp", "Reloads the help structure", new string[] { })]
	private void ReloadHelpStructure()
	{
		if (!(helpCategoryTemplate == null))
		{
			string path = Path.Combine(Application.streamingAssetsPath, "helpstructure.json");
			_categories = JsonConvert.DeserializeObject<List<HelpStructureGroupEntry>>(File.ReadAllText(path));
			LoadCategories();
			_isInitialized = true;
		}
	}

	private void EnsureInitialized()
	{
		if (!_isInitialized)
		{
			ReloadHelpStructure();
		}
	}

	private void OnLanguageChanged()
	{
		if (_isInitialized)
		{
			ReloadHelpStructure();
		}
	}

	public void Toggle()
	{
		Toggle(!IsVisible);
	}

	public void Toggle(bool show)
	{
		Toggle(show, null);
	}

	public void Toggle(bool show, string linkSlug)
	{
		if (show)
		{
			EnsureInitialized();
			InstanceBehavior<GameManager>.Instance?.playerController?.SetNavigationBlocker(NavigationBlocker.HelpSystem);
		}
		else
		{
			InstanceBehavior<GameManager>.Instance?.playerController?.UnsetNavigationBlocker(NavigationBlocker.HelpSystem);
		}
		container.gameObject.SetActive(show);
		dimmer.SetActive(show);
		IsVisible = show;
		if (!show)
		{
			return;
		}
		string text = linkSlug;
		if (text == null)
		{
			string forcedHelpPageSlug = TutorialHelper.GetForcedHelpPageSlug();
			if (forcedHelpPageSlug != null && ExistsLinkSlug(forcedHelpPageSlug))
			{
				text = forcedHelpPageSlug;
			}
		}
		if (text != null)
		{
			OpenLink(text);
		}
		else if (currentSlug == null)
		{
			OpenLink("businesstypes-giftshop");
		}
		GameEvent.Invoke("ba:gameevent_helppageopened");
		GameAnalytics.TrackOpenHelpSystem();
		if (!searchField.text.IsNullOrEmpty())
		{
			Search(searchField.text);
		}
	}

	public bool ExistsLinkSlug(string linkSlug)
	{
		EnsureInitialized();
		if (_categories != null)
		{
			return _categories.Any((HelpStructureGroupEntry x) => x.Pages.Any((HelpStructurePageEntry y) => y.Slug == linkSlug));
		}
		return false;
	}

	private void LoadCategories()
	{
		if (helpCategoryTemplate == null || _categories == null)
		{
			return;
		}
		helpCategoryTemplate.transform.ResetTemplate();
		_generatedHelpCategories.Clear();
		foreach (HelpStructureGroupEntry helpCategoryEntry in _categories)
		{
			HelpCategory helpCategory = UnityEngine.Object.Instantiate(helpCategoryTemplate, helpCategoryTemplate.transform.parent);
			helpCategory.categoryNameLabel.Key = helpCategoryEntry.CategoryLocalizorKey;
			helpCategory.gameObject.SetActive(value: true);
			_generatedHelpCategories.Add(helpCategory);
			helpCategory.pageTemplate.transform.ResetTemplate();
			foreach (HelpStructurePageEntry helpPageEntry in helpCategoryEntry.Pages)
			{
				HelpPageLink helpPageLink = UnityEngine.Object.Instantiate(helpCategory.pageTemplate, helpCategory.pageTemplate.transform.parent);
				helpPageLink.languageChangeEvent.Key = helpPageEntry.PageLocalizorKeyPrefix;
				helpPageLink.linkSlug = helpPageEntry.Slug;
				helpPageLink.rawTranslatedKey = helpPageLink.languageChangeEvent.TextContainer.text;
				helpCategory.links.Add(helpPageLink);
				helpPageLink.gameObject.SetActive(value: true);
				helpPageLink.GetComponent<Button>().onClick.AddListener(delegate
				{
					helpPageRenderer.RenderPage(helpCategoryEntry, helpPageEntry);
				});
			}
		}
	}

	public void OpenLink(string slug)
	{
		EnsureInitialized();
		if (slug.StartsWith("address:"))
		{
			if (InteriorDesignerUI.IsOpen)
			{
				Notifications.ShowError("notification_citymap_interiordesigner_open");
				return;
			}
			Building building = BuildingHelper.GetBuilding(BuildingHelper.ParseAddressString(slug.Substring("address:".Length)));
			CityBuildingController buildingController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(building.Address);
			InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
			InstanceBehavior<CityManager>.Instance.cityMap.FocusOnBuilding(buildingController);
			Toggle(show: false);
			return;
		}
		bool flag = false;
		foreach (HelpCategory generatedHelpCategory in _generatedHelpCategories)
		{
			foreach (HelpPageLink link in generatedHelpCategory.links)
			{
				if (link.linkSlug == slug)
				{
					link.GetComponent<Button>().onClick.Invoke();
					generatedHelpCategory.SetOpenState(isOpen: true);
					flag = true;
				}
			}
		}
		if (!flag)
		{
			helpPageRenderer.RenderPage(slug);
		}
	}

	private void OnCurrentSlugChanged(string slug)
	{
		if (slug == currentSlug)
		{
			return;
		}
		foreach (HelpPageLink item in _generatedHelpCategories.SelectMany((HelpCategory c) => c.links))
		{
			item.SetSelectedState(item.linkSlug == slug);
		}
		currentSlug = slug;
		if (_suppressClearForwardHistory)
		{
			_suppressClearForwardHistory = false;
		}
		else
		{
			_forwardHistory.Clear();
		}
		_backHistory.Push(slug);
		if (IsVisible)
		{
			GameEvent.Invoke("ba:gameevent_helppageopened");
		}
	}

	public void HistoryBack()
	{
		if (_backHistory.Count > 1)
		{
			_forwardHistory.Push(_backHistory.Pop());
			string slug = _backHistory.Pop();
			_suppressClearForwardHistory = true;
			OpenLink(slug);
		}
	}

	public void HistoryForward()
	{
		if (!_forwardHistory.IsEmpty())
		{
			_backHistory.Push(currentSlug);
			string slug = _forwardHistory.Pop();
			_suppressClearForwardHistory = true;
			OpenLink(slug);
		}
	}
}
