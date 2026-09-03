using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Neighborhoods;
using BigAmbitions.Tags;
using Blueprints;
using HGAttributes;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/BuildingTypeData")]
public class BuildingTypeData : TaggedScriptableObject
{
	public string buildingType;

	public Sprite poiIcon;

	public bool hasCityMapFilter;

	[ShowIf("hasCityMapFilter")]
	public Color mapFilterColor;

	[AutocompleteDropdown("BusinessTypes")]
	public string[] availableBusinessTypes;

	[AutocompleteDropdown("BusinessTypes")]
	public string[] availableDevBusinessTypes;

	public float buildingPriceMultiplier;

	[Range(0.001f, 15f)]
	public float buildingPriceMultiplierForRent = 1f;

	[Range(10f, 200f)]
	public int daysToCalculateDeposit = 30;

	[AutocompleteDropdown("Skills")]
	public string[] requiredBuildingSkills;

	public int maxDaysToBusinessSwap = 15;

	public List<DataElement> blueprintsExtraData;

	public IdealAvailableBuildingsInNeighborhood[] idealAvailableBuildingsInNeighborhood;

	public float marketingReachMultiplier = 1f;

	public bool NeedsCleaning
	{
		get
		{
			if (requiredBuildingSkills != null)
			{
				return requiredBuildingSkills.Contains("ba:skill_cleaning");
			}
			return false;
		}
	}
}
