using System.Collections.Generic;
using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class RemoveOldMops : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		List<ItemInstance> list = new List<ItemInstance>();
		foreach (ItemInstance item in gameInstance.WorldItemsHashSet)
		{
			if (item.itemName == "ba:itemname_mop")
			{
				list.Add(item);
			}
		}
		foreach (ItemInstance item2 in list)
		{
			gameInstance.WorldItemsHashSet.Remove(item2);
		}
	}
}
