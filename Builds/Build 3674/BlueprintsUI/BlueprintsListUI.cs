using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blueprints;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintsUI;

public class BlueprintsListUI : MonoBehaviour
{
	public const int MaxNumberOnPage = 16;

	private static readonly Dictionary<BlueprintCategory, bool> IsLoadingDict = new Dictionary<BlueprintCategory, bool>();

	private CancellationTokenSource _loadCts;

	[SerializeField]
	private BlueprintFilterUI filterUI;

	[SerializeField]
	private SelectedBlueprintUI selectedBlueprintUI;

	[SerializeField]
	private Transform blueprintTemplate;

	[SerializeField]
	private GameObject uploadToWorkshopConfirm;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private RectTransform loadingSpinnerRectTransform;

	[SerializeField]
	private TextLocalizationComponent noResultsLabel;

	[Header("Pages")]
	[SerializeField]
	private TextLocalizationComponent pageLabel;

	[SerializeField]
	private Button nextPageButton;

	[SerializeField]
	private Button previousPageButton;

	private readonly List<Blueprint> _cachedBlueprints = new List<Blueprint>();

	private static BlueprintCategory BlueprintCategory;

	private int _currentPage;

	private float _lastSearchTime;

	private bool _initialized;

	private int _totalPages;

	public static BlueprintLibraryController LibraryController { get; private set; }

	public static BlueprintFeedbackController FeedbackController { get; private set; }

	private static BlueprintGalleryController GalleryController { get; set; }

	private static BlueprintBusinessLayoutsController BusinessLayoutsController { get; set; }

	private static BlueprintInteriorDesignsController InteriorDesignsController { get; set; }

	public bool IsSelectedBlueprintPanelOpen => selectedBlueprintUI.IsOpen;

	public bool IsWorkshopConfirmPanelOpen => uploadToWorkshopConfirm.activeSelf;

	public static BlueprintController Controller => BlueprintCategory switch
	{
		BlueprintCategory.Gallery => GalleryController, 
		BlueprintCategory.MyLibrary => LibraryController, 
		BlueprintCategory.DevBusinessLayouts => BusinessLayoutsController, 
		BlueprintCategory.DevInteriorDesigns => InteriorDesignsController, 
		BlueprintCategory.Feedback => FeedbackController, 
		_ => throw new ArgumentOutOfRangeException("BlueprintCategory", BlueprintCategory, null), 
	};

	public void Open(BlueprintCategory blueprintCategory)
	{
		BlueprintCategory = blueprintCategory;
		if (IsLoadingDict.ContainsKey(blueprintCategory) && IsLoadingDict[blueprintCategory])
		{
			LoadingSpinner.Show(loadingSpinnerRectTransform);
			return;
		}
		LoadingSpinner.Hide();
		CancelCurrentLoad();
		blueprintTemplate.ResetTemplate();
		Init();
		filterUI.OnOpen(blueprintCategory);
		filterUI.LoadFilters();
		LoadPage(1, filterUI.LoadFilters);
		base.gameObject.SetActive(value: true);
	}

	private void Init()
	{
		if (!_initialized)
		{
			GalleryController = new BlueprintGalleryController();
			LibraryController = new BlueprintLibraryController();
			BusinessLayoutsController = new BlueprintBusinessLayoutsController();
			InteriorDesignsController = new BlueprintInteriorDesignsController();
			FeedbackController = new BlueprintFeedbackController();
			filterUI.SetUp((int page) => LoadPage(page), OnSearch);
			filterUI.InitListeners();
			selectedBlueprintUI.reloadBlueprints = delegate
			{
				ReloadBlueprints(clearCaches: true);
			};
			_initialized = true;
		}
	}

	private void OnSearch()
	{
		LoadPage(1);
	}

	private void Update()
	{
		if (GameManager.IsDevMode && !LoadingSpinner.isLoading && Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl))
		{
			if (Controller is BlueprintDevController blueprintDevController)
			{
				blueprintDevController.devBlueprints.Clear();
			}
			ReloadBlueprints(clearCaches: true);
		}
	}

	private void ShowBlueprints(List<Blueprint> blueprints)
	{
		foreach (Blueprint blueprint in blueprints)
		{
			blueprint.isHidden = false;
			BlueprintElementUI component = blueprintTemplate.CreateElement().GetComponent<BlueprintElementUI>();
			component.onShowElementInfo = SelectBlueprint;
			component.Display(blueprint);
		}
	}

	public async Task ReloadBlueprints(bool clearCaches)
	{
		if (clearCaches)
		{
			GalleryController.ClearCache();
			LibraryController.ClearCache();
			FeedbackController.ClearCache();
			BusinessLayoutsController.ClearCache();
			InteriorDesignsController.ClearCache();
		}
		selectedBlueprintUI.Close();
		filterUI.ResetSortInfo();
		await LoadPage(1);
	}

	private void CleanCachedBlueprints()
	{
		if (_cachedBlueprints.Count == 0)
		{
			return;
		}
		foreach (Blueprint cachedBlueprint in _cachedBlueprints)
		{
			cachedBlueprint.CleanCachedThumbnail();
			cachedBlueprint.isHidden = true;
		}
		_cachedBlueprints.Clear();
	}

	private void SelectBlueprint(Blueprint blueprint)
	{
		selectedBlueprintUI.Show(blueprint);
	}

	public void Close()
	{
		CleanCachedBlueprints();
		blueprintTemplate.ResetTemplate();
		selectedBlueprintUI.Close();
		LoadingSpinner.Hide();
		CancelCurrentLoad();
		uploadToWorkshopConfirm.SetActive(value: false);
		base.gameObject.SetActive(value: false);
	}

	public void CloseWorkshopConfirm()
	{
		uploadToWorkshopConfirm.SetActive(value: false);
	}

	public void CloseBlueprintInfo()
	{
		selectedBlueprintUI.Close();
	}

	public void NextPage()
	{
		if (_totalPages == -1 || _currentPage + 1 <= _totalPages)
		{
			LoadPage(_currentPage + 1);
		}
	}

	public void PreviousPage()
	{
		if (_currentPage - 1 > 0)
		{
			LoadPage(_currentPage - 1);
		}
	}

	private async Task LoadPage(int pageNo, Action onPageLoaded = null)
	{
		BlueprintCategory category = BlueprintCategory;
		if ((IsLoadingDict.TryGetValue(category, out var value) & value) || (!filterUI.SortInfo.HasChanged && Time.time - _lastSearchTime < 1f && pageNo == _currentPage))
		{
			return;
		}
		IsLoadingDict[category] = true;
		try
		{
			CancelCurrentLoad();
			_loadCts = new CancellationTokenSource();
			CancellationToken token = _loadCts.Token;
			blueprintTemplate.ResetTemplate();
			LoadingSpinner.Show(loadingSpinnerRectTransform);
			await Task.Delay(50, token);
			List<Blueprint> list = await Controller.GetBlueprints(pageNo, filterUI.SortInfo);
			if (token.IsCancellationRequested)
			{
				HandleNoResultsLabel(list.Count);
				_lastSearchTime = Time.time;
				return;
			}
			CleanCachedBlueprints();
			_cachedBlueprints.AddRange(list);
			_totalPages = Controller.GetMaxPageNumber();
			_currentPage = pageNo;
			HandleNoResultsLabel(list.Count);
			ShowBlueprints(_cachedBlueprints);
			HandleButtonStates();
			onPageLoaded?.Invoke();
		}
		finally
		{
			_lastSearchTime = Time.time;
			LoadingSpinner.Hide();
			IsLoadingDict[category] = false;
		}
	}

	private void HandleButtonStates()
	{
		nextPageButton.gameObject.SetActive(_totalPages == -1 || _currentPage + 1 <= _totalPages);
		previousPageButton.gameObject.SetActive(_currentPage - 1 > 0);
		scrollRect.normalizedPosition = new Vector2(0f, 1f);
		int currentPage = _currentPage;
		pageLabel.Arguments = new
		{
			pageNo = currentPage
		};
	}

	private void HandleNoResultsLabel(int blueprintsCount)
	{
		if (blueprintsCount == 0)
		{
			pageLabel.gameObject.SetActive(value: false);
			nextPageButton.gameObject.SetActive(value: false);
			previousPageButton.gameObject.SetActive(value: false);
			noResultsLabel.gameObject.SetActive(value: true);
			if (filterUI.SortInfo.HasChanged)
			{
				noResultsLabel.Key = "blueprint_list_no_results_filter";
			}
			else if (!SteamHelper.IsConnectedToSteam())
			{
				noResultsLabel.Key = "blueprint_list_no_results_connection";
			}
			else
			{
				noResultsLabel.Key = "blueprint_list_no_results_other";
			}
		}
		else
		{
			noResultsLabel.gameObject.SetActive(value: false);
			pageLabel.gameObject.SetActive(value: true);
			nextPageButton.gameObject.SetActive(value: true);
			previousPageButton.gameObject.SetActive(value: true);
		}
	}

	private void CancelCurrentLoad()
	{
		CancellationTokenSource loadCts = _loadCts;
		if (loadCts != null && !loadCts.IsCancellationRequested)
		{
			_loadCts.Cancel();
			_loadCts.Dispose();
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsLoadingDict.Clear();
	}
}
