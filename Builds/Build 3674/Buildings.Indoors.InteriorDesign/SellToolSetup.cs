using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class SellToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Sell;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new SellTool
		{
			getItemsToSellIndices = GetEnabledItemControllerIndicesInItem,
			getSellPrice = GetSellPrice,
			getIsOccupied = GetIsOccupied,
			changeHighlightColors = ChangeHighlightColors,
			sellableDespitePrice = SellableDespitePrice,
			isInDevMode = () => GameManager.IsDevMode,
			toggleItemForSale = ToggleItemForSale,
			showAllItemHighlights = ShowAllItemHighlights,
			isInBlueprintCreator = () => InteriorDesignerHelper.BlueprintCreatorMode
		};
		SellRevertibleAction.hideItems = HideItems;
		SellRevertibleAction.addToSoldList = AddToSoldList;
		SellRevertibleAction.removeFromSoldList = RemoveFromSoldList;
		SellRevertibleAction.getAttachmentIndex = GetAttachmentIndex;
	}

	private void HideItems(int[] itemControllerIndices, int[] attachmentIndices, bool hide)
	{
		if (itemControllerIndices.Length == 0)
		{
			return;
		}
		for (int i = 0; i < itemControllerIndices.Length; i++)
		{
			ItemController itemController = GetItemControllerAtIndex(itemControllerIndices[i]);
			if (hide)
			{
				InteriorDesignerController.RemoveItemFromLists(itemController);
				if (attachmentIndices[i] != -1)
				{
					AttachableChild item = itemController.parentItemController.ItemInstance.stackedItems.FirstOrDefault((AttachableChild x) => x.childId == itemController.ItemInstance.id);
					itemController.parentItemController.ItemInstance.stackedItems.Remove(item);
				}
				itemController.gameObject.DisableWithScaleAnim(itemController.Colliders);
				continue;
			}
			InteriorDesignerController.AddItemToLists(itemController);
			if (attachmentIndices[i] != -1)
			{
				itemController.parentItemController.ItemInstance.stackedItems.Add(new AttachableChild
				{
					childId = itemController.ItemInstance.id,
					childItemName = itemController.ItemInstance.itemName,
					attachmentIndex = attachmentIndices[i]
				});
			}
			itemController.gameObject.EnableWithScaleAnim(itemController.Colliders);
		}
	}

	private void AddToSoldList(int[] itemControllerIndices)
	{
		foreach (int num in itemControllerIndices)
		{
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(num);
			InteriorDesignerController.SoldItemIndexes.Add(num);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(itemControllerAtIndex.ItemInstance, triggerAction: false);
			InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(itemControllerAtIndex);
		}
	}

	private void RemoveFromSoldList(int[] itemControllerIndices)
	{
		foreach (int num in itemControllerIndices)
		{
			InteriorDesignerController.SoldItemIndexes.Remove(num);
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(num);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.AddItemInstanceToBuilding(itemControllerAtIndex.ItemInstance);
			InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(itemControllerAtIndex);
		}
	}

	private int[] GetEnabledItemControllerIndicesInItem(int itemIndex)
	{
		List<int> list = new List<int> { itemIndex };
		foreach (ItemController childItemController in GetItemControllerAtIndex(itemIndex).childItemControllers)
		{
			if (childItemController.gameObject.activeSelf)
			{
				int indexOfItemController = GetIndexOfItemController(childItemController);
				if (!InteriorDesignerController.SoldItemIndexes.Contains(indexOfItemController))
				{
					list.Add(indexOfItemController);
				}
			}
		}
		return list.ToArray();
	}

	private float GetSellPrice(int itemIndex)
	{
		if (itemIndex == -1)
		{
			return 0f;
		}
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		Item byName = ItemsGetter.GetByName(itemControllerAtIndex.itemName);
		if ((object)byName != null && byName.HasTag(TagRef.Itemtag.isdeliveryspot))
		{
			return 0f;
		}
		return itemControllerAtIndex.ItemInstance.GetSellingPrice();
	}

	private bool GetIsOccupied(int itemIndex)
	{
		if (itemIndex == -1)
		{
			return true;
		}
		if (!GetItemControllerAtIndex(itemIndex).Occupied)
		{
			return false;
		}
		Notifications.ShowError("interiordesigner_notification_cant_sell_occupied_objects");
		return true;
	}

	private static void ChangeHighlightColors(bool sellMode)
	{
		Color32 color = (sellMode ? InstanceBehavior<GlobalReferences>.Instance.colors.yellow : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		foreach (ItemController item in InteriorDesignerController.ItemControllersCache)
		{
			if ((bool)item && (item.Item.DefaultMarketPrice > 0f || item.Item.isSpecialGift))
			{
				item.SetOutlineColor(color);
				item.highlightChildrenOnHover = sellMode;
			}
		}
	}

	private bool SellableDespitePrice(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (!itemControllerAtIndex.Item.isSpecialGift)
		{
			return itemControllerAtIndex.Item.canBeGrabbed;
		}
		return true;
	}

	private void ToggleItemForSale(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		itemControllerAtIndex.playerItemPurchaserSettings.enabled = !itemControllerAtIndex.playerItemPurchaserSettings.enabled;
		bool enabled = itemControllerAtIndex.playerItemPurchaserSettings.enabled;
		if (enabled)
		{
			itemControllerAtIndex.playerItemPurchaserSettings.itemName = itemControllerAtIndex.itemName;
			itemControllerAtIndex.playerItemPurchaserSettings.itemQuantity = 1;
		}
		Transform transform = itemControllerAtIndex.transform.Find("ForSaleVisuals");
		if (transform != null)
		{
			transform.gameObject.SetActive(enabled);
		}
		itemControllerAtIndex.SetOutlineColor(enabled ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
	}

	private static void ShowAllItemHighlights(bool show)
	{
		foreach (ItemController item in InteriorDesignerController.ItemControllersCache)
		{
			Color32 color = (item.playerItemPurchaserSettings.enabled ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			item.SetOutlineColor(color);
			item.SetPermanentOutline(show);
		}
		if (!show)
		{
			ChangeHighlightColors(sellMode: true);
		}
	}

	private int GetAttachmentIndex(int itemIndex)
	{
		ItemController controller = GetItemControllerAtIndex(itemIndex);
		if (controller != null && controller.parentItemController != null)
		{
			return controller.parentItemController.ItemInstance.stackedItems.Find((AttachableChild x) => x.childId == controller.ItemInstance.id).attachmentIndex;
		}
		return -1;
	}
}
