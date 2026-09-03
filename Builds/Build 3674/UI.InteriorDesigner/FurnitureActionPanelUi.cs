using System;
using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class FurnitureActionPanelUi : ActionPanelUI
{
	[SerializeField]
	private IDItemTemplateScrollingController scrollingController;

	[SerializeField]
	private UI.Components.InputField searchField;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private Toggle showAllItemsToggle;

	[SerializeField]
	private Toggle showFavoriteItemsToggle;

	[SerializeField]
	private GameObject noFavoritesLabel;

	[SerializeField]
	private List<string> visibleTagsInFurnitureTool = new List<string>();

	private readonly List<string> _availableItemNames = new List<string>();

	public Action<string> placeItem;

	private bool _isInitialized;

	private bool _isFirstOpen = true;

	public override ToolName[] ToolNames => new ToolName[1] { ToolName.Furniture };

	private void Awake()
	{
		if (_isInitialized)
		{
			return;
		}
		ValidateTags();
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if (allItem.HasTag(TagRef.Itemtag.dev))
			{
				if (GameManager.IsDevMode)
				{
					_availableItemNames.Add(allItem.itemName);
				}
			}
			else if (allItem.canBeGrabbed && allItem.tagIndexes.Count != 0 && (visibleTagsInFurnitureTool.Count <= 0 || HasAnyVisibleTag(allItem)))
			{
				_availableItemNames.Add(allItem.itemName);
			}
		}
		scrollingController.SetUp(ShowFavoriteItems, () => showFavoriteItemsToggle.isOn);
		noFavoritesLabel.SetActive(value: false);
		_isInitialized = true;
	}

	private void Start()
	{
		showAllItemsToggle.onValueChanged.AddListener(delegate(bool isOn)
		{
			if (isOn)
			{
				ShowAllItems();
			}
		});
		showFavoriteItemsToggle.onValueChanged.AddListener(delegate(bool isOn)
		{
			if (isOn)
			{
				ShowFavoriteItems();
			}
		});
		searchField.tmpInputField.onValueChanged.AddListener(scrollingController.FilterReload);
		FurnitureItemSearch search = scrollingController.search;
		search.onAutocompleteCalculated = (Action<string>)Delegate.Combine(search.onAutocompleteCalculated, new Action<string>(SetAutocomplete));
		UI.Components.InputField inputField = searchField;
		inputField.onAutoCompleteConfirm = (Action<string>)Delegate.Combine(inputField.onAutoCompleteConfirm, new Action<string>(scrollingController.search.ApplyAutocomplete));
	}

	public override void OnOpen()
	{
		if (!_isInitialized)
		{
			Awake();
		}
		if (_isFirstOpen)
		{
			scrollingController.LoadList(_availableItemNames, visibleTagsInFurnitureTool, SelectButton);
			showAllItemsToggle.SetIsOnWithoutNotify(value: true);
			_isFirstOpen = false;
		}
		base.gameObject.SetActive(value: true);
	}

	private void ShowAllItems()
	{
		FurnitureCategoryToggle.CurrentActiveToggle = null;
		searchField.tmpInputField.SetTextWithoutNotify(string.Empty);
		noFavoritesLabel.SetActive(value: false);
		scrollingController.ClearFilterAndReload();
	}

	private void ShowFavoriteItems()
	{
		FurnitureCategoryToggle.CurrentActiveToggle = null;
		searchField.tmpInputField.SetTextWithoutNotify(string.Empty);
		HashSet<string> iDFurnitureFavorites = PlayerSettingsHelper.GetIDFurnitureFavorites();
		bool flag = iDFurnitureFavorites.Count == 0;
		noFavoritesLabel.SetActive(flag);
		if (flag)
		{
			scrollingController.ShowNoItems();
		}
		else
		{
			scrollingController.FilterReload(iDFurnitureFavorites);
		}
	}

	public void ShowCategory(List<string> includedTags, List<string> excludedTags)
	{
		noFavoritesLabel.SetActive(value: false);
		searchField.tmpInputField.SetTextWithoutNotify(string.Empty);
		scrollingController.FilterReload(includedTags, excludedTags);
	}

	private bool HasAnyVisibleTag(Item item)
	{
		for (int i = 0; i < visibleTagsInFurnitureTool.Count; i++)
		{
			if (item.HasTag(visibleTagsInFurnitureTool[i]))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}

	public override void OnEnterInteriorDesignerMode()
	{
	}

	private void SelectButton(IDItemTemplateBase selectedButton, string itemName)
	{
		placeItem?.Invoke(itemName);
	}

	private void SetAutocomplete(string autocompleteText)
	{
		searchField.SetAutocompleteValue(autocompleteText);
	}

	private void ValidateTags()
	{
		for (int i = 0; i < visibleTagsInFurnitureTool.Count; i++)
		{
			string value = visibleTagsInFurnitureTool[i];
			if (string.IsNullOrWhiteSpace(value) || TagDatabaseHelper.TryGetTagIndex(value, "ItemTagDatabase") == -1)
			{
				visibleTagsInFurnitureTool.RemoveAt(i);
			}
		}
	}
}
