using System;

namespace Vehicles;

[Serializable]
public class VehicleDeliveryContract
{
	public string vehicleTypeName;

	public string vehicleColor;

	public int deliveryDay;

	public int deliveryHour;

	public Address deliveryAddress;

	public Address vehicleStoreAddress;

	public float deliveryPrice;
}
