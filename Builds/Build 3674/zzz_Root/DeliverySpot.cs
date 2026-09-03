using System;
using BigAmbitions.Items;
using UI;
using UnityEngine;

public class DeliverySpot : ShelfController
{
	[SerializeField]
	private GameObject deliveryPallet;

	public override void Start()
	{
		GetOrCreateBuildingContext();
		HandleLegacyDeliveredItems();
		ShowItemVisuals("ba:itemname_closedcardboardbox");
		base.Start();
	}

	public override void UpdateVisuals()
	{
		int count = base.ItemInstance.cargoInstances.Count;
		if (count == 0)
		{
			InstanceBehavior<UIs>.Instance?.playerHUD.manageCargoUI.Close();
			deliveryPallet.SetActive(value: false);
		}
		else
		{
			deliveryPallet.SetActive(value: true);
			UpdateFillState(Math.Min(1.0, (double)count / 36.0));
		}
	}

	public void ManageStorage()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
			InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Show(base.ItemInstance);
		});
	}

	private void HandleLegacyDeliveredItems()
	{
		BuildingRegistration registration = base.BuildingContext.Registration;
		if (registration.deliveredItems == null)
		{
			return;
		}
		foreach (string deliveredItem in registration.deliveredItems)
		{
			if (registration.itemInstances.TryGetValue(deliveredItem, out var value))
			{
				registration.RemoveItemInstanceFromBuilding(value);
				base.ItemInstance.AddToCargo(new CargoInstance(value.itemName, 1, value.priceOnPurchase));
			}
		}
		registration.deliveredItems = null;
	}
}
