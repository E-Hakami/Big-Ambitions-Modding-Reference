using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Player.HUD.ItemWarningIcons;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Buildings.Indoors.InteriorDesign;

public class HandToolSetup : ToolSetup
{
	private static readonly PlacementOutlineAltBehavior AltBehavior = new PlacementOutlineAltBehavior
	{
		onPlacementInvalid = delegate(IPlaceableItem placeItem, Collider _)
		{
			placeItem.RemoveOutline();
		},
		predicate = delegate(IPlaceableItem placeItem, Collider overlappingCollider)
		{
			if ((placeItem != null && placeItem.GetItemInstance()?.ItemCached.HasTag(TagRef.Itemtag.isbox) == false) || overlappingCollider == null)
			{
				return false;
			}
			ICargoHolder cargoHolder = overlappingCollider.GetComponent<ItemController>()?.GetItemInstance();
			ICargoHolder obj = cargoHolder ?? overlappingCollider.GetComponentInParent<VehicleController>()?.vehicleInstance;
			return obj != null && obj.GetCargoSpace() > 0;
		}
	};

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Hand;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new HandTool
		{
			getAttachmentPointOfItemAtIndex = GetAttachmentPointOfItemAtIndex,
			getAttachmentPointOfItemBeingMoved = GetAttachmentPointOfItemBeingMoved,
			tryToStartItemPlacement = TryToStartItemPlacement,
			canPlaceItem = PlacementSystem.CanPlaceItem,
			stopItemPlacement = StopItemPlacement,
			sellItem = InteriorDesignerController.GetTool<SellTool>(ToolName.Sell).SellItem,
			destroyItem = DestroyItem,
			showItemWarnings = delegate
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.HandleCanvasVisibility(shouldHide: false);
			},
			hideItemWarnings = delegate
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.HandleCanvasVisibility(shouldHide: true);
			},
			getCargoInstanceFromBox = GetCargoInstanceFromBox,
			isItemBox = IsItemBox,
			hasCapacity = HasCapacity,
			setOutline = SetOutline,
			setPlacementAltBehavior = SetPlacementAltBehavior
		};
	}

	private (int, AttachmentPoint) GetAttachmentPointOfItemAtIndex(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		return (GetIndexOfItemController(itemControllerAtIndex.parentItemController), itemControllerAtIndex.parentAttachmentPoint);
	}

	private (int, AttachmentPoint) GetAttachmentPointOfItemBeingMoved()
	{
		IPlaceableItem lastParentItem = PlacementSystem.lastParentItem;
		if (lastParentItem == null)
		{
			return (-1, null);
		}
		return (GetIndexOfItemController(lastParentItem as ItemController), PlacementSystem.lastAttachmentPoint);
	}

	private bool TryToStartItemPlacement(int itemIndex)
	{
		return GetItemControllerAtIndex(itemIndex).StartPlacementMode(EventSystem.current.IsPointerOverGameObject(), setCamera: false);
	}

	private static void StopItemPlacement()
	{
		if (PlacementSystem.IsInPlacementMode)
		{
			PlacementHelper.CancelPlacementMode(setCamera: false);
		}
	}

	private static void DestroyItem(int itemIndex)
	{
		HashSet<int> itemAndAttachedIndexes = GetItemAndAttachedIndexes(itemIndex);
		if (itemAndAttachedIndexes.Count == 0)
		{
			return;
		}
		List<ItemController> itemControllersCache = InteriorDesignerController.ItemControllersCache;
		int num = itemAndAttachedIndexes.Count;
		int num2 = itemControllersCache.Count - 1;
		while (num2 >= 0 && num > 0)
		{
			if (itemAndAttachedIndexes.Remove(num2))
			{
				ItemController itemController = itemControllersCache[num2];
				InteriorDesignerController.RemoveItemFromLists(itemController, notify: false);
				itemControllersCache.RemoveAt(num2);
				InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(itemController);
				Object.Destroy(itemController.gameObject);
				num--;
			}
			num2--;
		}
		InteriorDesignerUI.onManualExecuteAction = null;
	}

	private static HashSet<int> GetItemAndAttachedIndexes(int itemIndex)
	{
		List<ItemController> itemControllersCache = InteriorDesignerController.ItemControllersCache;
		if (itemIndex < 0 || itemIndex >= itemControllersCache.Count)
		{
			return new HashSet<int>();
		}
		if (itemControllersCache[itemIndex] == null)
		{
			return new HashSet<int>();
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(itemControllersCache.Count);
		for (int i = 0; i < itemControllersCache.Count; i++)
		{
			string text = itemControllersCache[i]?.ItemInstance?.id;
			if (!string.IsNullOrEmpty(text))
			{
				dictionary[text] = i;
			}
		}
		HashSet<int> hashSet = new HashSet<int>();
		Stack<int> stack = new Stack<int>();
		stack.Push(itemIndex);
		while (stack.Count > 0)
		{
			int num = stack.Pop();
			if (num < 0 || num >= itemControllersCache.Count)
			{
				continue;
			}
			ItemController itemController = itemControllersCache[num];
			if (itemController?.ItemInstance == null || !hashSet.Add(num))
			{
				continue;
			}
			List<AttachableChild> stackedItems = itemController.ItemInstance.stackedItems;
			for (int j = 0; j < stackedItems.Count; j++)
			{
				string childId = stackedItems[j].childId;
				if (!string.IsNullOrEmpty(childId) && dictionary.TryGetValue(childId, out var value))
				{
					stack.Push(value);
				}
			}
		}
		return hashSet;
	}

	private bool IsItemBox(int itemIndex)
	{
		return GetItemControllerAtIndex(itemIndex).Item.HasTag(TagRef.Itemtag.isbox);
	}

	private bool HasCapacity(int index, bool isVehicle)
	{
		ICargoHolder cargoHolder;
		if (!isVehicle)
		{
			ICargoHolder itemInstance = GetItemControllerAtIndex(index).ItemInstance;
			cargoHolder = itemInstance;
		}
		else
		{
			ICargoHolder itemInstance = GetVehicleControllerAtIndex(index).vehicleInstance;
			cargoHolder = itemInstance;
		}
		return cargoHolder.GetCargoSpace() > 0;
	}

	private CargoInstance GetCargoInstanceFromBox(int boxIndex)
	{
		return ((ICargoHolder)GetItemControllerAtIndex(boxIndex).ItemInstance).GetCargoInstances()[0];
	}

	private void SetOutline(int index, bool isVehicle, bool showOutline)
	{
		bool num = !isVehicle && IsItemBox(index);
		EntityController entityController = (isVehicle ? ((EntityController)GetVehicleControllerAtIndex(index)) : ((EntityController)GetItemControllerAtIndex(index)));
		if (!num)
		{
			entityController.SetPermanentOutline(showOutline);
			return;
		}
		if (showOutline)
		{
			entityController.IsOutlineSuppressed = false;
		}
		entityController.SetOutlineColor(showOutline ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		if (!showOutline)
		{
			entityController.IsOutlineSuppressed = true;
		}
	}

	private static void SetPlacementAltBehavior(bool enabled)
	{
		if (enabled)
		{
			PlacementSystem.AddAltBehavior(AltBehavior);
		}
		else
		{
			PlacementSystem.RemoveAltBehavior(AltBehavior);
		}
	}
}
