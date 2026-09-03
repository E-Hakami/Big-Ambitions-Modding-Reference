using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RemovePositionZeroItemsInLayouts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		float num = 0f;
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			for (int num2 = buildingRegistration.itemInstances.Count - 1; num2 >= 0; num2--)
			{
				var (key, itemInstance2) = buildingRegistration.itemInstances.ElementAt(num2);
				if (!(itemInstance2.position != Vector3.zero))
				{
					buildingRegistration.itemInstances.Remove(key);
					num += ItemHelper.GetDefaultMarketPrice(itemInstance2.itemName);
					foreach (AttachableChild stackedItem in itemInstance2.stackedItems)
					{
						num += ItemHelper.GetDefaultMarketPrice(stackedItem.childItemName);
						buildingRegistration.itemInstances.Remove(stackedItem.childId);
					}
				}
			}
		}
		SaveGameManager.Current.Money += num;
		Dictionary<string, string> data = new Dictionary<string, string> { { "text", "Invalid items were sold (caused by compatibility support)" } };
		TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
		SaveGameManager.Current.Transactions.Enqueue(new Transaction(info)
		{
			amount = num
		});
	}
}
