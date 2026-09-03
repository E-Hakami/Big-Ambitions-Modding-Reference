using System.Collections.Generic;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasRentedBuildingWithTrafficIndex")]
public class HasRentedBuildingWithTrafficIndex : QuestRequirement
{
	[SerializeField]
	[AutocompleteDropdown("BuildingTypes")]
	private string buildingType;

	[SerializeField]
	private int trafficIndex;

	[SerializeField]
	private int minimumPreviousBusinesses;

	public override bool CheckIfCompleted()
	{
		List<BuildingRegistration> playerBuildingRegistrations = BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilter, PlayerBuildingSortByCreationDay);
		if (playerBuildingRegistrations.Count <= minimumPreviousBusinesses)
		{
			return false;
		}
		for (int i = minimumPreviousBusinesses; i < playerBuildingRegistrations.Count; i++)
		{
			if (playerBuildingRegistrations[i].BuildingCached.trafficIndex >= trafficIndex)
			{
				return true;
			}
		}
		return false;
	}

	private bool PlayerBuildingFilter(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.RentedByPlayer)
		{
			return buildingRegistration.GetBuildingType() == buildingType;
		}
		return false;
	}

	private int PlayerBuildingSortByCreationDay(BuildingRegistration x, BuildingRegistration y)
	{
		int num = ((x.creationDay == -1) ? TimeHelper.CurrentDay : x.creationDay);
		int value = ((y.creationDay == -1) ? TimeHelper.CurrentDay : y.creationDay);
		return num.CompareTo(value);
	}
}
