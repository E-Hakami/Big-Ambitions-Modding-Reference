using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings.Schedule;
using HGAttributes;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "WorksOnItem", menuName = "BigAmbitions/Employees/JobDemand/WorksOnItem")]
public class WorksOnItem : JobDemand, IEquipmentDemand, IScheduleDemand, IWorkStationFilter
{
	[Header("Workstation Item Demand")]
	[AutocompleteDropdown("Items")]
	public string[] itemNames;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		string[] array = itemNames;
		foreach (string item in array)
		{
			if (instance.assignedWorkStationItems.Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool AcceptsWorkStation(WorkStationInfo workStation, BuildingRegistration buildingRegistration)
	{
		ItemInstance itemInstance = workStation.itemInstance;
		if (itemNames.Contains(itemInstance.itemName))
		{
			return true;
		}
		if (!string.IsNullOrEmpty(itemInstance.parentId) && buildingRegistration.itemInstances.TryGetValue(itemInstance.parentId, out var value))
		{
			itemInstance = value;
			if (itemNames.Contains(itemInstance.itemName))
			{
				return true;
			}
		}
		foreach (AttachableChild stackedItem in itemInstance.stackedItems)
		{
			if (itemNames.Contains(stackedItem.childItemName))
			{
				return true;
			}
		}
		return false;
	}
}
