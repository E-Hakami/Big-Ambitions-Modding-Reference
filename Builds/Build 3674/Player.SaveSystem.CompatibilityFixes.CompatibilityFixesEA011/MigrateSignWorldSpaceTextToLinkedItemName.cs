using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem.Legacy;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class MigrateSignWorldSpaceTextToLinkedItemName : ICompatibilityFix
{
	private static readonly ItemNameLegacyMap ItemNameLegacyMap = new ItemNameLegacyMap();

	private static readonly HashSet<string> Signs = new HashSet<string> { "ba:itemname_wallproductsign", "ba:itemname_hangingproductsignrectangular", "ba:itemname_hangingproductsignsquare", "ba:itemname_hangingsignhigh" };

	public void Apply(GameInstance gameInstance)
	{
		List<BuildingRegistration> list = gameInstance?.BuildingRegistrations;
		if (list == null || list.Count == 0)
		{
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			BuildingRegistration buildingRegistration = list[i];
			if (buildingRegistration?.itemInstances == null || buildingRegistration.itemInstances.Count == 0)
			{
				continue;
			}
			foreach (ItemInstance value2 in buildingRegistration.itemInstances.Values)
			{
				if (value2 != null && string.IsNullOrWhiteSpace(value2.linkedItemName) && !string.IsNullOrWhiteSpace(value2.worldSpaceTextValue) && Signs.Contains(value2.itemName) && int.TryParse(value2.worldSpaceTextValue, out var result) && ItemNameLegacyMap.TryMap(result, out var value))
				{
					value2.linkedItemName = value;
					value2.worldSpaceTextValue = string.Empty;
				}
			}
		}
	}
}
