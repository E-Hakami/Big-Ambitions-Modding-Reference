using System;
using System.Collections.Generic;
using HGAttributes;

[Serializable]
public class BuildingVersion
{
	public int number;

	public bool specialBuildingOnly;

	[AutocompleteDropdown("BuildingTypes")]
	public List<string> supportedBuildingTypes;
}
