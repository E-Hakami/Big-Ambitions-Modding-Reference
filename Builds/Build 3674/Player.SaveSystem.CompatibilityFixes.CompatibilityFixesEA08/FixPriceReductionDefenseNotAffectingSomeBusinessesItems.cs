using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using BusinessLayoutSets;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixPriceReductionDefenseNotAffectingSomeBusinessesItems : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		InstanceBehavior<GameManager>.Instance?.StartCoroutine(UpdateCachedAvailableProductsAndRetailPricesForRivalBusinesses(gameInstance));
	}

	private IEnumerator UpdateCachedAvailableProductsAndRetailPricesForRivalBusinesses(GameInstance gameInstance)
	{
		yield return new WaitUntil(() => !BusinessLayoutSetHelper.loadingLayouts);
		IReadOnlyCollection<SpecialRival> specialRivals = RivalsHelper.GetSpecialRivals();
		foreach (BuildingRegistration item2 in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && !x.AvailableForRent && specialRivals.Any((SpecialRival y) => y.rivalData.id == x.businessOwnerRivalId) && x.businessTypeName != "ba:businesstype_empty"))
		{
			item2.cachedAvailableProducts = item2.GetListOfItemsForSale().ToList();
		}
		foreach (SpecialRivalState specialRivalState in gameInstance.specialRivalStates)
		{
			List<BuildingRegistration> list = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessOwnerRivalId == specialRivalState.rivalId).ToList();
			foreach (BuildingRegistration item3 in list)
			{
				CompetitionHelper.RecalculateRetailPrices(item3);
			}
			if (specialRivalState.defenseStates == null)
			{
				continue;
			}
			foreach (DefenseState defenseState in specialRivalState.defenseStates)
			{
				if (defenseState.defensiveMechanic != DefensiveMechanic.PriceReduction)
				{
					continue;
				}
				List<RetailPrice> list2 = list.Select((BuildingRegistration business) => business.retailPrices.Where((RetailPrice x) => defenseState.affectedItems.Contains(x.itemName)).ToList()).SelectMany((List<RetailPrice> retailPrices) => retailPrices).ToList();
				float item = RivalDefenseHelper.GetPriceReductionValues(defenseState.aggression).Item1;
				foreach (RetailPrice item4 in list2)
				{
					item4.price = Mathf.Max(item4.price * item, CompetitionHelper.GetMinimumRivalPrice(item4.itemName));
				}
			}
		}
	}
}
