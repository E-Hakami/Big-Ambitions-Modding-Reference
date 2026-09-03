using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPlacedItem")]
public class HasPlacedItem : QuestRequirement
{
	[FormerlySerializedAs("businessType")]
	[Tooltip("Empty for searching on building types only")]
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	[AutocompleteDropdown("Items")]
	public string[] itemNames;

	public string[] itemTags;

	public override bool CheckIfCompleted()
	{
		if (businessTypeName != "ba:businesstype_empty")
		{
			return SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessTypeName == businessTypeName && x.RentedByPlayer).SelectMany((BuildingRegistration x) => x.itemInstances.Values.Select((ItemInstance i) => i.itemName)).Any((string x) => QuestRequirement.ItemMatches(x, itemNames, itemTags));
		}
		return SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.GetBuildingType() == buildingType && x.RentedByPlayer).SelectMany((BuildingRegistration x) => x.itemInstances.Values.Select((ItemInstance i) => i.itemName)).Any((string x) => QuestRequirement.ItemMatches(x, itemNames, itemTags));
	}
}
