using System;
using System.Collections;
using System.Threading.Tasks;
using Extensions;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintsUI;

public class BlueprintFilterUI : MonoBehaviour
{
	[SerializeField]
	private Transform sortByFilter;

	[SerializeField]
	private Transform sortByOptionTemplate;

	[SerializeField]
	private Transform filterTemplate;

	[SerializeField]
	private Transform searchTransform;

	[SerializeField]
	private TMP_InputField searchInputField;

	[SerializeField]
	[Range(0.01f, 2f)]
	private float searchDebounceDelayInSeconds = 0.5f;

	private Action _loadPageOne;

	private Action _onSearchQueryChanged;

	private Toggle _popularityToggle;

	private Coroutine _debounceCoroutine;

	private BlueprintCategory _currentCategory;

	public BlueprintSortInfo SortInfo { get; private set; }

	public void SetUp(Func<int, Task> loadPage, Action onSearchQueryChanged)
	{
		_loadPageOne = delegate
		{
			loadPage?.Invoke(1);
		};
		_onSearchQueryChanged = onSearchQueryChanged;
		searchInputField.onValueChanged.AddListener(OnSearchQueryChanged);
		sortByFilter.GetButtonByName("Label").onClick.AddListener(delegate
		{
			ChangeFilterCollapseState(sortByFilter);
		});
		searchTransform.GetButtonByName("Label").onClick.AddListener(delegate
		{
			ChangeFilterCollapseState(searchTransform);
		});
	}

	public void OnOpen(BlueprintCategory category)
	{
		_currentCategory = category;
		searchTransform.gameObject.SetActive(category != BlueprintCategory.Gallery);
		SortInfo = new BlueprintSortInfo();
		searchInputField.text = SortInfo.SearchQuery;
		_popularityToggle.SetIsOnWithoutNotify(value: true);
	}

	public void InitListeners()
	{
		LoadSortBy();
	}

	private void LoadSortBy()
	{
		sortByOptionTemplate.ResetTemplate();
		foreach (SortByOption value in Enum.GetValues(typeof(SortByOption)))
		{
			SetUpSortByOption(value);
		}
	}

	private void SetUpSortByOption(SortByOption sortByOption)
	{
		Transform obj = sortByOptionTemplate.CreateElement();
		obj.GetLanguageChangeEventByName("Label").Key = sortByOption.GetLocalizeKey();
		Toggle component = obj.GetComponent<Toggle>();
		component.onValueChanged.AddListener(delegate(bool toggled)
		{
			if (toggled)
			{
				SortInfo.SetSortByOption(sortByOption);
				_loadPageOne();
			}
		});
		if (sortByOption == SortByOption.Popularity)
		{
			_popularityToggle = component;
		}
	}

	internal void ResetSortInfo()
	{
		CancelInvoke("_onSearchQueryChanged");
		SortInfo = new BlueprintSortInfo();
		LoadFilters();
		LoadSortBy();
	}

	public void LoadFilters()
	{
		filterTemplate.ResetTemplate();
		SortInfo.SetBuildingTypeFilter(BlueprintFilterHelper.GetBuildingTypeFilter());
		SetUpFilter(SortInfo.BuildingTypeFilter);
		SortInfo.SetBuildingSizeFilter(BlueprintFilterHelper.GetBuildingSizeFilter());
		SetUpFilter(SortInfo.BuildingSizeFilter);
		SortInfo.SetBusinessTypeFilter(BlueprintFilterHelper.GetBusinessTypeFilter());
		SetUpFilter(SortInfo.BusinessTypeFilter);
		if (_currentCategory == BlueprintCategory.Gallery || _currentCategory == BlueprintCategory.MyLibrary)
		{
			SortInfo.SetBuildVersionFilter(BlueprintFilterHelper.GetBuildVersionFilter());
			SetUpFilter(SortInfo.BuildVersionFilter);
		}
	}

	private void SetUpFilter(BlueprintFilter filter)
	{
		Transform filterTransform = filterTemplate.CreateElement();
		filterTransform.GetLanguageChangeEventByName("Label").Key = filter.localizationKey;
		filterTransform.GetButtonByName("Label").onClick.AddListener(delegate
		{
			ChangeFilterCollapseState(filterTransform);
		});
		Transform filterOptionTemplate = filterTransform.Find("Options").Find("FilterOptionTemplate");
		filterOptionTemplate.ResetTemplate();
		foreach (BlueprintFilterOption filterOption in filter.filterOptions)
		{
			Transform transform = filterOptionTemplate.CreateElement();
			if (filterOption.localizeText)
			{
				transform.GetLanguageChangeEventByName("Label").Key = filterOption.text;
			}
			else
			{
				transform.GetLabelByName("Label").text = filterOption.text;
			}
			Toggle toggle = transform.GetComponent<Toggle>();
			if (filterOption is BlueprintAllFilterOption)
			{
				filter.allFilterToggle = toggle;
			}
			toggle.onValueChanged.AddListener(delegate(bool toggled)
			{
				SortInfo.HasChanged = true;
				if (!toggled && GetToggledOptionCount(filter) <= 1)
				{
					if (filterOption is BlueprintAllFilterOption)
					{
						toggle.SetIsOnWithoutNotify(value: true);
					}
					else
					{
						filterOption.toggled = false;
						filter.allFilterToggle.isOn = true;
					}
				}
				else
				{
					filterOption.toggled = toggled;
					if (!toggled)
					{
						_loadPageOne();
					}
					else if (filterOption is BlueprintAllFilterOption)
					{
						Toggle[] componentsInChildren = filterOptionTemplate.parent.GetComponentsInChildren<Toggle>();
						foreach (Toggle toggle2 in componentsInChildren)
						{
							if (!(toggle2 == filter.allFilterToggle))
							{
								toggle2.SetIsOnWithoutNotify(value: false);
							}
						}
						foreach (BlueprintFilterOption filterOption2 in filter.filterOptions)
						{
							if (!(filterOption2 is BlueprintAllFilterOption))
							{
								filterOption2.toggled = false;
							}
						}
						_loadPageOne();
					}
					else if (filter.allFilterToggle.isOn)
					{
						filter.allFilterToggle.isOn = false;
					}
					else
					{
						_loadPageOne();
					}
				}
			});
			toggle.SetIsOnWithoutNotify(filterOption.toggled);
		}
	}

	private static int GetToggledOptionCount(BlueprintFilter filter)
	{
		int num = 0;
		foreach (BlueprintFilterOption filterOption in filter.filterOptions)
		{
			if (filterOption.toggled)
			{
				num++;
			}
		}
		return num;
	}

	private static void ChangeFilterCollapseState(Transform filterTransform)
	{
		GameObject obj = filterTransform.Find("Label").Find("Collapsed").gameObject;
		GameObject gameObject = filterTransform.Find("Label").Find("Uncollapsed").gameObject;
		bool flag = !obj.gameObject.activeSelf;
		obj.SetActive(flag);
		gameObject.SetActive(!flag);
		filterTransform.Find("Options").gameObject.SetActive(!flag);
	}

	private void OnSearchQueryChanged(string query)
	{
		SortInfo.SetSearchQuery(query);
		if (_debounceCoroutine != null)
		{
			StopCoroutine(_debounceCoroutine);
		}
		if (base.gameObject.activeInHierarchy)
		{
			_debounceCoroutine = StartCoroutine(DebounceAndFire(searchDebounceDelayInSeconds));
		}
	}

	private IEnumerator DebounceAndFire(float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		_onSearchQueryChanged?.Invoke();
	}
}
