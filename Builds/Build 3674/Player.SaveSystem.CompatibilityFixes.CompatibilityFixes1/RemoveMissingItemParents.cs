using System.Collections.Generic;
using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class RemoveMissingItemParents : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration == null)
			{
				continue;
			}
			Dictionary<string, ItemInstance> itemInstances = buildingRegistration.itemInstances;
			if (itemInstances == null)
			{
				continue;
			}
			foreach (ItemInstance value in itemInstances.Values)
			{
				if (value != null && !string.IsNullOrEmpty(value.parentId) && !itemInstances.ContainsKey(value.parentId))
				{
					value.parentId = null;
				}
			}
		}
	}
}
