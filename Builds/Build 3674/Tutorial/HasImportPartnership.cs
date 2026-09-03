using System.Collections.Generic;
using System.Linq;
using Entities;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/ImportExport/HasImportPartnership")]
public class HasImportPartnership : QuestRequirement
{
	public bool mustBeActive;

	[HideIf("mustBeActive")]
	[Tooltip("Amount of partnerships needed")]
	public int minimumAmount;

	[ShowIf("mustBeActive")]
	public RequiredProduct[] products;

	public override bool CheckIfCompleted()
	{
		if (!mustBeActive)
		{
			return SaveGameManager.Current.importPartnerships.Count >= minimumAmount;
		}
		IEnumerable<ImportPartnership> activeImportPartnerships = SaveGameManager.Current.importPartnerships.Where((ImportPartnership x) => x.isActive);
		if (products.Length == 0)
		{
			return activeImportPartnerships.Any();
		}
		return products.All((RequiredProduct requiredProduct) => activeImportPartnerships.Any((ImportPartnership x) => x.products.Any((ImportProduct product) => product.itemName == requiredProduct.itemName && product.amount >= requiredProduct.minimumAmount)));
	}
}
