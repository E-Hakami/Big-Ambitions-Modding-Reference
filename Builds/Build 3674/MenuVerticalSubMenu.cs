using System.Collections.Generic;
using System.Linq;
using UI.Components.MenuVerticalCategorized;
using UnityEngine;

public class MenuVerticalSubMenu : MonoBehaviour
{
	[SerializeField]
	private SubCategoryButton subCategoryButtonPrefab;

	private readonly List<SubCategoryButton> _subCategoryButtons = new List<SubCategoryButton>();

	private MenuVertical _menu;

	public void SetUp(MenuVertical.Category category, MenuVertical menu)
	{
		DestroySubCategoryButtons();
		int num = 0;
		for (int i = 0; i < category.subCategories.Length; i++)
		{
			MenuVertical.SubCategory subCategory = category.subCategories[i];
			if (!menu.shouldShowCategory(subCategory.linkId))
			{
				subCategory.subCategoryId = -1;
				continue;
			}
			SubCategoryButton subCategoryButton = Object.Instantiate(subCategoryButtonPrefab, base.transform);
			subCategory.subCategoryId = num;
			subCategoryButton.SetUp(subCategory, OnSubCategoryClick);
			_subCategoryButtons.Add(subCategoryButton);
			num++;
		}
		if (category.subCategories.Length < 1)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		_menu = menu;
		base.transform.SetSiblingIndex(category.categoryId + 1);
		base.gameObject.SetActive(value: true);
		OnSubCategoryClick(category.subCategories.First((MenuVertical.SubCategory x) => x.subCategoryId == 0));
	}

	private void OnSubCategoryClick(MenuVertical.SubCategory subCategory)
	{
		for (int i = 0; i < _subCategoryButtons.Count; i++)
		{
			_subCategoryButtons[i].SetSelected(i == subCategory.subCategoryId);
		}
		_menu.onSubCategoryClick(subCategory.linkId);
	}

	private void DestroySubCategoryButtons()
	{
		foreach (SubCategoryButton subCategoryButton in _subCategoryButtons)
		{
			Object.Destroy(subCategoryButton.gameObject);
		}
		_subCategoryButtons.Clear();
	}
}
