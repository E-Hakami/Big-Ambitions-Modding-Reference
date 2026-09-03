using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using Helpers;
using Localizor;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class SellOldSofaAndDesktopWorkstationFurniture : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		SellItemOfType(gameInstance, "ba:itemname_hangingsignhigh", 3500f);
		SellItemOfType(gameInstance, "ba:itemname_desktopworkstation1", 1900f);
		SellItemOfType(gameInstance, "ba:itemname_desktopworkstation1", 4900f);
	}

	private void SellItemOfType(GameInstance savegame, string itemName, float costPerUnit)
	{
		List<ItemInstance> list = savegame.WorldItemsHashSet.Where((ItemInstance x) => x.itemName == itemName).ToList();
		int count = list.Count;
		if (count == 0)
		{
			return;
		}
		foreach (ItemInstance instance in list)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(instance.AddressCached);
			if (buildingRegistration != null && buildingRegistration.scheduleDays != null)
			{
				foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
				{
					scheduleDay.workShifts?.RemoveAll((WorkShift x) => x.itemInstanceId == instance.id);
				}
			}
			buildingRegistration?.RemoveItemInstanceFromBuilding(instance);
			foreach (TodoTask item in savegame.TodoTasks.ToList())
			{
				if (item.itemInstanceId == instance.id)
				{
					savegame.TodoTasks.Remove(item);
				}
			}
		}
		savegame.WorldItemsHashSet.RemoveWhere((ItemInstance x) => x.itemName == itemName);
		float num = costPerUnit * (float)count;
		SaveGameManager.Current.Money += num;
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"text",
			$"{count}x{itemName.GetLocalization()} was sold (caused by compatibility support)"
		} };
		TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
		SaveGameManager.Current.Transactions.Enqueue(new Transaction(info)
		{
			amount = num
		});
	}
}
