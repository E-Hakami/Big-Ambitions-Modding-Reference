using System;
using BigAmbitions.SoundSystem;
using HGAttributes;

[Serializable]
public class BuildingEnterSound
{
	[AutocompleteDropdown("BuildingTypes")]
	public string[] buildingTypes;

	[AutocompleteDropdown("BusinessTypes")]
	public string[] businessTypes;

	public string[] buildingSizes;

	public SoundType type;
}
