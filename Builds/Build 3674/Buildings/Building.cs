using BigAmbitions.Neighborhoods;
using BigAmbitions.Tags;
using HGAttributes;
using Helpers;
using NaughtyAttributes;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/Building")]
public class Building : ScriptableObject
{
	[AutocompleteDropdown("StreetNames")]
	public string StreetName;

	public int StreetNumber;

	[AutocompleteDropdown("BuildingSizes")]
	public string BuildingSize;

	public int BuildingVersion = 1;

	[AutocompleteDropdown("BuildingTypes")]
	public string BuildingType;

	[Expandable]
	public SpecialService SpecialService;

	[AutocompleteDropdown("Neighborhoods")]
	public string Neighbourhood;

	public int trafficIndex;

	public int totalSqm;

	public SteamAPI.DLC requiredDLC;

	public DeliveryJobStartLocation deliveryJobStartLocation;

	public bool unlocksHamptonsPrivateFence;

	[ShowIf("unlocksHamptonsPrivateFence")]
	public int privateFenceIndex;

	[ShowIf("IsHamptonsAIVilla")]
	public string hamptonsAIVillaOwner;

	public float customMarketPrice;

	public Address Address => new Address(StreetName, StreetNumber);

	public int GetCustomerCapacity => BuildingSizeHelper.GetData(BuildingSize).GetCustomerCapacity(BuildingType, BuildingVersion);

	public BuildingRegistration GetRegistration()
	{
		return BuildingHelper.GetBuildingRegistration(Address);
	}

	public float GetMarketPricePerSqm()
	{
		NeighborhoodData data = NeighborhoodHelper.GetData(Neighbourhood);
		BuildingTypeData data2 = BuildingTypeHelper.GetData(BuildingType);
		float baseBuildingPricePerSqm = data.baseBuildingPricePerSqm;
		baseBuildingPricePerSqm *= data2.buildingPriceMultiplier;
		baseBuildingPricePerSqm *= data.realEstateMultiplier;
		if (data2.HasTag(TagRef.Buildingtypetag.lowerrentforhightraffic))
		{
			float num = baseBuildingPricePerSqm * (float)Mathf.Min(trafficIndex - 30, 0) / 100f;
			return baseBuildingPricePerSqm - num;
		}
		return baseBuildingPricePerSqm * (1.3f - (float)(100 - trafficIndex) / 100f);
	}

	public float GetMarketValue()
	{
		if (customMarketPrice > 0f)
		{
			return customMarketPrice;
		}
		return (float)totalSqm * GetMarketPricePerSqm();
	}

	public int GetBuildingDailyMarketRent()
	{
		int squareMeters = BuildingSizeHelper.GetData(BuildingSize).squareMeters;
		float buildingPriceMultiplierForRent = BuildingTypeHelper.GetData(BuildingType).buildingPriceMultiplierForRent;
		return Mathf.CeilToInt((float)squareMeters * GetBuildingDailyMarketRentPerSqm() * buildingPriceMultiplierForRent);
	}

	public float GetBuildingDailyMarketRentPerSqm()
	{
		return GetMarketPricePerSqm() * NeighborhoodHelper.GetData(Neighbourhood).rentMultiplierPerSqmPrice;
	}

	public bool IsHamptonsHouse()
	{
		return BuildingSize == "ba:buildingsize_t";
	}

	public bool IsHamptonsAIVilla()
	{
		if (BuildingType == "ba:buildingtype_residential" && Neighbourhood == "ba:neighborhood_thehamptons")
		{
			return BuildingVersion == -1;
		}
		return false;
	}
}
