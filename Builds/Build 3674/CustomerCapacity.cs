using System;
using HGAttributes;
using NaughtyAttributes;

[Serializable]
public class CustomerCapacity
{
	public int amount;

	[AutocompleteDropdown("BuildingTypes")]
	public string buildingType;

	[Label("Building size version (0: any)")]
	[AllowNesting]
	public int buildingVersion;
}
