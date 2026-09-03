using Buildings;
using HGAttributes;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasBuildingRented")]
public class HasRentedBuildings : QuestRequirement
{
	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	[FormerlySerializedAs("businessType")]
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName = "ba:businesstype_empty";

	public bool specificAddress;

	[HideIf("specificAddress")]
	public int amount = 1;

	[ShowIf("specificAddress")]
	public QuestEntryTarget address;

	public int minimumSize;

	public int maximumSize;

	public string neighbourhood;

	public int trafficIndex;

	public override bool CheckIfCompleted()
	{
		if (specificAddress)
		{
			foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
			{
				if (buildingRegistration.Address == address.GetAddress() && buildingRegistration.RentedByPlayer)
				{
					return true;
				}
			}
			return false;
		}
		int num = 0;
		foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
		{
			if ((string.IsNullOrEmpty(neighbourhood) || !(neighbourhood != "ba:neighborhood_global") || !(buildingRegistration2.Neighborhood != neighbourhood)) && !(buildingRegistration2.GetBuildingType() != buildingType) && buildingRegistration2.RentedByPlayer && !(buildingRegistration2.businessTypeName != businessTypeName) && buildingRegistration2.BuildingCached.trafficIndex >= trafficIndex)
			{
				int squareMeters = BuildingSizeHelper.GetData(buildingRegistration2.BuildingCached.BuildingSize).squareMeters;
				if ((minimumSize == 0 || squareMeters >= minimumSize) && (maximumSize == 0 || squareMeters <= maximumSize) && ++num >= amount)
				{
					return true;
				}
			}
		}
		return false;
	}
}
