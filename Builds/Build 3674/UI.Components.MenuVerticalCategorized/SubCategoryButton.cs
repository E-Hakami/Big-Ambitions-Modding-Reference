using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components.MenuVerticalCategorized;

public class SubCategoryButton : SelectableButton
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image iconImage;

	private MenuVertical.SubCategory _subCategory;

	public void SetUp(MenuVertical.SubCategory subCategory, Action<MenuVertical.SubCategory> onClick)
	{
		_subCategory = subCategory;
		button.onClick.AddListener(delegate
		{
			onClick(_subCategory);
		});
		iconImage.sprite = _subCategory.icon;
	}
}
