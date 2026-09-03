using System.Collections.Generic;
using Blueprints;
using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public sealed class FixWarehouseSlotMismatch : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (!InstanceBehavior<BuildingManager>.IsInitialized)
		{
			return;
		}
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration is Warehouse warehouse)
			{
				TryFixWarehouse(gameInstance, warehouse);
			}
		}
	}

	private static void TryFixWarehouse(GameInstance gameInstance, Warehouse warehouse)
	{
		Transform buildingTransform = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(warehouse));
		if (buildingTransform == null)
		{
			return;
		}
		WarehouseSlotController[] componentsInChildren = buildingTransform.GetComponentsInChildren<WarehouseSlotController>(includeInactive: true);
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			if (vehicleInstance.IsAtAddress(warehouse.Address) && Warehouse.CanVehicleOccupySlot(vehicleInstance))
			{
				FixVehicleSlot(warehouse, componentsInChildren, vehicleInstance);
			}
		}
		ClearSlotsAssignedToIneligibleVehicles(gameInstance, warehouse);
	}

	private static void FixVehicleSlot(Warehouse warehouse, WarehouseSlotController[] slotControllers, VehicleInstance vehicleInstance)
	{
		Vector3 vehiclePosition = new Vector3(vehicleInstance.position.x, vehicleInstance.position.y, vehicleInstance.position.z);
		WarehouseSlotController warehouseSlotController = FindNearestController(slotControllers, vehiclePosition);
		if (!(warehouseSlotController == null))
		{
			int slotIndex = warehouseSlotController.slotIndex;
			if (slotIndex >= 1 && slotIndex <= warehouse.vehicleSlots.Count && (!(warehouse.vehicleSlots[slotIndex - 1].vehicleInstanceId == vehicleInstance.id) || warehouse.HasDuplicateSlotAssignment(vehicleInstance.id)))
			{
				warehouse.ClearSlotAssignments(vehicleInstance.id);
				warehouse.vehicleSlots[slotIndex - 1].AssignVehicle(vehicleInstance);
			}
		}
	}

	private static void ClearSlotsAssignedToIneligibleVehicles(GameInstance gameInstance, Warehouse warehouse)
	{
		Dictionary<string, VehicleInstance> dictionary = BuildVehicleMap(gameInstance);
		foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
		{
			if (!string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId) && (!dictionary.TryGetValue(vehicleSlot.vehicleInstanceId, out var value) || !Warehouse.CanVehicleOccupySlot(value)))
			{
				vehicleSlot.vehicleInstanceId = null;
			}
		}
	}

	private static Dictionary<string, VehicleInstance> BuildVehicleMap(GameInstance gameInstance)
	{
		Dictionary<string, VehicleInstance> dictionary = new Dictionary<string, VehicleInstance>();
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			dictionary[vehicleInstance.id] = vehicleInstance;
		}
		return dictionary;
	}

	private static WarehouseSlotController FindNearestController(WarehouseSlotController[] slotControllers, Vector3 vehiclePosition)
	{
		WarehouseSlotController result = null;
		float num = float.MaxValue;
		foreach (WarehouseSlotController warehouseSlotController in slotControllers)
		{
			if (!(warehouseSlotController.vehiclePosition == null))
			{
				float sqrMagnitude = (warehouseSlotController.vehiclePosition.position - vehiclePosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = warehouseSlotController;
				}
			}
		}
		return result;
	}
}
