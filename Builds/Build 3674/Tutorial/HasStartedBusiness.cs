using System.Collections.Generic;
using Buildings;
using Entities;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasStartedBusiness")]
public class HasStartedBusiness : QuestRequirement
{
	[SerializeField]
	[AutocompleteDropdown("BusinessTypes")]
	private string[] businessTypeNames;

	[SerializeField]
	[AutocompleteDropdown("BuildingTypes")]
	private string[] buildingTypes;

	[SerializeField]
	private int minimumPreviousBusinesses;

	[SerializeField]
	private int maximumSize;

	[SerializeField]
	private int trafficIndex;

	[SerializeField]
	private int minimumDemandForPrimaryProduct;

	[SerializeField]
	private bool skipChecksIfPlayerIsAhead;

	public override bool CheckIfCompleted()
	{
		List<BuildingRegistration> playerBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterExcludingResidentialAndNewRented, PlayerBuildingSortByCreationDay);
		if (playerBuildingRegistrations.Count <= minimumPreviousBusinesses)
		{
			return false;
		}
		for (int i = minimumPreviousBusinesses; i < playerBuildingRegistrations.Count; i++)
		{
			BuildingRegistration buildingRegistration = playerBuildingRegistrations[i];
			if ((HasValues(businessTypeNames) && !HasValue(businessTypeNames, buildingRegistration.businessTypeName)) || (HasValues(buildingTypes) && !HasValue(buildingTypes, buildingRegistration.GetBuildingType())) || string.IsNullOrWhiteSpace(buildingRegistration.BusinessName))
			{
				continue;
			}
			if (skipChecksIfPlayerIsAhead && !buildingRegistration.temporarilyClosed)
			{
				return true;
			}
			if ((maximumSize <= 0 || BuildingSizeHelper.GetData(buildingRegistration.BuildingCached.BuildingSize).squareMeters <= maximumSize) && buildingRegistration.BuildingCached.trafficIndex >= trafficIndex)
			{
				if (minimumDemandForPrimaryProduct <= 0)
				{
					return true;
				}
				if (HasPrimaryProductDemand(buildingRegistration))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PlayerBuildingFilterExcludingResidentialAndNewRented(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.GetBuildingType() != "ba:buildingtype_residential")
		{
			return buildingRegistration.creationDay != -1;
		}
		return false;
	}

	private int PlayerBuildingSortByCreationDay(BuildingRegistration x, BuildingRegistration y)
	{
		int num = ((x.creationDay == -1) ? TimeHelper.CurrentDay : x.creationDay);
		int value = ((y.creationDay == -1) ? TimeHelper.CurrentDay : y.creationDay);
		return num.CompareTo(value);
	}

	private bool HasPrimaryProductDemand(BuildingRegistration business)
	{
		foreach (string primaryProduct in BusinessTypeHelper.GetData(business).GetPrimaryProducts())
		{
			foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
			{
				if (productMarketEntry.itemName != primaryProduct)
				{
					continue;
				}
				for (int i = 0; i < productMarketEntry.demandValues.Count; i++)
				{
					if (productMarketEntry.demandValues[i].demand > minimumDemandForPrimaryProduct)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static bool HasValue(string[] values, string value)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] == value)
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasValues(string[] values)
	{
		if (values != null)
		{
			return values.Length > 0;
		}
		return false;
	}
}
