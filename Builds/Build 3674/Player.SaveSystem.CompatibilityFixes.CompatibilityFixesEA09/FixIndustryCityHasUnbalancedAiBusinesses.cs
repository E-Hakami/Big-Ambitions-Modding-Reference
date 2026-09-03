using System.Collections.Generic;
using System.Linq;
using AI.Citizens;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class FixIndustryCityHasUnbalancedAiBusinesses : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_industrycity";

	private const int MinBuildNumber = 3204;

	private const int NumberOfSameBusinessTypeToTriggerReshuffle = 10;

	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.buildNumberAtLastSave >= 3204)
		{
			CitizenHelper.Init();
			if (HasUnbalancedBusinesses(gameInstance))
			{
				CompatibilityHelper.ReshuffleNeighborhood("ba:neighborhood_industrycity");
			}
		}
	}

	private static bool HasUnbalancedBusinesses(GameInstance gameInstance)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.Neighborhood != "ba:neighborhood_industrycity" || buildingRegistration.RentedByPlayer || buildingRegistration.AvailableForRent || string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
			{
				continue;
			}
			string buildingType = buildingRegistration.GetBuildingType();
			if (!(buildingType == "ba:buildingtype_special") && !(buildingType == "ba:buildingtype_residential"))
			{
				string businessTypeName = buildingRegistration.businessTypeName;
				if (dictionary.TryGetValue(businessTypeName, out var value))
				{
					dictionary[businessTypeName] = value + 1;
				}
				else
				{
					dictionary[businessTypeName] = 1;
				}
			}
		}
		return dictionary.Any((KeyValuePair<string, int> x) => x.Value > 10);
	}
}
