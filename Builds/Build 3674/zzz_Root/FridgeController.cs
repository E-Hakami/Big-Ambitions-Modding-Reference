using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;
using Player.HUD.ItemInfoOverlays;
using UI.Notification;

public class FridgeController : ItemController
{
	public bool CanStoreAny(ItemInstance itemInstance)
	{
		if (itemInstance != null)
		{
			return CanStoreAny(itemInstance.cargoInstances);
		}
		return false;
	}

	public bool CanStoreAny(List<CargoInstance> cargoInstancesToStore)
	{
		if (cargoInstancesToStore != null && cargoInstancesToStore.Count > 0)
		{
			return cargoInstancesToStore.Exists(CanStore);
		}
		return false;
	}

	public override void Start()
	{
		base.Start();
		base.ItemInstance.AddCallToOnItemsInCargoUpdated(OnItemsInCargoUpdated);
	}

	public override bool ShouldShowDetailedOverlay()
	{
		return base.ItemInstance.cargoInstances.Count > 0;
	}

	public void EmptyFridge()
	{
		MoveTowardsEntity(delegate
		{
			if (base.ItemInstance.cargoInstances.Count != 0)
			{
				VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
				if (currentVehicle != null)
				{
					if (!ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(base.ItemInstance, currentVehicle))
					{
						Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", itemName } };
						Notifications.Show(NotificationType.Error, "fridge_notification_cant_fit_some_items", notificationData);
					}
				}
				else if (!PlayerHelper.IsHoldingItem)
				{
					ItemInstance itemInstance = ItemHelper.InitializeNewInstance(ItemsGetter.GetRandomBag());
					ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(base.ItemInstance, itemInstance);
					PlayerHelper.ItemInstanceInHands = itemInstance;
				}
				else if (!ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(base.ItemInstance, PlayerHelper.ItemInstanceInHands))
				{
					Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "itemname", itemName } };
					Notifications.Show(NotificationType.Error, "fridge_notification_empty_hands_to_empty", notificationData2);
				}
			}
		});
	}

	public void ConsumeItem(CargoInstance cargoInstance)
	{
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		MoveTowardsEntity(delegate
		{
			if (cargoInstance.ItemCached.TryToConsume())
			{
				base.ItemInstance.ReduceFromCargo(cargoInstance, 1);
			}
		});
	}

	public void TryAddToStorage()
	{
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle != null && !currentVehicle.VehicleType.IsMotorVehicle)
		{
			WalkOverAndAddToStorage(currentVehicle);
		}
		else if (PlayerHelper.IsHoldingItem)
		{
			WalkOverAndAddToStorage(PlayerHelper.ItemInstanceInHands);
		}
	}

	public void WalkOverAndAddToStorage(ItemInstance itemInstance)
	{
		MoveTowardsEntity(delegate
		{
			AddToStorage(itemInstance);
		});
	}

	public void WalkOverAndAddToStorage(VehicleInstance vehicleInstance)
	{
		MoveTowardsEntity(delegate
		{
			AddToStorage(vehicleInstance);
		});
	}

	private void AddToStorage(ICargoHolder cargoHolder)
	{
		if (cargoHolder.GetCargoInstances().Exists((CargoInstance x) => x.IsSealed))
		{
			Notifications.Show(NotificationType.Error, "notification_cant_tamper_sealed_box");
			return;
		}
		if (!CanStoreAny(cargoHolder.GetCargoInstances()))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", itemName } };
			Notifications.Show(NotificationType.Error, "fridge_notification_cant_store_item", notificationData);
		}
		else if (!TryToMoveCargoToFridge(cargoHolder))
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "itemname", itemName } };
			Notifications.Show(NotificationType.Info, "fridge_notification_cant_fit_some_items", notificationData2);
		}
		InstanceBehavior<OverlayManager>.Instance?.HideDetailedOverlay();
		GameEvent.Invoke("ba:gameevent_itemstockedup");
	}

	private bool TryToMoveCargoToFridge(ICargoHolder cargoHolder)
	{
		bool result = false;
		List<CargoInstance> cargoInstances = cargoHolder.GetCargoInstances();
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = cargoInstances[num];
			if (TryToMoveCargoToFridge(cargoInstance, out var shouldRemoveSourceCargo))
			{
				result = true;
				if (shouldRemoveSourceCargo || ShouldDiscardEmptyContainer(cargoInstance))
				{
					cargoHolder.RemoveFromCargo(cargoInstance);
				}
				else
				{
					cargoHolder.OnItemsInCargoUpdated()?.Invoke();
				}
			}
		}
		DiscardEmptyContainerInHands(cargoHolder);
		return result;
	}

	private bool TryToMoveCargoToFridge(CargoInstance cargoInstance, out bool shouldRemoveSourceCargo)
	{
		shouldRemoveSourceCargo = false;
		if (cargoInstance == null)
		{
			return false;
		}
		bool num = cargoInstance.nestedCargoInstances.Count > 0;
		bool flag = TryToMoveNestedCargoToFridge(cargoInstance);
		if (num && cargoInstance.nestedCargoInstances.Count > 0)
		{
			return flag;
		}
		if (cargoInstance.ItemCached.saturation <= 0)
		{
			return flag;
		}
		return TryToMoveCargoStackToFridge(cargoInstance, out shouldRemoveSourceCargo) | flag;
	}

	private bool TryToMoveNestedCargoToFridge(CargoInstance cargoInstance)
	{
		bool result = false;
		for (int num = cargoInstance.nestedCargoInstances.Count - 1; num >= 0; num--)
		{
			NestedCargoInstance nestedCargoInstance = cargoInstance.nestedCargoInstances[num];
			if (nestedCargoInstance != null)
			{
				CargoInstance cargoInstance2 = nestedCargoInstance.ConvertToCargoInstance();
				if (TryToMoveCargoToFridge(cargoInstance2, out var shouldRemoveSourceCargo))
				{
					result = true;
					if (shouldRemoveSourceCargo || ShouldDiscardEmptyContainer(cargoInstance2))
					{
						cargoInstance.nestedCargoInstances.RemoveAt(num);
					}
					else
					{
						nestedCargoInstance.amount = cargoInstance2.amount;
					}
				}
			}
		}
		return result;
	}

	private bool TryToMoveCargoStackToFridge(CargoInstance cargoInstance, out bool shouldRemoveSourceCargo)
	{
		shouldRemoveSourceCargo = false;
		int amount = cargoInstance.amount;
		base.ItemInstance.MergeIntoCargo(cargoInstance);
		if (cargoInstance.amount <= 0)
		{
			shouldRemoveSourceCargo = true;
			return true;
		}
		if (base.ItemInstance.TryToAddToCargo(cargoInstance))
		{
			shouldRemoveSourceCargo = true;
			return true;
		}
		return amount > cargoInstance.amount;
	}

	private static bool ShouldDiscardEmptyContainer(CargoInstance cargoInstance)
	{
		if (cargoInstance.nestedCargoInstances.Count == 0)
		{
			return cargoInstance.ItemCached.HasTag(TagRef.Itemtag.discardcontainerwhenempty);
		}
		return false;
	}

	private static void DiscardEmptyContainerInHands(ICargoHolder cargoHolder)
	{
		if (cargoHolder is ItemInstance itemInstance && PlayerHelper.ItemInstanceInHands == itemInstance && itemInstance.cargoInstances.Count <= 0 && itemInstance.ItemCached.HasTag(TagRef.Itemtag.discardcontainerwhenempty))
		{
			PlayerHelper.ItemInstanceInHands = null;
		}
	}

	private static bool CanStore(CargoInstance cargoInstance)
	{
		if (cargoInstance == null)
		{
			return false;
		}
		if (cargoInstance.ItemCached.saturation > 0)
		{
			return true;
		}
		if (cargoInstance.nestedCargoInstances.Exists((NestedCargoInstance x) => x != null && CanStore(x.ConvertToCargoInstance())))
		{
			return true;
		}
		return false;
	}

	private void OnItemsInCargoUpdated()
	{
		if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.ShowDetailedOverlay(this);
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			base.OnDestroy();
			base.ItemInstance.RemoveCallFromOnItemsInCargoUpdated(OnItemsInCargoUpdated);
		}
	}
}
