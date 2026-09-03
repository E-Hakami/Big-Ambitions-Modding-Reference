using System.Collections.Generic;
using BigAmbitions.Items;
using Extensions;
using HGAttributes;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/ItemsInStockGoal")]
public class ItemsInStockGoal : IntBaseGoal
{
	[Tooltip("If BuildingType is Special it uses all buildings the player owns")]
	[AutocompleteDropdown("BuildingTypes")]
	public string type;

	private readonly Dictionary<string, int> _itemsStocks = new Dictionary<string, int>();

	protected override int GetValue()
	{
		_itemsStocks.Clear();
		bool flag = type == "ba:buildingtype_special";
		int num = 0;
		for (int i = 0; i < SaveGameManager.Current.BuildingRegistrations.Count; i++)
		{
			BuildingRegistration buildingRegistration = SaveGameManager.Current.BuildingRegistrations[i];
			if (!buildingRegistration.RentedByPlayer || (!flag && buildingRegistration.GetBuildingType() != type))
			{
				continue;
			}
			foreach (ItemInstance value2 in buildingRegistration.itemInstances.Values)
			{
				if (value2 == null)
				{
					continue;
				}
				for (int j = 0; j < value2.cargoInstances.Count; j++)
				{
					CargoInstance cargoInstance = value2.cargoInstances[j];
					if (!string.IsNullOrEmpty(cargoInstance.itemName))
					{
						_itemsStocks.TryGetValue(cargoInstance.itemName, out var value);
						value += cargoInstance.amount;
						_itemsStocks[cargoInstance.itemName] = value;
						if (value > num)
						{
							num = value;
						}
						if (num >= amount)
						{
							return num;
						}
					}
				}
			}
		}
		return num;
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			amount = amount.ToFormattedNumber(),
			type = type
		};
		return result;
	}
}
