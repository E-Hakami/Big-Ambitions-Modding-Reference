using System;

namespace City.CityMap;

public class CityMapFilterData
{
	public string buildingType;

	public string businessTypeName;

	public string neighbourhood;

	public string rivalId;

	public Address address;

	public Func<bool> isAvailable;
}
