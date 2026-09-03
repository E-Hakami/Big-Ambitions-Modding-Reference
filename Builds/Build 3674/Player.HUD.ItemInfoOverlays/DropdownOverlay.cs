using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Controllers;
using Items.SpecialItems;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class DropdownOverlay : IOverlay
{
	[Header("Dropdown")]
	[SerializeField]
	private UI.Elements.Dropdown dropdown;

	[SerializeField]
	private TMP_Text stockLabel;

	[SerializeField]
	private GameObject stockSplitter;

	[SerializeField]
	private Button removeContentButton;

	[SerializeField]
	private Button pickSingleItemButton;

	private void Start()
	{
		removeContentButton.onClick.AddListener(OnRemoveContentClick);
		pickSingleItemButton.onClick.AddListener(OnPickSingleItemClick);
	}

	public override bool IsValid(EntityController entityController)
	{
		ItemController itemController = entityController as ItemController;
		if (!itemController)
		{
			return false;
		}
		if ((bool)(entityController as SignController))
		{
			return true;
		}
		string[] itemsThatCanShowcase = itemController.Item.itemsThatCanShowcase;
		if (itemsThatCanShowcase == null)
		{
			return false;
		}
		return itemsThatCanShowcase.Length != 0;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return false;
		}
		ItemController itemController = entityController as ItemController;
		if ((bool)itemController && (itemController.ItemInstance.ItemCached.type & ItemType.PointOfSale) != 0 && !InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedpaperbags))
		{
			return false;
		}
		return true;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		ItemController itemController = entityController as ItemController;
		SetDropdown(itemController);
		SetStockLabel(itemController);
		if ((itemController.Item.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) == 0)
		{
			removeContentButton.gameObject.SetActive(value: false);
		}
		else
		{
			removeContentButton.gameObject.SetActive(itemController.ItemInstance.GetStockInstance().amount > 0);
		}
		pickSingleItemButton.gameObject.SetActive(CanPickSingleItem(itemController));
	}

	private void OnRemoveContentClick()
	{
		(linkedController as ItemController)?.RemoveStockInContent();
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	private void OnPickSingleItemClick()
	{
		ShowcaseShelfController showcaseShelfController = linkedController as ShowcaseShelfController;
		if ((bool)showcaseShelfController && CanPickSingleItem(showcaseShelfController))
		{
			showcaseShelfController.WalkToTakeItem(showcaseShelfController.ItemInstance.GetStockInstance().itemName);
		}
	}

	private static bool CanPickSingleItem(ItemController itemController)
	{
		ShowcaseShelfController showcaseShelfController = itemController as ShowcaseShelfController;
		if (!showcaseShelfController)
		{
			return false;
		}
		CargoInstance stockInstance = showcaseShelfController.ItemInstance.GetStockInstance();
		if (stockInstance.amount <= 0 || string.IsNullOrEmpty(stockInstance.itemName))
		{
			return false;
		}
		Item itemCached = stockInstance.ItemCached;
		if ((bool)itemCached)
		{
			if ((itemCached.type & ItemType.RetailProduct) == 0)
			{
				return itemCached.canPutInShoppingBasket;
			}
			return true;
		}
		return false;
	}

	private void SetDropdown(ItemController itemController)
	{
		List<string> stockTypes = itemController.StockTypes;
		int selectedStockIndex = itemController.GetSelectedStockIndex();
		dropdown.SetOptions(stockTypes, localize: true, selectedStockIndex);
		dropdown.ResetSelectedOption(selectedStockIndex);
		dropdown.onOptionSelected.RemoveAllListeners();
		dropdown.onOptionSelected.AddListener(delegate(int index)
		{
			itemController.OnStockOptionSelected(index, dropdown);
			SetStockLabel(itemController);
		});
	}

	private void SetStockLabel(ItemController itemController)
	{
		if ((bool)(itemController as SignController))
		{
			SetStockText("");
		}
		else if (string.IsNullOrEmpty(itemController.CurrentStockDropdownOption))
		{
			SetStockText("");
		}
		else
		{
			SetStockText(itemController.StockAmountText);
		}
	}

	private void SetStockText(string stockText)
	{
		if (string.IsNullOrEmpty(stockText))
		{
			stockLabel.gameObject.SetActive(value: false);
			stockSplitter.gameObject.SetActive(value: false);
		}
		else
		{
			stockLabel.text = stockText;
			stockLabel.gameObject.SetActive(value: true);
			stockSplitter.gameObject.SetActive(value: true);
		}
	}
}
