using System;
using System.Collections.Generic;
using System.Linq;
using UI.Components.MenuVerticalCategorized;
using UnityEngine;

public class MenuVertical : MonoBehaviour
{
	[Serializable]
	public class Category
	{
		[HideInInspector]
		public int categoryId;

		public string linkId;

		public Sprite icon;

		public SubCategory[] subCategories;
	}

	[Serializable]
	public class SubCategory
	{
		[HideInInspector]
		public int subCategoryId;

		public string linkId;

		public Sprite icon;
	}

	[Header("Prefab")]
	[SerializeField]
	private CategoryButton categoryButtonPrefab;

	[Header("Scene References")]
	[SerializeField]
	private MenuVerticalSubMenu subMenu;

	[Header("Settings")]
	public Category[] categories;

	private readonly List<CategoryButton> _categoryButtons = new List<CategoryButton>();

	public Action<string> onCategoryClick;

	public Action<string> onSubCategoryClick;

	public Func<string, bool> shouldShowCategory;

	private int _selectedCategoryId;

	public bool HasMultipleCategories
	{
		get
		{
			if (categories.Length <= 1)
			{
				return categories.Sum((Category x) => x.subCategories.Length) > 1;
			}
			return true;
		}
	}

	private void Start()
	{
		Reset();
	}

	public void Reset()
	{
		CreateCategoryButtons();
		subMenu.gameObject.SetActive(value: false);
	}

	private void CreateCategoryButtons()
	{
		DestroyCategoryButtons();
		int num = 0;
		for (int i = 0; i < categories.Length; i++)
		{
			Category category = categories[i];
			if (shouldShowCategory != null)
			{
				if (!string.IsNullOrEmpty(category.linkId))
				{
					if (!shouldShowCategory(category.linkId))
					{
						category.categoryId = -1;
						continue;
					}
				}
				else if (category.subCategories.Length != 0 && category.subCategories.All((SubCategory x) => !shouldShowCategory(x.linkId)))
				{
					category.categoryId = -1;
					continue;
				}
			}
			CategoryButton categoryButton = UnityEngine.Object.Instantiate(categoryButtonPrefab, base.transform);
			category.categoryId = num;
			categoryButton.SetUp(category, OnCategoryButtonClick);
			if (_categoryButtons.Count == 0)
			{
				categoryButton.SetSelected(isSelected: true);
			}
			_categoryButtons.Add(categoryButton);
			num++;
		}
	}

	private void DestroyCategoryButtons()
	{
		foreach (CategoryButton categoryButton in _categoryButtons)
		{
			UnityEngine.Object.Destroy(categoryButton.gameObject);
		}
		_categoryButtons.Clear();
	}

	public void OnCategoryButtonClick(Category category)
	{
		if (_selectedCategoryId == category.categoryId && subMenu.gameObject.activeSelf)
		{
			subMenu.gameObject.SetActive(value: false);
			_categoryButtons[category.categoryId].SetSelected(isSelected: false);
			return;
		}
		_selectedCategoryId = category.categoryId;
		for (int i = 0; i < _categoryButtons.Count; i++)
		{
			_categoryButtons[i].SetSelected(i == category.categoryId);
		}
		Category category2 = categories.FirstOrDefault((Category x) => x.categoryId == category.categoryId);
		subMenu.SetUp(category2, this);
		onCategoryClick?.Invoke(category.linkId);
	}
}
