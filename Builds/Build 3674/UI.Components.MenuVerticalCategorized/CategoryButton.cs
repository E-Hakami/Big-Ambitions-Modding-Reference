using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components.MenuVerticalCategorized;

public class CategoryButton : SelectableButton
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image iconImage;

	private MenuVertical.Category _category;

	public void SetUp(MenuVertical.Category category, Action<MenuVertical.Category> onClick)
	{
		_category = category;
		iconImage.sprite = _category.icon;
		button.onClick.AddListener(delegate
		{
			onClick(_category);
		});
	}
}
