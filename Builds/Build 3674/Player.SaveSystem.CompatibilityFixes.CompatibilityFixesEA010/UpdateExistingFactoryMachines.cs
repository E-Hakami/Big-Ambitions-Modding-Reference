using System.Linq;
using BigAmbitions.Items;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateExistingFactoryMachines : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			for (int i = 0; i < buildingRegistration.itemInstances.Count; i++)
			{
				ItemInstance itemInstance = buildingRegistration.itemInstances.Values.ElementAt(i);
				string itemName = itemInstance.itemName;
				if ((itemName == "ba:itemname_consumergoodsassemblymachine" || itemName == "ba:itemname_foodassemblymachine") && itemInstance != null && !(itemInstance is FactoryWorkstationInstance))
				{
					FactoryWorkstationInstance value = new FactoryWorkstationInstance(itemInstance.itemName)
					{
						position = itemInstance.position,
						rotation = itemInstance.rotation,
						id = itemInstance.id,
						yRotation = itemInstance.yRotation,
						customColors = itemInstance.customColors,
						customPositions = itemInstance.customPositions,
						itemName = itemInstance.itemName,
						streetName = itemInstance.streetName,
						streetNumber = itemInstance.streetNumber,
						dirtSpotsThatAffects = itemInstance.dirtSpotsThatAffects
					};
					buildingRegistration.itemInstances[itemInstance.id] = value;
				}
			}
		}
		Debug.Log("Updated existing factory machines");
	}
}
