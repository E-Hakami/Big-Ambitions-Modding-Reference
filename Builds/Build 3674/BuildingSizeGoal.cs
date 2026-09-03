using System.Linq;
using HGAttributes;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/BuildingSizeGoal")]
public class BuildingSizeGoal : IntBaseGoal
{
	[Tooltip("If BuildingType is Special it counts all buildings the player owns")]
	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	protected override int GetValue()
	{
		bool countAllBuildingTypes = buildingType == "ba:buildingtype_special";
		return (from x in SaveGameManager.Current.BuildingRegistrations
			where x.RentedByPlayer
			where countAllBuildingTypes || BuildingHelper.GetBuilding(x.Address).BuildingType == buildingType
			select BuildingHelper.GetBuildingSquareMeters(x.Address)).DefaultIfEmpty(0).Max();
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			size = amount.ToFormattedArea(),
			type = buildingType
		};
		return result;
	}

	protected override object FormatProgressValue(int value)
	{
		return value.ToFormattedArea();
	}
}
