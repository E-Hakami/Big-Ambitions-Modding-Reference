using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasStock")]
public class HasStock : QuestRequirement
{
	[FormerlySerializedAs("businessType")]
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	[AutocompleteDropdown("Items")]
	public string[] shelfTypes;

	[AutocompleteDropdown("Items")]
	public string itemName;

	[Tooltip("If in boxes, use number of boxes (200 cheap gifts => 1 box)")]
	public int minimumAmount;

	public override bool CheckIfCompleted()
	{
		List<BuildingRegistration> source = ((!(businessTypeName != "ba:businesstype_empty")) ? SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.GetBuildingType() == buildingType && x.RentedByPlayer).ToList() : SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessTypeName == businessTypeName && x.RentedByPlayer).ToList());
		int num = (from x in (from x in source.SelectMany((BuildingRegistration x) => x.itemInstances.Values)
				where shelfTypes.Contains(x.itemName)
				select x).SelectMany((ItemInstance x) => x.cargoInstances)
			where x.itemName == itemName
			select x).ToList().Sum((CargoInstance x) => x.amount);
		if (minimumAmount == 0)
		{
			return num > 0;
		}
		return num >= minimumAmount;
	}
}
