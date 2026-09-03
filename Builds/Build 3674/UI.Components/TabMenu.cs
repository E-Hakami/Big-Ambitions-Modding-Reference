using System.Collections.Generic;
using JimmysUnityUtilities;
using UnityEngine;

namespace UI.Components;

public class TabMenu : MonoBehaviour
{
	private int _selectedTabIndex = -1;

	[SerializeField]
	private TabMenuButton tabButtonPrefab;

	[SerializeField]
	private Transform tabsParent;

	[SerializeField]
	private Transform containersParent;

	[SerializeField]
	private SplitterIndicator splitterIndicator;

	public List<string> tabsLocalizationKeys = new List<string>();

	private readonly List<string> _containerNames = new List<string>();

	private readonly List<TabMenuButton> _allTabs = new List<TabMenuButton>();

	private void OnEnable()
	{
		_containerNames.Clear();
		foreach (Transform item in containersParent)
		{
			_containerNames.Add(item.name);
		}
		tabsParent.DestroyAllChildren();
		_allTabs.Clear();
		for (int i = 0; i < tabsLocalizationKeys.Count; i++)
		{
			TabMenuButton tab = Object.Instantiate(tabButtonPrefab, tabsParent);
			tab.SetUp(tabsLocalizationKeys[i], i, _containerNames[i]);
			tab.button.onClick.AddListener(delegate
			{
				SelectTab(tab.index);
			});
			_allTabs.Add(tab);
		}
		SelectTab(0);
	}

	public void SelectTab(string containerName)
	{
		if (string.IsNullOrEmpty(containerName) || !_containerNames.Contains(containerName))
		{
			Debug.LogWarning("Couldn't find tab with name " + containerName);
		}
		else
		{
			SelectTab(_containerNames.IndexOf(containerName));
		}
	}

	public void SelectTab(int tabIndex)
	{
		if (_selectedTabIndex == tabIndex || tabIndex < 0)
		{
			return;
		}
		TabMenuButton tabMenuButton = _allTabs[tabIndex];
		if (tabMenuButton == null)
		{
			return;
		}
		DeselectTab(_selectedTabIndex);
		tabMenuButton.text.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		splitterIndicator.Set(tabMenuButton.GetRectTransform());
		string text = _containerNames[tabIndex];
		foreach (Transform item in containersParent.transform)
		{
			item.gameObject.SetActive(item.name == text);
		}
		_selectedTabIndex = tabIndex;
	}

	private void DeselectTab(int tabIndex)
	{
		if (tabIndex >= 0 && tabIndex <= _allTabs.Count - 1)
		{
			_allTabs[tabIndex].text.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;
		}
	}
}
