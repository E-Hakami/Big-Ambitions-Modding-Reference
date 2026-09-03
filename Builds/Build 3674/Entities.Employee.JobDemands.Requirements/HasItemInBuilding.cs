using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using HGAttributes;
using Streets;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "HasItemInBuilding", menuName = "BigAmbitions/Employees/JobDemand/HasItemInBuilding")]
public class HasItemInBuilding : JobDemand, IEquipmentDemand
{
	[Header("Building Item Demand")]
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string[] itemNames;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		return CheckIfItemIsInBuildingAndIsFilled(instance, itemNames);
	}

	private static bool CheckIfItemIsInBuildingAndIsFilled(EmployeeInstance employeeInstance, string[] itemNames)
	{
		if (employeeInstance.assignedAddress == null || employeeInstance.assignedAddress.IsUndefined())
		{
			return false;
		}
		foreach (ItemInstance value in ItemHelper.GetItemsByAddress(employeeInstance.assignedAddress).Values)
		{
			for (int i = 0; i < itemNames.Length; i++)
			{
				if (itemNames[i] != value.itemName)
				{
					continue;
				}
				if (value.ItemCached.itemsThatCanShowcase.Length == 0)
				{
					return true;
				}
				foreach (CargoInstance cargoInstance in value.cargoInstances)
				{
					if (!string.IsNullOrEmpty(cargoInstance.itemName) && cargoInstance.amount > 0)
					{
						return true;
					}
				}
				break;
			}
		}
		return false;
	}
}
