using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Enums;
using HGAttributes;

namespace BigAmbitions.Rivals;

[Serializable]
public class DefenseState
{
	public Timestamp timestamp;

	public DefensiveMechanic defensiveMechanic;

	public Priority aggression;

	[AutocompleteDropdown("Items")]
	public List<string> affectedItems;

	public List<string> affectedEmployeeIds;
}
