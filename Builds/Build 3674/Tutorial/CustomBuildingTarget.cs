using System.Collections.Generic;
using BigAmbitions.Tags;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/CustomBuildingTarget")]
public class CustomBuildingTarget : QuestEntryTarget
{
	[SerializeField]
	private bool rentedByPlayer;

	[SerializeField]
	[AutocompleteDropdown("BuildingTypes")]
	private string buildingType;

	[SerializeField]
	[AutocompleteDropdown("BusinessTypes")]
	private string businessTypeName;

	[SerializeField]
	private bool generatesRevenue;

	[SerializeField]
	private int buildingRegistrationIndex;

	public override Address GetAddress()
	{
		return GetBuildingRegistration()?.Address;
	}

	public BuildingRegistration GetBuildingRegistration()
	{
		bool flag = string.IsNullOrWhiteSpace(businessTypeName) || businessTypeName == "ba:businesstype_empty";
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer == rentedByPlayer && !(buildingRegistration.GetBuildingType() != buildingType) && (flag || !(buildingRegistration.businessTypeName != businessTypeName)))
			{
				BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
				if (!(data == null) && data.HasTag(TagRef.Businesstag.generatesrevenue) == generatesRevenue)
				{
					list.Add(buildingRegistration);
				}
			}
		}
		list.Sort((BuildingRegistration a, BuildingRegistration b) => a.creationDay.CompareTo(b.creationDay));
		if (buildingRegistrationIndex < list.Count)
		{
			return list[buildingRegistrationIndex];
		}
		return null;
	}
}
