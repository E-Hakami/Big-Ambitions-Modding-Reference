using System;
using Entities;
using HGAttributes;

namespace Buildings;

[Serializable]
public class NpcSpawnSettings
{
	[AutocompleteDropdown("Items")]
	public string spawnItem;

	public BuildingStationaryAiData buildingStationaryAiData;

	public int spawnChance;
}
