using Buildings;
using Helpers;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace BAModAPI;

public static class ModdingAPI
{
	public static bool RegisterModBusinessType(BusinessType businessType)
	{
		if (businessType == null)
		{
			Debug.LogError("ModdingAPI.RegisterModBusinessType: business type is null.");
			return false;
		}
		if (string.IsNullOrEmpty(businessType.businessTypeName))
		{
			Debug.LogError("ModdingAPI.RegisterModBusinessType: businessTypeName is null/empty.");
			return false;
		}
		if (string.IsNullOrEmpty(businessType.suitableBuildingType))
		{
			Debug.LogError("ModdingAPI.RegisterModBusinessType: suitableBuildingType is null/empty for '" + businessType.businessTypeName + "'.");
			return false;
		}
		if (!BusinessTypeHelper.RegisterModBusinessType(businessType))
		{
			return false;
		}
		if (BuildingTypeHelper.RegisterModBusinessTypeAvailability(businessType.suitableBuildingType, businessType.businessTypeName))
		{
			return true;
		}
		BusinessTypeHelper.UnregisterModBusinessType(businessType.businessTypeName);
		return false;
	}

	public static bool UnregisterModBusinessType(BusinessType businessType)
	{
		if (businessType != null)
		{
			return UnregisterModBusinessType(businessType.businessTypeName);
		}
		return false;
	}

	public static bool UnregisterModBusinessType(string businessTypeName)
	{
		BusinessType modBusinessType = BusinessTypeHelper.GetModBusinessType(businessTypeName);
		if (modBusinessType == null)
		{
			return false;
		}
		string suitableBuildingType = modBusinessType.suitableBuildingType;
		bool num = BusinessTypeHelper.UnregisterModBusinessType(businessTypeName);
		bool flag = BuildingTypeHelper.UnregisterModBusinessTypeAvailability(suitableBuildingType, businessTypeName);
		return num | flag;
	}

	public static bool RegisterModVehicleType(VehicleType vehicleType)
	{
		if (vehicleType == null)
		{
			Debug.LogError("ModdingAPI.RegisterModVehicleType: vehicle type is null.");
			return false;
		}
		if (string.IsNullOrEmpty(vehicleType.vehicleTypeName))
		{
			Debug.LogError("ModdingAPI.RegisterModVehicleType: vehicleTypeName is null/empty.");
			return false;
		}
		return VehicleTypeHelper.RegisterModVehicleType(vehicleType);
	}

	public static bool UnregisterModVehicleType(string vehicleTypeName)
	{
		return VehicleTypeHelper.UnregisterModVehicleType(vehicleTypeName);
	}
}
