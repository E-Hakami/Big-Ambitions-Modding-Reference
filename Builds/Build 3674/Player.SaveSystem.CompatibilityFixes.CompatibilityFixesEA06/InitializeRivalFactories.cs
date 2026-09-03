using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using Extensions;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class InitializeRivalFactories : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		IList<BuildingRegistration> list = gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => (bool)x.BuildingCached && x.GetBuildingType() == "ba:buildingtype_warehouse" && !x.RentedByPlayer).ToList().Shuffle();
		List<string> specialRivalIds = (from x in gameInstance.specialRivalStates
			where !x.isDefeated
			select x.rivalId).ToList();
		List<BuildingRegistration> futureFactories = list.Where((BuildingRegistration x) => x.AvailableForRent).Take(specialRivalIds.Count).ToList();
		if (futureFactories.Count < specialRivalIds.Count)
		{
			List<BuildingRegistration> list2 = list.Where((BuildingRegistration x) => specialRivalIds.Contains(x.businessOwnerRivalId) && !futureFactories.Contains(x)).ToList();
			foreach (string rivalId in specialRivalIds.ToList())
			{
				BuildingRegistration buildingRegistration = list2.FirstOrDefault((BuildingRegistration x) => x.businessOwnerRivalId == rivalId);
				if (buildingRegistration != null)
				{
					futureFactories.Add(buildingRegistration);
					list2.Remove(buildingRegistration);
					specialRivalIds.Remove(rivalId);
				}
				if (futureFactories.Count >= specialRivalIds.Count)
				{
					break;
				}
			}
			if (futureFactories.Count < specialRivalIds.Count)
			{
				futureFactories.AddRange(list.Except(futureFactories).Take(specialRivalIds.Count - futureFactories.Count));
			}
		}
		ConvertToFactories(futureFactories, specialRivalIds);
	}

	private static void ConvertToFactories(IList<BuildingRegistration> futureFactories, IList<string> specialRivalIds)
	{
		int i;
		for (i = 0; i < specialRivalIds.Count; i++)
		{
			BuildingRegistration buildingRegistration = futureFactories.FirstOrDefault((BuildingRegistration x) => x.buildingOwnerRivalId == specialRivalIds[i]);
			if (buildingRegistration != null)
			{
				buildingRegistration.businessTypeName = "ba:businesstype_factory";
				buildingRegistration.businessOwnerRivalId = specialRivalIds[i];
				buildingRegistration.AvailableForRent = false;
				futureFactories.Remove(buildingRegistration);
				specialRivalIds.RemoveAt(i);
				i--;
			}
		}
		for (int num = 0; num < futureFactories.Count; num++)
		{
			BuildingRegistration buildingRegistration2 = futureFactories[num];
			buildingRegistration2.businessTypeName = "ba:businesstype_factory";
			buildingRegistration2.businessOwnerRivalId = specialRivalIds[num];
			buildingRegistration2.AvailableForRent = false;
			buildingRegistration2.GenerateAiBusinessEmployees();
		}
	}
}
