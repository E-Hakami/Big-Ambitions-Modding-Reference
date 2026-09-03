using System.Linq;
using BigAmbitions.Items;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class RotateCashRegisters : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		ItemHelper.Init();
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer))
		{
			foreach (ItemInstance item2 in from x in item.itemsInBuilding
				select gameInstance.WorldItemsHashSet.FirstOrDefault((ItemInstance y) => y.id == x) into x
				where x != null && x.itemName == "ba:itemname_cashregister"
				select x)
			{
				item2.rotation *= Quaternion.Euler(0f, 180f, 0f);
			}
		}
	}
}
