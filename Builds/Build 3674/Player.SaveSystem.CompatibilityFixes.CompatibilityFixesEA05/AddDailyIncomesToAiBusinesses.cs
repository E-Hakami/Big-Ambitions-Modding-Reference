using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class AddDailyIncomesToAiBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.dailyIncomes == null || buildingRegistration.dailyIncomes.Count <= 0)
			{
				if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
				{
					float element = buildingRegistration.GetEstimatedWeeklyIncome() / 7f;
					buildingRegistration.dailyIncomes = Enumerable.Repeat(element, 20).ToList();
				}
				else
				{
					buildingRegistration.dailyIncomes = new List<float>();
				}
			}
		}
	}
}
