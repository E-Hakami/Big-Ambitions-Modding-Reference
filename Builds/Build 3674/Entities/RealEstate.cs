using System;
using System.Runtime.Serialization;
using Buildings;
using Helpers;
using UnityEngine;

namespace Entities;

[Serializable]
public class RealEstate
{
	public Address address;

	public double purchasePrice;

	public int purchaseDay;

	public int totalSqm;

	public float occupancy;

	public float pricePerSqm;

	public float pendingPricePerSqm;

	public int daysUntilUpdatingPricePerSqm;

	[IgnoreDataMember]
	public int OccupancyPercentage => Math.Min(Mathf.RoundToInt(occupancy), MaxOccupancy);

	[IgnoreDataMember]
	public float DailyIncome => pricePerSqm * (float)totalSqm * ((float)OccupancyPercentage / 100f);

	[IgnoreDataMember]
	public int MaxOccupancy => 100 - Mathf.RoundToInt((BuildingRegistration.RentedByPlayer ? ((float)BuildingSizeHelper.GetData(Building).squareMeters) : 0f) / (float)totalSqm);

	[IgnoreDataMember]
	public Building Building => BuildingHelper.GetBuilding(address);

	[IgnoreDataMember]
	public BuildingRegistration BuildingRegistration => BuildingHelper.GetBuildingRegistration(address);

	[IgnoreDataMember]
	public float TaxesAmount => (float)totalSqm * BuildingTypeHelper.GetData(Building).buildingPriceMultiplier * NeighborhoodHelper.GetData(Building.Neighbourhood).realEstateMultiplier * 3.5f;
}
