using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class AddRequiredBathroomItemsAndChangingRoomsToDeliverySpots : ICompatibilityFix
{
	private readonly List<string> _itemsToAdd = new List<string>();

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			string buildingType = buildingRegistration.GetBuildingType();
			if (!(buildingType == "ba:buildingtype_office") && !(buildingType == "ba:buildingtype_retail"))
			{
				continue;
			}
			SetItemsToAdd(buildingRegistration);
			if (_itemsToAdd.Count == 0)
			{
				continue;
			}
			ItemInstance itemInstance = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot");
			if (itemInstance == null)
			{
				continue;
			}
			foreach (string item in _itemsToAdd)
			{
				itemInstance.AddToCargo(new CargoInstance(item, 1, 0f));
			}
		}
	}

	private void SetItemsToAdd(BuildingRegistration buildingRegistration)
	{
		_itemsToAdd.Clear();
		List<BusinessRequirement> businessRequirements = BusinessTypeHelper.GetData(buildingRegistration).businessRequirements;
		if (businessRequirements.Count == 0)
		{
			return;
		}
		foreach (BusinessRequirement item in businessRequirements)
		{
			string[] items = item.GetItems();
			if (items == null || items.Length == 0)
			{
				continue;
			}
			if (items.Contains("ba:itemname_changingroom"))
			{
				int num = buildingRegistration.itemInstances.Values.Count((ItemInstance x) => x.itemName == "ba:itemname_changingroom");
				int num2 = Mathf.CeilToInt((float)buildingRegistration.customerCapacity / 20f);
				if (num < num2)
				{
					for (int num3 = num; num3 < num2; num3++)
					{
						_itemsToAdd.Add("ba:itemname_changingroom");
					}
				}
				continue;
			}
			int num4 = 0;
			if (item is SpecificItemsInBuildingBySqm specificItemsInBuildingBySqm)
			{
				num4 = specificItemsInBuildingBySqm.GetRequiredItemCount(buildingRegistration);
			}
			else if (item is ItemsOfTypeInBuildingBySqm itemsOfTypeInBuildingBySqm)
			{
				num4 = itemsOfTypeInBuildingBySqm.GetRequiredItemCount(buildingRegistration);
			}
			for (int num5 = 0; num5 < num4; num5++)
			{
				_itemsToAdd.Add(items[0]);
			}
		}
	}
}
