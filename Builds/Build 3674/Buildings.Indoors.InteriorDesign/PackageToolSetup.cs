using System.Collections.Generic;
using System.Linq;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Controllers;
using Extensions;
using Helpers;
using Items.SpecialItems;
using UI.InteriorDesigner;
using UI.Notification;
using UnityEngine;

namespace Buildings.Indoors.InteriorDesign;

public class PackageToolSetup : ToolSetup
{
	private PackageOverlay _packageOverlay;

	public override IInteriorDesignerTool Tool { get; protected set; }

	public override ToolName ToolName => ToolName.Package;

	public override void Setup(ActionPanelUI actionPanel, MonoBehaviour overlay)
	{
		_packageOverlay = overlay as PackageOverlay;
		if (_packageOverlay == null)
		{
			return;
		}
		Tool = new PackageTool
		{
			openPackageOverlay = OpenPackageOverlay,
			closePackageOverlay = _packageOverlay.Close,
			isCargoHolder = IsCargoHolder,
			moveItemWithHandTool = MoveItemWithHandTool,
			isInPlacementMode = () => PlacementSystem.IsInPlacementMode,
			isHoldingSpecialBehavior = () => InteriorDesignerAction.SpecialBehavior.Pressing(),
			canBePacked = CanBePacked,
			isBox = IsBox,
			packItem = PackItem,
			hasCargoInstances = HasCargoInstances
		};
		PackageRevertibleAction.addCargo = AddCargo;
		PackageRevertibleAction.discardCargo = DiscardCargo;
		PackageRevertibleAction.addSingleCargo = AddSingleCargo;
		PackageRevertibleAction.discardSingleCargo = DiscardSingleCargo;
		PackageRevertibleAction.moveCargo = MoveCargo;
		PackageRevertibleAction.hideBoxIfEmpty = HideBoxIfEmpty;
		PackageRevertibleAction.closeOverlayIfSelected = _packageOverlay.CloseIfSelected;
		PackageRevertibleAction.allowMove = AllowMove;
		PackageRevertibleAction.playSuccessSfx = delegate(bool isVehicle)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(isVehicle ? SoundType.HandTruckAddBox : SoundType.ObjectPutDown, PlayerHelper.GetPosition(), 0.5f);
		};
		PackageRevertibleAction.discardCargoSingleCargoInstance = DiscardCargoSingleCargoInstance;
		PackagePackItemRevertibleAction.playPlaceSfx = delegate(int itemIndex)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectPutDown, GetItemControllerAtIndex(itemIndex).transform.position);
		};
		PackageRevertibleAction.removeFromParentStackedItems = delegate(int itemIndex)
		{
			ItemController itemController = GetItemControllerAtIndex(itemIndex);
			itemController.parentItemController?.ItemInstance.stackedItems?.RemoveAll((AttachableChild x) => itemController.ItemInstance.id == x.childId);
		};
	}

	private void OpenPackageOverlay(int clickedIndex, bool isVehicle)
	{
		ICargoHolder cargoHolder;
		if (!isVehicle)
		{
			ICargoHolder itemInstance = GetItemControllerAtIndex(clickedIndex).ItemInstance;
			cargoHolder = itemInstance;
		}
		else
		{
			ICargoHolder itemInstance = GetVehicleControllerAtIndex(clickedIndex).vehicleInstance;
			cargoHolder = itemInstance;
		}
		ICargoHolder cargoHolder2 = cargoHolder;
		_packageOverlay.Open(cargoHolder2, clickedIndex, isVehicle);
	}

	private bool IsCargoHolder(int index)
	{
		ItemInstance itemInstance = GetItemControllerAtIndex(index).ItemInstance;
		if (itemInstance.ItemCached.cargoCapacity <= 0)
		{
			return itemInstance.cargoInstances.Count > 0;
		}
		return true;
	}

	private void MoveItemWithHandTool(int itemIndex)
	{
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Package;
		IInteriorDesignerTool.moveItemWithHandTool(itemIndex, null, null);
	}

	private bool CanBePacked(int itemIndex)
	{
		if (IsBox(itemIndex))
		{
			return true;
		}
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (itemControllerAtIndex.ItemInstance.stackedItems.Count > 0)
		{
			Notifications.Show(NotificationType.Error, "interiordesigner_package_cannot_pack_stacked_items");
			return false;
		}
		if (!itemControllerAtIndex.ItemInstance.IsEmpty() && !(itemControllerAtIndex is ShowcaseShelfController))
		{
			Notifications.Show(NotificationType.Error, "interiordesigner_package_cannot_pack_stock");
			return false;
		}
		if (itemControllerAtIndex is EmployeeStationController)
		{
			if (itemControllerAtIndex.Occupied)
			{
				goto IL_008e;
			}
		}
		else if (itemControllerAtIndex is SeatController { Occupied: not false })
		{
			goto IL_008e;
		}
		bool flag = false;
		goto IL_0094;
		IL_0094:
		if (flag)
		{
			Notifications.Show(NotificationType.Error, "interiordesigner_package_cannot_pack_occupied");
			return false;
		}
		if (!itemControllerAtIndex.Item.canBeGrabbed)
		{
			Notifications.Show(NotificationType.Error, "interiordesigner_package_cannot_pack");
			return false;
		}
		return true;
		IL_008e:
		flag = true;
		goto IL_0094;
	}

	private bool IsBox(int itemIndex)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (!itemControllerAtIndex.Item.HasTag(TagRef.Itemtag.isbox))
		{
			return itemControllerAtIndex.Item.HasTag(TagRef.Itemtag.isbag);
		}
		return true;
	}

	private void PackItem(int itemIndex)
	{
		ItemController itemController = GetItemControllerAtIndex(itemIndex);
		CargoInstance cargoInstance = itemController.ItemInstance.ConvertToCargoInstance();
		itemController.gameObject.DisableWithScaleAnim(itemController.Colliders);
		ItemInstance itemInstance = ItemHelper.InitializeNewInstance("ba:itemname_closedcardboardbox");
		itemInstance.AddToCargo(cargoInstance);
		ItemController boxController = PrefabHelper.CreatePrefabItem("ba:itemname_closedcardboardbox", InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer);
		boxController.ItemInstance = itemInstance;
		boxController.transform.rotation = Quaternion.identity;
		int boxIndex = InteriorDesignerController.ItemControllersCache.Count;
		InteriorDesignerController.ItemControllersCache.Add(boxController);
		InstanceBehavior<BuildingManager>.Instance.allItemControllers.Add(boxController);
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectPickup, PlayerHelper.GetPosition());
		IInteriorDesignerTool.toolToOpenAfterUsage = ToolName.Package;
		IInteriorDesignerTool.moveItemWithHandTool(boxIndex, (HandRevertibleAction handRevertibleAction) => new PackagePackItemRevertibleAction(itemIndex, cargoInstance, boxIndex, handRevertibleAction), delegate
		{
			if (InteriorDesignerController.ItemControllersCache.Count > boxIndex && InteriorDesignerController.ItemControllersCache[boxIndex] == boxController)
			{
				InteriorDesignerController.ItemControllersCache.RemoveAt(boxIndex);
			}
			InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(boxController);
			boxController.gameObject.DestroyWithScaleAnim(boxController.Colliders);
			itemController.gameObject.EnableWithScaleAnim(itemController.Colliders);
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ObjectPutDown, itemController.transform.position);
		});
	}

	private bool HasCargoInstances(int itemIndex, bool isVehicle)
	{
		return GetCargoHolder(itemIndex, isVehicle).GetCargoInstances().Count > 0;
	}

	private void AddCargo(int cargoHolderIndex, int cargoIndex, bool isVehicle, CargoInstance cargoInstance)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[cargoHolderIndex]);
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			itemController.ItemInstance.GetStockInstance().amount += cargoInstance.amount;
			if (itemController is ShowcaseShelfController showcaseShelfController)
			{
				showcaseShelfController.UpdateVisuals();
			}
		}
		else
		{
			cargoHolder.AddToCargo(cargoInstance, cargoIndex);
		}
	}

	private void DiscardCargo(int cargoHolderIndex, int cargoIndex, bool isVehicle)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[cargoHolderIndex]);
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			itemController.ItemInstance.GetStockInstance().amount = 0;
			if (itemController is ShowcaseShelfController showcaseShelfController)
			{
				showcaseShelfController.UpdateVisuals();
			}
			return;
		}
		foreach (CargoInstance cargoInstance in CargoItem.ConvertCargoInstancesToCargoItems(cargoHolder.GetCargoInstances())[cargoIndex].cargoInstances)
		{
			cargoHolder.RemoveFromCargo(cargoInstance);
		}
	}

	private void DiscardCargoSingleCargoInstance(int cargoHolderIndex, int cargoIndex, bool isVehicle)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[cargoHolderIndex]);
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			itemController.ItemInstance.GetStockInstance().amount = 0;
			if (itemController is ShowcaseShelfController showcaseShelfController)
			{
				showcaseShelfController.UpdateVisuals();
			}
		}
		else
		{
			CargoItem cargoItem = CargoItem.ConvertCargoInstancesToCargoItems(cargoHolder.GetCargoInstances())[cargoIndex];
			cargoHolder.RemoveFromCargo(cargoItem.cargoInstances[0]);
		}
	}

	private void DiscardSingleCargo(int cargoHolderIndex, int cargoIndex, bool isVehicle)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[cargoHolderIndex]);
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			itemController.ItemInstance.GetStockInstance().amount--;
			if (itemController is ShowcaseShelfController showcaseShelfController)
			{
				showcaseShelfController.UpdateVisuals();
			}
		}
		else
		{
			CargoInstance cargoInstance = CargoItem.ConvertCargoInstancesToCargoItems(cargoHolder.GetCargoInstances())[cargoIndex].cargoInstances[0];
			cargoHolder.ReduceFromCargo(cargoInstance, 1);
		}
	}

	private void AddSingleCargo(int cargoHolderIndex, int cargoIndex, bool isVehicle, CargoInstance cargoInstance)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ItemController itemController = (isVehicle ? null : InteriorDesignerController.ItemControllersCache[cargoHolderIndex]);
		if (itemController != null && itemController.Item.IsStockCarrier())
		{
			itemController.ItemInstance.GetStockInstance().amount++;
			if (itemController is ShowcaseShelfController showcaseShelfController)
			{
				showcaseShelfController.UpdateVisuals();
			}
		}
		else
		{
			cargoHolder.MergeIntoCargo(cargoInstance);
			if (cargoInstance.amount > 0)
			{
				cargoHolder.AddToCargo(cargoInstance);
			}
		}
	}

	private bool AllowMove(int cargoHolderIndex, string itemName)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(cargoHolderIndex);
		if (!(itemControllerAtIndex == null) && itemControllerAtIndex.Item.HasTag(TagRef.Itemtag.isbag))
		{
			return ItemsGetter.GetByName(itemName).isADemandedProduct;
		}
		return true;
	}

	private (bool, int) MoveCargo(int cargoHolderIndex, bool isVehicle, int targetCargoHolderIndex, bool isTargetVehicle, string itemName, int amount, bool ignoreMoveRestrictions)
	{
		ICargoHolder cargoHolder = GetCargoHolder(cargoHolderIndex, isVehicle);
		ICargoHolder cargoHolder2 = GetCargoHolder(targetCargoHolderIndex, isTargetVehicle);
		bool flag = ignoreMoveRestrictions && cargoHolder2 is ItemInstance itemInstance && (ItemsGetter.GetByName(itemInstance.itemName)?.HasTag(TagRef.Itemtag.isdeliveryspot) ?? false);
		List<CargoInstance> list = (from x in cargoHolder.GetCargoInstances()
			where x.itemName == itemName
			select x).ToList();
		if (list.Count == 0 || amount <= 0)
		{
			return (false, 0);
		}
		List<CargoInstance> list2 = new List<CargoInstance>();
		foreach (CargoInstance item in list)
		{
			List<NestedCargoInstance> nestedCargoInstances = item.nestedCargoInstances;
			if (nestedCargoInstances != null && nestedCargoInstances.Count > 0 && item.amount > 0)
			{
				list2.Add(item);
			}
		}
		int movedItems = 0;
		if (list2.Count > 0)
		{
			int num = amount;
			int num2 = list2.Count - 1;
			while (num2 >= 0 && num > 0)
			{
				CargoInstance cargoInstance = list2[num2];
				while (cargoInstance.amount > 0 && num > 0)
				{
					CargoInstance cargoInstance2 = new CargoInstance(cargoInstance.itemName, 1, cargoInstance.pricePerUnit, cargoInstance.paid, CopyCustomColors(cargoInstance.customColors))
					{
						nestedCargoInstances = CopyNestedCargo(cargoInstance.nestedCargoInstances)
					};
					if (!flag && !cargoHolder2.TryToAddToCargo(cargoInstance2))
					{
						return (movedItems == amount, movedItems);
					}
					if (flag)
					{
						AddToRestrictedTarget(cargoHolder2, cargoInstance2);
					}
					cargoInstance.amount--;
					num--;
					movedItems++;
				}
				if (cargoInstance.amount <= 0)
				{
					cargoHolder.RemoveFromCargo(cargoInstance);
				}
				num2--;
			}
			return (movedItems == amount, movedItems);
		}
		MoveItemsToInventory(isTargetVehicle, targetCargoHolderIndex, cargoHolder2, itemName, amount, list, notify: true, flag, out movedItems);
		if (movedItems == 0)
		{
			return (false, 0);
		}
		RemoveItemsFromInventory(isVehicle, cargoHolderIndex, cargoHolder, itemName, movedItems, list, out var removedItems);
		if (removedItems == movedItems)
		{
			return (true, movedItems);
		}
		int num3 = movedItems - removedItems;
		if (num3 > 0)
		{
			List<CargoInstance> cargoInstances = (from x in cargoHolder2.GetCargoInstances()
				where x.itemName == itemName
				select x).ToList();
			RemoveItemsFromInventory(isTargetVehicle, targetCargoHolderIndex, cargoHolder2, itemName, num3, cargoInstances, out var _);
		}
		return (false, removedItems);
		static List<CustomColor> CopyCustomColors(List<CustomColor> colors)
		{
			return colors?.Select((CustomColor x) => x.Copy()).ToList();
		}
		static List<NestedCargoInstance> CopyNestedCargo(List<NestedCargoInstance> nested)
		{
			if (nested != null)
			{
				return nested.Select((NestedCargoInstance x) => new NestedCargoInstance(x.itemName, x.amount, x.pricePerUnit, CopyCustomColors(x.customColors))).ToList();
			}
			return new List<NestedCargoInstance>();
		}
	}

	private ICargoHolder GetCargoHolder(int cargoHolderIndex, bool isVehicle)
	{
		if (!isVehicle)
		{
			return GetItemControllerAtIndex(cargoHolderIndex).ItemInstance;
		}
		return GetVehicleControllerAtIndex(cargoHolderIndex).vehicleInstance;
	}

	private void HideBoxIfEmpty(int itemIndex, bool hide, bool animate)
	{
		ItemController itemControllerAtIndex = GetItemControllerAtIndex(itemIndex);
		if (!IsBox(itemIndex))
		{
			return;
		}
		if (hide)
		{
			if (itemControllerAtIndex.ItemInstance.cargoInstances.Count == 0)
			{
				InteriorDesignerController.RemoveItemFromLists(itemControllerAtIndex);
				if (!animate)
				{
					itemControllerAtIndex.gameObject.SetActive(value: false);
				}
				else
				{
					itemControllerAtIndex.gameObject.DisableWithScaleAnim(itemControllerAtIndex.Colliders);
				}
				InteriorDesignerController.SoldItemIndexes.Add(itemIndex);
				InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(itemControllerAtIndex.ItemInstance, triggerAction: false);
				InstanceBehavior<BuildingManager>.Instance.allItemControllers.Remove(itemControllerAtIndex);
				_packageOverlay.Close();
			}
		}
		else if (InteriorDesignerController.SoldItemIndexes.Contains(itemIndex))
		{
			InteriorDesignerController.AddItemToLists(itemControllerAtIndex);
			itemControllerAtIndex.gameObject.EnableWithScaleAnim(itemControllerAtIndex.Colliders);
			InteriorDesignerController.SoldItemIndexes.Remove(itemIndex);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.AddItemInstanceToBuilding(itemControllerAtIndex.ItemInstance);
		}
	}

	private void MoveItemsToInventory(bool isTargetVehicle, int targetCargoHolderIndex, ICargoHolder targetCargoHolder, string itemName, int amount, List<CargoInstance> cargoInstances, bool notify, bool allowRestrictedTarget, out int movedItems)
	{
		movedItems = 0;
		if (allowRestrictedTarget)
		{
			float pricePerUnit = cargoInstances[0].pricePerUnit;
			CargoInstance cargoInstance = new CargoInstance(itemName, amount, pricePerUnit);
			AddToRestrictedTarget(targetCargoHolder, cargoInstance);
			movedItems = amount;
			return;
		}
		if (!isTargetVehicle)
		{
			ItemController itemControllerAtIndex = GetItemControllerAtIndex(targetCargoHolderIndex);
			if (itemControllerAtIndex.Item.IsStockCarrier())
			{
				CargoInstance stockInstance = itemControllerAtIndex.ItemInstance.GetStockInstance();
				if (stockInstance == null || stockInstance.itemName != itemName)
				{
					if (notify)
					{
						IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_already_stores_other_product", targetCargoHolderIndex);
					}
					return;
				}
				if (itemControllerAtIndex is ShowcaseShelfController showcaseShelfController && showcaseShelfController.IsFull())
				{
					if (notify)
					{
						IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_cargoholder_full", targetCargoHolderIndex);
					}
					return;
				}
				if (itemControllerAtIndex is CashRegisterController cashRegisterController)
				{
					if (!ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.isbag))
					{
						if (notify)
						{
							IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_cannot_store", targetCargoHolderIndex);
						}
						return;
					}
					if (cashRegisterController.IsFull())
					{
						if (notify)
						{
							IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_cargoholder_full", targetCargoHolderIndex);
						}
						return;
					}
				}
				if (!(itemControllerAtIndex as Producer).CanAddAnyToInventory(cargoInstances))
				{
					if (notify)
					{
						IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_cannot_store", targetCargoHolderIndex);
					}
					return;
				}
				int num = amount;
				{
					foreach (CargoInstance cargoInstance2 in cargoInstances)
					{
						if (num <= 0)
						{
							break;
						}
						int num2 = Mathf.Min(cargoInstance2.amount, num);
						if (num2 > 0)
						{
							int num3 = Mathf.Min(stockInstance.GetMaxStockCapacity(itemControllerAtIndex.ItemInstance) - stockInstance.amount, num2);
							stockInstance.amount += num3;
							itemControllerAtIndex.ItemInstance.OnItemsInCargoUpdated()?.Invoke();
							movedItems += num3;
							num -= num3;
							if (num3 < num2)
							{
								break;
							}
						}
					}
					return;
				}
			}
			Producer producer = itemControllerAtIndex as Producer;
			if ((bool)producer && !producer.CanAddAnyToInventory(cargoInstances))
			{
				if (notify)
				{
					IInteriorDesignerTool.showErrorNotificationWithItem?.Invoke("interiordesigner_package_cannot_store", targetCargoHolderIndex);
				}
				return;
			}
		}
		else if (GetVehicleControllerAtIndex(targetCargoHolderIndex).vehicleInstance.IsCargoFull())
		{
			if (notify)
			{
				IInteriorDesignerTool.showErrorNotificationWithVehicle?.Invoke("interiordesigner_package_cargoholder_full", targetCargoHolderIndex);
			}
			return;
		}
		int amountByItemName = targetCargoHolder.GetAmountByItemName(itemName);
		targetCargoHolder.TryToAddToCargo(new CargoInstance(itemName, amount, cargoInstances[0].pricePerUnit));
		int amountByItemName2 = targetCargoHolder.GetAmountByItemName(itemName);
		movedItems = Mathf.Max(0, amountByItemName2 - amountByItemName);
	}

	private void RemoveItemsFromInventory(bool isVehicle, int cargoHolderIndex, ICargoHolder cargoHolder, string itemName, int itemsToMove, List<CargoInstance> cargoInstances, out int removedItems)
	{
		removedItems = 0;
		if (!isVehicle)
		{
			ItemInstance itemInstance = GetItemControllerAtIndex(cargoHolderIndex).ItemInstance;
			if (itemInstance.ItemCached.IsStockCarrier())
			{
				CargoInstance stockInstance = itemInstance.GetStockInstance();
				if (stockInstance != null && stockInstance.itemName == itemName)
				{
					removedItems = Mathf.Min(stockInstance.amount, itemsToMove);
					stockInstance.amount -= removedItems;
					itemInstance.OnItemsInCargoUpdated()?.Invoke();
				}
				return;
			}
		}
		cargoInstances.Reverse();
		int num = itemsToMove;
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			if (num <= 0)
			{
				break;
			}
			int amountByItemName = cargoHolder.GetAmountByItemName(cargoInstance.itemName);
			int num2 = Mathf.Min(cargoInstance.amount, num);
			if (num2 > 0)
			{
				cargoHolder.ReduceFromCargo(cargoInstance, num2);
				int amountByItemName2 = cargoHolder.GetAmountByItemName(cargoInstance.itemName);
				int num3 = amountByItemName - amountByItemName2;
				if (num3 > 0)
				{
					removedItems += num3;
					num -= num3;
				}
			}
		}
	}

	private static void AddToRestrictedTarget(ICargoHolder targetCargoHolder, CargoInstance cargoInstance)
	{
		targetCargoHolder.MergeIntoCargo(cargoInstance);
		if (cargoInstance.amount > 0)
		{
			targetCargoHolder.AddToCargo(cargoInstance);
		}
	}
}
