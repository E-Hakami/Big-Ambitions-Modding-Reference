using System;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Helpers;
using Items.SpecialItems;
using JimmysUnityUtilities;
using Seasons;
using UI.InteriorDesigner;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class DuplicateToolSetup : ToolSetup
{
	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Duplicate;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		Tool = new DuplicateTool
		{
			duplicateItem = DuplicateItem,
			isHoldingShift = () => InteriorDesignerAction.SpecialBehavior.Pressing(),
			resetCursor = delegate
			{
				MouseController.SetCursor(null);
			},
			setDuplicateCursor = delegate
			{
				MouseController.SetCursor(new DuplicateCursorChangeEvent
				{
					ChangedCursor = true
				});
			}
		};
	}

	private bool DuplicateItem(int itemIndex, bool singleItem)
	{
		if (!GetItemControllerAtIndex(itemIndex).Item.canBeGrabbed)
		{
			IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interior_designer_duplicate_cannot_duplicate", itemIndex);
			return false;
		}
		float worth = 0f;
		int duplicateIndex = CreateDuplicate(itemIndex, singleItem, ref worth);
		if (duplicateIndex == -1)
		{
			return false;
		}
		int num = InteriorDesignerController.ItemControllersCache.Count - duplicateIndex - 1;
		int[] affectedChildIndexes = new int[num];
		for (int i = 0; i < num; i++)
		{
			affectedChildIndexes[i] = duplicateIndex + i + 1;
		}
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Duplicate;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			InteriorDesignerUI.onManualExecuteAction = (Action<IRevertibleAction>)Delegate.Combine(InteriorDesignerUI.onManualExecuteAction, new Action<IRevertibleAction>(OnDuplicateManualExecution));
		});
		IInteriorDesignerTool.moveItemWithHandTool(duplicateIndex, (HandRevertibleAction handRevertibleAction) => new DuplicateRevertibleAction(duplicateIndex, affectedChildIndexes, worth, handRevertibleAction), null);
		return true;
	}

	private void OnDuplicateManualExecution(IRevertibleAction revertibleAction)
	{
		InteriorDesignerUI.onManualExecuteAction = (Action<IRevertibleAction>)Delegate.Remove(InteriorDesignerUI.onManualExecuteAction, new Action<IRevertibleAction>(OnDuplicateManualExecution));
		if (InteriorDesignerAction.SpecialBehavior.Pressing() && revertibleAction is DuplicateRevertibleAction duplicateRevertibleAction)
		{
			DuplicateItem(duplicateRevertibleAction.ItemIndex, singleItem: false);
		}
	}

	private int CreateDuplicate(int itemIndex, bool singleItem, ref float worth, ItemController parent = null, int attachmentIndex = -1)
	{
		return CreateDuplicate(GetItemControllerAtIndex(itemIndex), singleItem, ref worth, parent, attachmentIndex);
	}

	private int CreateDuplicate(ItemController copiedItemController, bool singleItem, ref float worth, ItemController parent = null, int attachmentIndex = -1)
	{
		string itemName = copiedItemController.ItemInstance.itemName;
		float yRotation = copiedItemController.ItemInstance.yRotation;
		Item byName = ItemsGetter.GetByName(itemName);
		ItemInstance itemInstance;
		ItemController itemController;
		if (byName.isSeasonalForSale)
		{
			ItemSeason itemSeason = byName.itemsBySeason.FirstOrDefault((ItemSeason x) => x.seasonName == SeasonHelper.CurrentSeason.seasonName) ?? byName.itemsBySeason.FirstOrDefault((ItemSeason x) => x.seasonName == SeasonName.None);
			if (itemSeason == null)
			{
				Debug.LogError("No seasonal item found for " + itemName);
				worth = 0f;
				return -1;
			}
			itemInstance = ItemHelper.InitializeNewInstance(itemSeason.itemName);
			itemController = PrefabHelper.CreatePrefabItem(itemSeason.itemName, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
			itemInstance.itemName = itemName;
			itemController.itemName = itemName;
		}
		else
		{
			itemInstance = ItemHelper.InitializeNewInstance(itemName);
			itemController = PrefabHelper.CreatePrefabItem(itemName, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		}
		itemController.ItemInstance = itemInstance;
		InteriorDesignerController.ItemControllersCache.Add(itemController);
		int result = InteriorDesignerController.ItemControllersCache.Count - 1;
		if (parent != null && attachmentIndex != -1)
		{
			itemController.SetToParentPlaceableItem(parent, parent.AttachmentPoints[attachmentIndex]);
			itemController.transform.localPosition = copiedItemController.transform.localPosition;
		}
		itemInstance.yRotation = yRotation;
		itemController.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
		DuplicateProperties(itemController, copiedItemController);
		if ((itemController.Item.stackType & AttachmentPointType.FactoryMachine) != 0 && !string.IsNullOrEmpty(itemController.ItemInstance.parentId))
		{
			itemController.SetPlacementIndicatorsVisibility(visible: true);
		}
		if (singleItem || copiedItemController.ItemInstance.stackedItems.Count <= 0)
		{
			worth += itemController.ItemInstance.GetWorth();
			return result;
		}
		foreach (AttachableChild stackedItem in copiedItemController.ItemInstance.stackedItems)
		{
			ItemController itemControllerByID = ItemHelper.GetItemControllerByID(stackedItem.childId);
			if (itemControllerByID.isActiveAndEnabled)
			{
				int num = CreateDuplicate(itemControllerByID, singleItem: false, ref worth, itemController, stackedItem.attachmentIndex);
				if (num != -1)
				{
					ItemController itemControllerAtIndex = GetItemControllerAtIndex(num);
					itemControllerAtIndex.SetToParentPlaceableItem(itemController, itemController.AttachmentPoints[stackedItem.attachmentIndex]);
					itemController.ItemInstance.stackedItems.Add(new AttachableChild
					{
						childId = itemControllerAtIndex.ItemInstance.id,
						childItemName = stackedItem.childItemName,
						attachmentIndex = stackedItem.attachmentIndex
					});
				}
			}
		}
		return result;
	}

	private static void DuplicateProperties(ItemController controller, ItemController originalItemController)
	{
		Vector3 localEulerAngles = originalItemController.transform.localEulerAngles;
		controller.transform.localEulerAngles = new Vector3(0f, localEulerAngles.y, 0f);
		controller.SetCustomColors(originalItemController.customColors);
		controller.ItemInstance.worldSpaceTextValue = originalItemController.ItemInstance.worldSpaceTextValue;
		if (originalItemController.StockTypes.Count > 1 && controller.StockTypes.Count > 1)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				controller.OnStockOptionSelected(originalItemController.GetSelectedStockIndex());
				if (controller is ShowcaseShelfController showcaseShelfController)
				{
					showcaseShelfController.ShowItemVisuals();
				}
			});
		}
		FactoryAssemblyMachineController originalAssemblyMachineController = originalItemController as FactoryAssemblyMachineController;
		if ((object)originalAssemblyMachineController != null)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				FactoryAssemblyMachineController obj = (FactoryAssemblyMachineController)controller;
				obj.WorkstationInstance.selectedRecipeId = originalAssemblyMachineController.WorkstationInstance.selectedRecipeId;
				obj.WorkstationInstance.workstationType = originalAssemblyMachineController.WorkstationInstance.workstationType;
			});
		}
	}
}
