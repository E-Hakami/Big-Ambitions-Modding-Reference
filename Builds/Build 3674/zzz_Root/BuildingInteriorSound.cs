using System;
using HGAttributes;
using UnityEngine;

[Serializable]
public class BuildingInteriorSound
{
	[AutocompleteDropdown("BuildingTypes")]
	public string[] buildingTypes;

	[AutocompleteDropdown("BusinessTypes")]
	public string[] businessTypes;

	public string[] buildingSizes;

	public AudioClip[] InteriorSounds;
}
