using System.Collections.Generic;
using Buildings;
using HGAttributes;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/FurnitureStoreSettings")]
public class FurnitureStoreSettings : SpecialServiceSettings
{
	[AutocompleteDropdown("BuildingTypes")]
	public List<string> allowedDeliveryBuildingTypes;
}
