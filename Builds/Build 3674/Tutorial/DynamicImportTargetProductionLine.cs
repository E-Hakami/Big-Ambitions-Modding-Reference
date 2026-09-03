using System.Collections.Generic;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Items;
using Buildings;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/DynamicImportTargetProductionLine")]
public class DynamicImportTargetProductionLine : QuestEntryTarget
{
	public override Address GetAddress()
	{
		GameInstance current = SaveGameManager.Current;
		HashSet<Address> hashSet = new HashSet<Address>();
		Address result = null;
		int num = 0;
		foreach (ImportPartnership importPartnership in current.importPartnerships)
		{
			hashSet.Add(importPartnership.importAddress);
		}
		foreach (BuildingRegistration buildingRegistration in current.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (!(value is FactoryWorkstationInstance factoryWorkstationInstance) || factoryWorkstationInstance.SelectedRecipe == null)
				{
					continue;
				}
				List<RecipeItem> ingredients = factoryWorkstationInstance.SelectedRecipe.ingredients;
				foreach (BuildingRegistration buildingRegistration2 in current.BuildingRegistrations)
				{
					if (buildingRegistration2.businessTypeName != "ba:businesstype_importexport" || hashSet.Contains(buildingRegistration2.Address))
					{
						continue;
					}
					ImportExportSettings importExportSettings = buildingRegistration2.BuildingCached.SpecialService.settings as ImportExportSettings;
					if (importExportSettings == null)
					{
						continue;
					}
					IReadOnlyList<string> itemsAvailable = importExportSettings.GetItemsAvailable();
					int num2 = 0;
					foreach (RecipeItem item in ingredients)
					{
						foreach (string item2 in itemsAvailable)
						{
							if (!(item2 != item.item))
							{
								num2++;
								break;
							}
						}
					}
					if (num2 > num)
					{
						num = num2;
						result = buildingRegistration2.Address;
					}
				}
			}
		}
		return result;
	}
}
