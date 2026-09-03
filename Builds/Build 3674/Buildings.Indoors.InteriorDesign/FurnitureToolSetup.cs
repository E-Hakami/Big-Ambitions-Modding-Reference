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
using UI.Notification;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class FurnitureToolSetup : ToolSetup
{
	private FurnitureActionPanelUi _furnitureActionPanel;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Furniture;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_furnitureActionPanel = actionPanel as FurnitureActionPanelUi;
		if (_furnitureActionPanel == null)
		{
			Debug.LogError("FurnitureActionPanelUi is not set up correctly.");
			return;
		}
		Tool = new FurnitureTool
		{
			moveItemWithHandTool = MoveItemWithHandTool,
			isInPlacementMode = () => PlacementSystem.IsInPlacementMode
		};
		_furnitureActionPanel.placeItem = delegate(string itemName)
		{
			PlaceItemFurnitureTool(itemName);
		};
	}

	private void MoveItemWithHandTool(int itemIndex)
	{
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Furniture;
		IInteriorDesignerTool.moveItemWithHandTool(itemIndex, null, null);
	}

	private void PlaceItemFurnitureTool(string itemName, float yRotation = 0f)
	{
		if (!ItemHelper.FitsInBuilding(itemName))
		{
			Notifications.ShowError("itempanelui_notification_item_too_tall");
			return;
		}
		Item byName = ItemsGetter.GetByName(itemName);
		ItemInstance itemInstance;
		ItemController controller;
		if (byName.isSeasonalForSale)
		{
			ItemSeason itemSeason = byName.itemsBySeason.FirstOrDefault((ItemSeason x) => x.seasonName == SeasonHelper.CurrentSeason.seasonName) ?? byName.itemsBySeason.FirstOrDefault((ItemSeason x) => x.seasonName == SeasonName.None);
			if (itemSeason == null)
			{
				Debug.LogError("No seasonal item found for " + itemName);
				return;
			}
			itemInstance = ItemHelper.InitializeNewInstance(itemSeason.itemName);
			controller = PrefabHelper.CreatePrefabItem(itemSeason.itemName, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
			itemInstance.itemName = itemName;
			controller.itemName = itemName;
		}
		else
		{
			itemInstance = ItemHelper.InitializeNewInstance(itemName);
			controller = PrefabHelper.CreatePrefabItem(itemName, InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		}
		controller.ItemInstance = itemInstance;
		itemInstance.yRotation = yRotation;
		controller.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
		int index = InteriorDesignerController.ItemControllersCache.Count;
		InteriorDesignerController.ItemControllersCache.Add(controller);
		InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(controller);
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Furniture;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (controller.StockTypes.Count == 2)
			{
				controller.OnStockOptionSelected(1);
				if (controller is ShowcaseShelfController showcaseShelfController)
				{
					showcaseShelfController.ShowItemVisuals();
				}
			}
			InteriorDesignerUI.onManualExecuteAction = (Action<IRevertibleAction>)Delegate.Combine(InteriorDesignerUI.onManualExecuteAction, new Action<IRevertibleAction>(OnFurnitureManualExecution));
		});
		IInteriorDesignerTool.moveItemWithHandTool(index, (HandRevertibleAction handRevertibleAction) => new FurnitureRevertibleAction(index, controller.Item.DefaultMarketPrice, handRevertibleAction), null);
	}

	private void OnFurnitureManualExecution(IRevertibleAction revertibleAction)
	{
		InteriorDesignerUI.onManualExecuteAction = (Action<IRevertibleAction>)Delegate.Remove(InteriorDesignerUI.onManualExecuteAction, new Action<IRevertibleAction>(OnFurnitureManualExecution));
		if (InteriorDesignerAction.SpecialBehavior.Pressing() && revertibleAction is FurnitureRevertibleAction furnitureRevertibleAction)
		{
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(furnitureRevertibleAction.ItemIndex);
			PlaceItemFurnitureTool(itemControllerAtIndex.itemName, furnitureRevertibleAction.GetYRotation());
		}
	}
}
