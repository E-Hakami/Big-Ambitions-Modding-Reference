using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Factories/HasProducedProductsInFactory")]
public class HasProducedProductsInFactory : QuestRequirement
{
	private static readonly HashSet<ItemInstance> PalletShelves = new HashSet<ItemInstance>();

	private static readonly HashSet<string> ItemsToCheckFor = new HashSet<string>();

	[SerializeField]
	private int amountProduced;

	public override bool CheckIfCompleted()
	{
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			PalletShelves.Clear();
			ItemsToCheckFor.Clear();
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value.ItemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
				{
					PalletShelves.Add(value);
				}
				if (value is FactoryWorkstationInstance factoryWorkstationInstance && factoryWorkstationInstance.SelectedRecipe != null)
				{
					ItemsToCheckFor.Add(factoryWorkstationInstance.SelectedRecipe.output.item);
				}
			}
			if (PalletShelves.Count == 0 || ItemsToCheckFor.Count == 0)
			{
				continue;
			}
			foreach (string item in ItemsToCheckFor)
			{
				foreach (ItemInstance palletShelf in PalletShelves)
				{
					num += palletShelf.GetAmountByItemName(item);
					if (num >= amountProduced)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
