using System;
using System.Collections.Generic;
using System.Linq;
using HGAttributes;
using UnityEngine;

namespace Vehicles.VehicleTypes;

public static class VehicleTypeHelper
{
	public const string AddressableLabel = "VehicleTypes";

	private static readonly Dictionary<string, VehicleType> VehicleTypes = new Dictionary<string, VehicleType>();

	private static readonly Dictionary<string, VehicleType> ModVehicleTypes = new Dictionary<string, VehicleType>(StringComparer.Ordinal);

	[AutocompleteProvider("VehicleTypes")]
	private static IEnumerable<string> VehicleTypeNames => VehicleTypes.Values.Select((VehicleType x) => x.vehicleTypeName);

	public static void OnVehicleTypesLoaded(IList<VehicleType> vehicleTypes)
	{
		VehicleTypes.Clear();
		VehicleTypes.EnsureCapacity(vehicleTypes.Count + ModVehicleTypes.Count);
		foreach (VehicleType vehicleType in vehicleTypes)
		{
			AddVehicleTypeInternal(vehicleType);
		}
		List<VehicleType> list = new List<VehicleType>();
		foreach (VehicleType value in ModVehicleTypes.Values)
		{
			if (VehicleTypes.ContainsKey(value.vehicleTypeName))
			{
				Debug.LogWarning("VehicleTypeHelper.OnVehicleTypesLoaded: mod vehicle type '" + value.vehicleTypeName + "' collides with a vanilla vehicle type; vanilla wins and the mod vehicle type is skipped for this load.");
				list.Add(value);
			}
			else
			{
				AddVehicleTypeInternal(value);
			}
		}
		foreach (VehicleType item in list)
		{
			ModVehicleTypes.Remove(item.vehicleTypeName);
		}
	}

	internal static bool RegisterModVehicleType(VehicleType vehicleType)
	{
		if (vehicleType == null || string.IsNullOrEmpty(vehicleType.vehicleTypeName))
		{
			Debug.LogError("VehicleTypeHelper.RegisterModVehicleType: vehicle type or vehicleTypeName is null/empty.");
			return false;
		}
		if (VehicleTypes.TryGetValue(vehicleType.vehicleTypeName, out var value) && !ModVehicleTypes.ContainsKey(vehicleType.vehicleTypeName))
		{
			Debug.LogWarning("VehicleTypeHelper.RegisterModVehicleType: vehicle type '" + vehicleType.vehicleTypeName + "' is already registered by the base game; ignoring mod registration.");
			return false;
		}
		if (value != null && value != vehicleType)
		{
			Debug.LogWarning("VehicleTypeHelper.RegisterModVehicleType: vehicle type '" + vehicleType.vehicleTypeName + "' is already registered; ignoring new registration.");
			return false;
		}
		vehicleType.BuildTagCache();
		ModVehicleTypes[vehicleType.vehicleTypeName] = vehicleType;
		AddVehicleTypeInternal(vehicleType);
		return true;
	}

	internal static bool UnregisterModVehicleType(string vehicleTypeName)
	{
		if (string.IsNullOrEmpty(vehicleTypeName))
		{
			return false;
		}
		VehicleType valueOrDefault = ModVehicleTypes.GetValueOrDefault(vehicleTypeName);
		if (!ModVehicleTypes.Remove(vehicleTypeName))
		{
			return false;
		}
		if (VehicleTypes.TryGetValue(vehicleTypeName, out var value) && value == valueOrDefault)
		{
			VehicleTypes.Remove(vehicleTypeName);
		}
		return true;
	}

	public static bool IsModVehicleType(string vehicleTypeName)
	{
		if (!string.IsNullOrEmpty(vehicleTypeName))
		{
			return ModVehicleTypes.ContainsKey(vehicleTypeName);
		}
		return false;
	}

	private static void AddVehicleTypeInternal(VehicleType vehicleType)
	{
		if (!(vehicleType == null) && !string.IsNullOrEmpty(vehicleType.vehicleTypeName))
		{
			VehicleTypes.TryAdd(vehicleType.vehicleTypeName, vehicleType);
		}
	}

	public static VehicleType GetVehicleType(string vehicleTypeName)
	{
		VehicleTypes.TryGetValue(vehicleTypeName, out var value);
		return value;
	}

	public static List<string> GetVehicleTypeNames()
	{
		return new List<string>(VehicleTypes.Keys);
	}

	public static bool IsValidVehicleType(string vehicleTypeName)
	{
		return VehicleTypes.ContainsKey(vehicleTypeName);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		VehicleTypes.Clear();
		ModVehicleTypes.Clear();
	}
}
