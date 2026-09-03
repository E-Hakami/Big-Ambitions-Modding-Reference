using System;
using System.Runtime.Serialization;
using Buildings;
using Helpers;

[Serializable]
public class BuildingForSale
{
	public Address address;

	public float buildingPrice;

	public int squareMeters;

	public float acceptOfferRate;

	[IgnoreDataMember]
	public float PricePerSquareMeter => buildingPrice / (float)squareMeters;

	[IgnoreDataMember]
	public Building Building => BuildingHelper.GetBuilding(address);

	[IgnoreDataMember]
	public BuildingRegistration BuildingRegistration => BuildingHelper.GetBuildingRegistration(address);

	[IgnoreDataMember]
	public string Neighbourhood => Building.Neighbourhood;

	[IgnoreDataMember]
	public string BuildingType => Building.BuildingType;
}
