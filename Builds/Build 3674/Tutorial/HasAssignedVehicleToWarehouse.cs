using Entities;
using HGAttributes;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Warehouse/HasAssignedVehicleToWarehouse")]
public class HasAssignedVehicleToWarehouse : QuestRequirement
{
	public bool anyVehicleType;

	[HideIf("anyVehicleType")]
	[AutocompleteDropdown("VehicleTypes")]
	public string[] vehicleTypes;

	public override bool CheckIfCompleted()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || !(buildingRegistration is Warehouse { vehicleSlots: not null } warehouse) || warehouse.vehicleSlots.Count == 0)
			{
				continue;
			}
			if (anyVehicleType)
			{
				foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
				{
					if (vehicleSlot.vehicleInstanceId != null)
					{
						return true;
					}
				}
				continue;
			}
			foreach (VehicleSlot vehicleSlot2 in warehouse.vehicleSlots)
			{
				if (string.IsNullOrEmpty(vehicleSlot2.vehicleInstanceId))
				{
					continue;
				}
				VehicleInstance vehicleInstance = null;
				foreach (VehicleInstance vehicleInstance2 in SaveGameManager.Current.VehicleInstances)
				{
					if (vehicleInstance2.id == vehicleSlot2.vehicleInstanceId)
					{
						vehicleInstance = vehicleInstance2;
						break;
					}
				}
				if (vehicleInstance == null)
				{
					continue;
				}
				string[] array = vehicleTypes;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == vehicleInstance.vehicleTypeName)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
