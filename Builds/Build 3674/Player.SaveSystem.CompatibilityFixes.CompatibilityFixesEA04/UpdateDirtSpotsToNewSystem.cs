using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateDirtSpotsToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration registration in gameInstance.BuildingRegistrations)
		{
			registration.dirtSpots = (registration.RentedByPlayer ? BuildingCleanlinessHelper.GetDirtSpotsForBuilding(registration.BuildingCached) : new List<DirtSpot>());
			foreach (string itemInstanceId in registration.itemsInBuilding)
			{
				ItemInstance itemInstance = gameInstance.WorldItemsHashSet.FirstOrDefault((ItemInstance x) => x.id == itemInstanceId);
				if (itemInstance == null)
				{
					continue;
				}
				itemInstance.dirtSpotsThatAffects = (from x in registration.dirtSpots
					where itemInstance.dirtAffectedCells != null && itemInstance.dirtAffectedCells.Any((CellPosition y) => x.x == y.x && x.z == y.z)
					select registration.dirtSpots.IndexOf(x)).ToList();
			}
		}
	}
}
