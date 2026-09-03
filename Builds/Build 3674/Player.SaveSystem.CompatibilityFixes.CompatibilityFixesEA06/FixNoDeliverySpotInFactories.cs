using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Special.FurnitureStore;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class FixNoDeliverySpotInFactories : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		ItemHelper.Init();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			ItemInstance value = buildingRegistration.itemInstances.FirstOrDefault((KeyValuePair<string, ItemInstance> x) => x.Value.itemName == "ba:itemname_deliveryspot").Value;
			if (value != null && !(value.position != Vector3.zero))
			{
				FurnitureDeliveryHelper.PlaceDeliverySpotOnDefaultPosition(buildingRegistration, value);
			}
		}
	}
}
