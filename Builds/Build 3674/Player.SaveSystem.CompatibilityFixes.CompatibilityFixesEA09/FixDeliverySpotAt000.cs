using System.Linq;
using BigAmbitions.Items;
using Streets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class FixDeliverySpotAt000 : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			ItemInstance itemInstance = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot" && x.position == Vector3.zero);
			if (itemInstance == null)
			{
				continue;
			}
			buildingRegistration.itemInstances.Remove(itemInstance.id);
			ItemInstance itemInstance2 = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot");
			if (itemInstance2 == null)
			{
				Debug.LogError("No delivery spot found at " + buildingRegistration.Address.ToFormattedString());
				continue;
			}
			foreach (CargoInstance cargoInstance in itemInstance.cargoInstances)
			{
				itemInstance2.AddToCargo(cargoInstance);
			}
		}
	}
}
