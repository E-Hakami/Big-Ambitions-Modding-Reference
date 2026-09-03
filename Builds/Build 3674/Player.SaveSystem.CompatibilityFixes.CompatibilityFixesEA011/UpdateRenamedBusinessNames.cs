using System.Collections.Generic;
using JimmysUnityUtilities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class UpdateRenamedBusinessNames : ICompatibilityFix
{
	private static readonly Dictionary<string, string> OldToNewBusinessNames = new Dictionary<string, string>
	{
		{ "Central Perk", "Metro Perk" },
		{ "Just Jeans", "Only Jeans" },
		{ "Pump!", "Pump Up" },
		{ "The Legal Eagle", "Eagle Eye Law" }
	};

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && !buildingRegistration.BusinessName.IsNullOrEmpty() && OldToNewBusinessNames.TryGetValue(buildingRegistration.BusinessName, out var value))
			{
				buildingRegistration.BusinessName = value;
			}
		}
	}
}
