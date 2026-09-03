using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixAttachedItemsBrokenByMovingService : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		EmployeeHelper.EnsureInit(gameInstance);
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			ItemInstance value = buildingRegistration.itemInstances.FirstOrDefault((KeyValuePair<string, ItemInstance> x) => x.Value.itemName == "ba:itemname_deliveryspot").Value;
			for (int num = buildingRegistration.itemInstances.Count - 1; num >= 0; num--)
			{
				ItemInstance value2 = buildingRegistration.itemInstances.ElementAt(num).Value;
				if (value2.stackedItems.Count > 0)
				{
					for (int num2 = value2.stackedItems.Count - 1; num2 >= 0; num2--)
					{
						AttachableChild attachableChild = value2.stackedItems[num2];
						if (buildingRegistration.itemInstances.TryGetValue(attachableChild.childId, out var value3) && !(Vector3.Distance(value2.position, value3.position) < 5f) && value != null)
						{
							MoveItemToDeliverySpot(value, value2, value3, buildingRegistration);
						}
					}
				}
			}
		}
	}

	private void MoveItemToDeliverySpot(ItemInstance deliverySpot, ItemInstance parentItemInstance, ItemInstance childItemInstance, BuildingRegistration registration)
	{
		parentItemInstance.stackedItems.RemoveAll((AttachableChild x) => childItemInstance.id == x.childId);
		if (childItemInstance.cargoInstances.Count > 0)
		{
			MoveCargoFromHolderToDeliverySpot(childItemInstance, deliverySpot);
		}
		childItemInstance.RemoveFromWorkShifts(registration.Address);
		deliverySpot.AddToCargo(new CargoInstance(childItemInstance.itemName, 1, childItemInstance.priceOnPurchase));
		registration.RemoveItemInstanceFromBuilding(childItemInstance);
	}

	private static void MoveCargoFromHolderToDeliverySpot(ICargoHolder cargoHolder, ItemInstance deliverySpot)
	{
		List<CargoInstance> cargoInstances = cargoHolder.GetCargoInstances();
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			cargoInstances[num].TryToMoveCargoBetweenHolders(cargoHolder, deliverySpot);
		}
	}
}
