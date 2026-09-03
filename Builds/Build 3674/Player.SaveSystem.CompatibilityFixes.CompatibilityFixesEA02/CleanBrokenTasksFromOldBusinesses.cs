using System.Linq;
using BigAmbitions.Items;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class CleanBrokenTasksFromOldBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.TodoTasks.RemoveAll(delegate(TodoTask x)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(x.address);
			return buildingRegistration != null && !buildingRegistration.RentedByPlayer && BuildingHelper.GetBuilding(x.address).SpecialService == null;
		});
		gameInstance.TodoTasks.RemoveAll((TodoTask x) => !string.IsNullOrEmpty(x.itemInstanceId) && gameInstance.WorldItemsHashSet.FirstOrDefault((ItemInstance y) => y.id == x.itemInstanceId) == null);
	}
}
