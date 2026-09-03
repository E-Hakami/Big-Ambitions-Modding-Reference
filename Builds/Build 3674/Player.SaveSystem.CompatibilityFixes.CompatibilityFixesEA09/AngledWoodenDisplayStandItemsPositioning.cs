using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class AngledWoodenDisplayStandItemsPositioning : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		EmployeeHelper.EnsureInit(gameInstance);
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			foreach (ItemInstance value2 in buildingRegistration.itemInstances.Values)
			{
				if (value2.itemName != "ba:itemname_angledwoodendisplaystand" || value2.stackedItems == null)
				{
					continue;
				}
				float y = ((Quaternion)value2.rotation).eulerAngles.y;
				Vector3 vector = Quaternion.Euler(0f, y, 0f) * Vector3.back * 0.2f;
				foreach (AttachableChild stackedItem in value2.stackedItems)
				{
					if (buildingRegistration.itemInstances.TryGetValue(stackedItem.childId, out var value) && value != null)
					{
						value.position.y = 0.85925f;
						ItemInstance itemInstance = value;
						itemInstance.position += vector;
						float y2 = ((Quaternion)value.rotation).eulerAngles.y;
						value.rotation = Quaternion.Euler(0f, y2, 0f);
					}
				}
			}
		}
	}
}
