using BigAmbitions.Factories;
using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateImportContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		int nextDeliveryDay = DeliveryHelper.GetNextDeliveryDay();
		foreach (ImportPartnership importPartnership in gameInstance.importPartnerships)
		{
			importPartnership.nextDeliveryDay = nextDeliveryDay;
			importPartnership.isTarget = importPartnership.daysUntilRepeat != 0;
			importPartnership.isRepeatingOrder = importPartnership.daysUntilRepeat != 0;
			foreach (ImportProduct product in importPartnership.products)
			{
				int max = int.MaxValue;
				if (!importPartnership.isTarget && !FactoriesHelper.IsFactoryIngredient(product.itemName))
				{
					max = product.ItemCached?.maxOrderAmountPerImporter ?? int.MaxValue;
				}
				product.amount = Mathf.Clamp(product.amount, 0, max);
			}
		}
	}
}
