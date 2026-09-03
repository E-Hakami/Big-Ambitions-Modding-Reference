using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Blueprints;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class ConvertHandTruckSpawnersToItemInstances : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer))
		{
			if (item.handTruckSpawnerData != null && item.handTruckSpawnerData.position != Vector3.zero)
			{
				ItemInstance itemInstance = ItemHelper.InitializeNewInstance("ba:itemname_handtruckspawner");
				itemInstance.position = item.handTruckSpawnerData.position;
				itemInstance.rotation = item.handTruckSpawnerData.rotation;
				item.AddItemInstanceToBuilding(itemInstance);
			}
			else
			{
				if (!InstanceBehavior<BuildingManager>.Instance)
				{
					continue;
				}
				List<VehicleSpawnerController> list = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(item))?.GetComponentsInChildren<VehicleSpawnerController>(includeInactive: true).Where((VehicleSpawnerController x) => x.itemName == "ba:itemname_handtruckspawner" && !x.gameObject.activeSelf).ToList();
				if (list == null)
				{
					continue;
				}
				foreach (VehicleSpawnerController item2 in list)
				{
					ItemInstance itemInstance2 = ItemHelper.InitializeNewInstance("ba:itemname_handtruckspawner");
					itemInstance2.position = item2.transform.position;
					itemInstance2.rotation = item2.transform.rotation;
					item.AddItemInstanceToBuilding(itemInstance2);
				}
			}
		}
	}
}
