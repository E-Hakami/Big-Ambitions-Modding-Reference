using System.Collections.Generic;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Items;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Factories/HasImportPartnershipForProductionLine")]
public class HasImportPartnershipForProductionLine : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		List<RecipeItem> list = new List<RecipeItem>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value is FactoryWorkstationInstance factoryWorkstationInstance && !(factoryWorkstationInstance.SelectedRecipe == null))
				{
					list.AddRange(factoryWorkstationInstance.SelectedRecipe.ingredients);
				}
			}
		}
		foreach (ImportPartnership importPartnership in SaveGameManager.Current.importPartnerships)
		{
			foreach (ImportProduct product in importPartnership.products)
			{
				if (product.amount <= 0)
				{
					continue;
				}
				foreach (RecipeItem item in list)
				{
					if (item.item == product.itemName)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
