using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveFlatbedSpawnersFromPlayerLayouts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			list.Clear();
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				string itemName = value.itemName;
				if (itemName == "ba:itemname_flatbedspawner" || itemName == "ba:itemname_flatbedspawnerstacked")
				{
					list.Add(value);
				}
			}
			if (list.Count != 0)
			{
				int num = 0;
				if (buildingRegistration.GetBuildingType() == "ba:buildingtype_warehouse" && list.Count > 0)
				{
					num = VehicleHelper.GetVehicleSpawnerTransformsInBuilding(buildingRegistration, "ba:itemname_flatbedspawnerstacked").Count + VehicleHelper.GetVehicleSpawnerTransformsInBuilding(buildingRegistration, "ba:itemname_flatbedspawner").Count;
				}
				for (int i = num; i < list.Count; i++)
				{
					buildingRegistration.itemInstances.Remove(list[i].id);
				}
			}
		}
	}
}
