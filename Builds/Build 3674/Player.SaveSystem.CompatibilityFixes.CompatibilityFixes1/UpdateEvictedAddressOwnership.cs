using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Entities;
using Streets;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class UpdateEvictedAddressOwnership : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Dictionary<string, BuildingRegistration> registrationCache = new Dictionary<string, BuildingRegistration>();
		Dictionary<string, Address> evictedAddresses = new Dictionary<string, Address>();
		UpdateEmployeeAssignments(gameInstance, registrationCache, evictedAddresses);
		UpdateTodoTasks(gameInstance, registrationCache, evictedAddresses);
		UpdateLogisticsDestinations(gameInstance, registrationCache, evictedAddresses);
		UpdateEvictedAddresses(evictedAddresses);
	}

	private static void UpdateEmployeeAssignments(GameInstance gameInstance, Dictionary<string, BuildingRegistration> registrationCache, Dictionary<string, Address> evictedAddresses)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			BuildingRegistration registration = GetRegistration(gameInstance, employeeInstance.assignedAddress, registrationCache);
			if (registration == null || !registration.RentedByPlayer)
			{
				AddEvictedAddress(employeeInstance.assignedAddress, registration, evictedAddresses);
				employeeInstance.assignedAddress = null;
			}
		}
	}

	private static void UpdateTodoTasks(GameInstance gameInstance, Dictionary<string, BuildingRegistration> registrationCache, Dictionary<string, Address> evictedAddresses)
	{
		for (int num = gameInstance.TodoTasks.Count - 1; num >= 0; num--)
		{
			TodoTask todoTask = gameInstance.TodoTasks[num];
			if (!todoTask.address.IsUndefined())
			{
				BuildingRegistration registration = GetRegistration(gameInstance, todoTask.address, registrationCache);
				if (registration == null || !registration.RentedByPlayer)
				{
					AddEvictedAddress(todoTask.address, registration, evictedAddresses);
					gameInstance.TodoTasks.RemoveAt(num);
				}
			}
		}
	}

	private static BuildingRegistration GetRegistration(GameInstance gameInstance, Address address, Dictionary<string, BuildingRegistration> registrationCache)
	{
		if (address.IsUndefined())
		{
			return null;
		}
		string addressKey = GetAddressKey(address);
		if (registrationCache.TryGetValue(addressKey, out var value))
		{
			return value;
		}
		value = GetRegistration(address, gameInstance.BuildingRegistrations);
		registrationCache.Add(addressKey, value);
		return value;
	}

	private static BuildingRegistration GetRegistration(Address address, List<BuildingRegistration> buildingRegistrations)
	{
		for (int i = 0; i < buildingRegistrations.Count; i++)
		{
			BuildingRegistration buildingRegistration = buildingRegistrations[i];
			if (buildingRegistration.StreetName == address.streetName && buildingRegistration.StreetNumber == address.streetNumber)
			{
				return buildingRegistration;
			}
		}
		return null;
	}

	private static void AddEvictedAddress(Address address, BuildingRegistration registration, Dictionary<string, Address> evictedAddresses)
	{
		if (!address.IsUndefined() && registration != null && !registration.RentedByPlayer)
		{
			string addressKey = GetAddressKey(address);
			evictedAddresses.TryAdd(addressKey, address);
		}
	}

	private static void UpdateLogisticsDestinations(GameInstance gameInstance, Dictionary<string, BuildingRegistration> registrationCache, Dictionary<string, Address> evictedAddresses)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in gameInstance.logisticsManagerPlans)
		{
			foreach (LogisticsManagerPlanDestination destination in logisticsManagerPlan.destinations)
			{
				BuildingRegistration registration = GetRegistration(gameInstance, destination.deliveryTargetAddress, registrationCache);
				if (!logisticsManagerPlan.isFactory || !(registration?.businessTypeName == "ba:businesstype_importexport"))
				{
					AddEvictedAddress(destination.deliveryTargetAddress, registration, evictedAddresses);
				}
			}
		}
	}

	private static void UpdateEvictedAddresses(Dictionary<string, Address> evictedAddresses)
	{
		foreach (Address value in evictedAddresses.Values)
		{
			CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupantWithoutBlueprint(value);
		}
	}

	private static string GetAddressKey(Address address)
	{
		return $"{address.streetName}:{address.streetNumber}";
	}
}
