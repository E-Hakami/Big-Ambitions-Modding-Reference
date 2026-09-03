using System;
using UnityEngine;

namespace UI.InteriorDesigner;

public class IDItemUiTemplateData
{
	public readonly ItemController itemController;

	public readonly string itemName;

	public readonly Action<IDItemTemplateBase, ItemController> onClickItemController;

	public readonly Action<IDItemTemplateBase, string> onClickItemName;

	public readonly Sprite overlayBackgroundSprite;

	public readonly Sprite overlayIconSprite;

	public readonly float price;

	public IDItemUiTemplateData(ItemController itemController, Action<IDItemTemplateBase, ItemController> onClickItemController, Sprite overlayBackgroundSprite = null, Sprite overlayIconSprite = null)
	{
		this.itemController = itemController;
		this.onClickItemController = onClickItemController;
		this.overlayBackgroundSprite = overlayBackgroundSprite;
		this.overlayIconSprite = overlayIconSprite;
	}

	public IDItemUiTemplateData(string itemName, float price, Action<IDItemTemplateBase, string> onClickItemName)
	{
		this.itemName = itemName;
		this.price = price;
		this.onClickItemName = onClickItemName;
	}
}
