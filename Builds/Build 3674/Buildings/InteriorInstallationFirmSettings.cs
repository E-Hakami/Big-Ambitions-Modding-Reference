using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/InteriorInstallationFirmSettings")]
public class InteriorInstallationFirmSettings : SpecialServiceSettings
{
	[AutocompleteDropdown("BuildingTypes")]
	public List<string> buildingTypesThatCanInstall;
}
