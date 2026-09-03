using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using UI.Notification;
using UnityEngine;
using Vehicles.VehicleTypes;

[Serializable]
public class VehicleInstance : ICargoHolder
{
	public const int MaxDeformationEntries = 20;

	public const float MinDeformationMagnitude = 200f;

	public string id;

	public string vehicleTypeName;

	public SerializableVector3 position;

	public SerializableQuaternion rotation;

	public string streetName;

	public int streetNumber;

	public List<CargoInstance> cargoInstances = new List<CargoInstance>();

	public string vehicleColorName;

	public ParkingState parkingState = ParkingState.Legal;

	public string parkingNeighbourhood;

	public float unpaidParkingAmount;

	public List<Timestamp> parkingTickets = new List<Timestamp>();

	public Timestamp lastSeen;

	public float dirtiness;

	public float fuel;

	public List<VehicleDeformationController.VehicleDeformation> deformations = new List<VehicleDeformationController.VehicleDeformation>();

	public float damage;

	public bool zeroSellingPrice;

	public bool preventWarehouseAssign;

	[NonSerialized]
	private Action _onItemsInCargoUpdated;

	[Obsolete("Since EA 0.8")]
	public List<string> cargoIds;

	[IgnoreDataMember]
	public Address Address => new Address(streetName, streetNumber);

	[IgnoreDataMember]
	public VehicleType VehicleType => VehicleTypeHelper.GetVehicleType(vehicleTypeName);

	public List<CargoInstance> GetCargoInstances()
	{
		return cargoInstances;
	}

	public Action OnItemsInCargoUpdated()
	{
		return _onItemsInCargoUpdated;
	}

	public void AddCallToOnItemsInCargoUpdated(Action callToAdd)
	{
		_onItemsInCargoUpdated = (Action)Delegate.Combine(_onItemsInCargoUpdated, callToAdd);
	}

	public void RemoveCallFromOnItemsInCargoUpdated(Action callToRemove)
	{
		_onItemsInCargoUpdated = (Action)Delegate.Remove(_onItemsInCargoUpdated, callToRemove);
	}

	public VehicleInstance(string vehicleTypeName)
	{
		id = UuidHelper.GenerateBase64Uuid();
		this.vehicleTypeName = vehicleTypeName;
	}

	public VehicleInstance Clone()
	{
		return new VehicleInstance(vehicleTypeName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = vehicleColorName,
			fuel = VehicleType.maxFuel
		};
	}

	public void SetStreetData(string name, int number)
	{
		streetName = name;
		streetNumber = number;
	}

	public bool IsAtAddress(Address address)
	{
		if (streetName == address.streetName)
		{
			return streetNumber == address.streetNumber;
		}
		return false;
	}

	public virtual bool TryToAddToCargo(CargoInstance cargoInstance)
	{
		if (VehicleType.maxCargoCapacity == 0)
		{
			return false;
		}
		int num = ((cargoInstance.nestedCargoInstances.Count > 0) ? 1 : cargoInstance.ItemCached.boxSize);
		if (num == 0)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < VehicleType.maxCargoCapacity; i++)
		{
			if (!cargoInstance.IsSealed && cargoInstances.Count - 1 >= i)
			{
				if (cargoInstances[i].itemName != cargoInstance.itemName || cargoInstances[i].paid != cargoInstance.paid || cargoInstances[i].IsSealed || cargoInstances[i].nestedCargoInstances.Count > 0)
				{
					continue;
				}
				int num2 = num - cargoInstances[i].amount;
				if (num2 > 0)
				{
					if (num2 >= cargoInstance.amount)
					{
						cargoInstances[i].MergeAmount(cargoInstance, cargoInstance.amount);
						_onItemsInCargoUpdated?.Invoke();
						return true;
					}
					cargoInstances[i].MergeAmount(cargoInstance, num2);
					flag = true;
				}
			}
			else
			{
				if (cargoInstance.IsSealed || cargoInstance.amount <= num)
				{
					AddToCargo(cargoInstance);
					return true;
				}
				CargoInstance cargoInstance2 = cargoInstance.Copy();
				cargoInstance2.amount = num;
				cargoInstance.amount -= num;
				AddToCargo(cargoInstance2);
			}
		}
		if (flag)
		{
			_onItemsInCargoUpdated?.Invoke();
		}
		return false;
	}

	public void AddToCargo(CargoInstance cargoInstance, int atIndex = -1)
	{
		if (atIndex >= 0 && atIndex < cargoInstances.Count)
		{
			cargoInstances.Insert(atIndex, cargoInstance);
		}
		else
		{
			cargoInstances.Add(cargoInstance);
		}
		_onItemsInCargoUpdated?.Invoke();
	}

	public void MergeIntoCargo(CargoInstance cargoInstanceToMerge)
	{
		if (cargoInstanceToMerge.IsSealed)
		{
			return;
		}
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			MergeCargo(cargoInstanceToMerge, cargoInstance, cargoInstanceToMerge.amount);
			if (cargoInstanceToMerge.amount == 0)
			{
				break;
			}
		}
	}

	public void MergeCargo(CargoInstance fromCargoInstance, CargoInstance toCargoInstance, int amount)
	{
		if (!(fromCargoInstance.itemName != toCargoInstance.itemName) && fromCargoInstance.paid == toCargoInstance.paid && !fromCargoInstance.IsSealed && !toCargoInstance.IsSealed && fromCargoInstance.nestedCargoInstances.Count <= 0 && toCargoInstance.nestedCargoInstances.Count <= 0)
		{
			int num = toCargoInstance.ItemCached.boxSize - toCargoInstance.amount;
			if (num != 0)
			{
				int amountToMerge = ((num > amount) ? amount : num);
				toCargoInstance.MergeAmount(fromCargoInstance, amountToMerge);
				_onItemsInCargoUpdated?.Invoke();
			}
		}
	}

	public void RemoveFromCargo(CargoInstance cargoInstance)
	{
		cargoInstances.Remove(cargoInstance);
		_onItemsInCargoUpdated?.Invoke();
	}

	public void ReduceFromCargo(CargoInstance cargoInstance, int amountToReduce)
	{
		if (!cargoInstance.IsSealed)
		{
			if (amountToReduce > cargoInstance.amount)
			{
				Debug.LogError("Tried to reduce from cargo more amount than it had " + $"({cargoInstance.itemName}: {cargoInstance.amount} amount, tried to reduce {amountToReduce})");
			}
			cargoInstance.amount -= amountToReduce;
			if (cargoInstance.amount <= 0)
			{
				RemoveFromCargo(cargoInstance);
			}
		}
	}

	public int GetAmountByItemName(string itemName)
	{
		int num = 0;
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			if (cargoInstance.itemName == itemName)
			{
				num += cargoInstance.amount;
			}
		}
		return num;
	}

	public int GetMaxCargoSize()
	{
		return VehicleType.maxCargoCapacity;
	}

	public void UnAssignFromWarehouse(bool showNotification)
	{
		if (!(BuildingHelper.GetBuildingRegistration(Address) is Warehouse warehouse))
		{
			return;
		}
		bool flag = false;
		foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
		{
			if (!(vehicleSlot.vehicleInstanceId != id))
			{
				vehicleSlot.vehicleInstanceId = null;
				flag = true;
				break;
			}
		}
		if (showNotification && flag)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "type", vehicleTypeName },
				{ "warehouseName", warehouse.BusinessName }
			};
			Notifications.Show(NotificationType.Success, "carcontroller_notification_warehouse_vehicle_unassigned", notificationData);
		}
	}

	public float CalculateRepairCost()
	{
		return damage * (VehicleType.price * 0.5f);
	}

	public float GetSellingPrice()
	{
		if (zeroSellingPrice || VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			return 0f;
		}
		float num = Mathf.Max(0f, VehicleType.price * ItemHelper.sellingMultiplier - CalculateRepairCost());
		foreach (CargoInstance cargoInstance in cargoInstances)
		{
			if (cargoInstance.paid)
			{
				num += cargoInstance.GetWorth();
			}
		}
		return num;
	}

	public Timestamp GetLatestParkingTicket()
	{
		return parkingTickets.OrderByDescending((Timestamp x) => x.GetTotalMinutes()).FirstOrDefault();
	}

	public bool IsCargoFull()
	{
		return cargoInstances.Count >= VehicleType.maxCargoCapacity;
	}
}
