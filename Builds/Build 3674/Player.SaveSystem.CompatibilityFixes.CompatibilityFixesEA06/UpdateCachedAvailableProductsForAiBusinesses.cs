using System.Collections;
using System.Linq;
using BusinessLayoutSets;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class UpdateCachedAvailableProductsForAiBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		InstanceBehavior<GameManager>.Instance?.StartCoroutine(UpdateCachedAvailableProducts(gameInstance));
	}

	private IEnumerator UpdateCachedAvailableProducts(GameInstance gameInstance)
	{
		yield return new WaitUntil(() => !BusinessLayoutSetHelper.loadingLayouts);
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && (bool)x.BuildingCached && !x.AvailableForRent && x.businessTypeName != "ba:businesstype_empty"))
		{
			item.cachedAvailableProducts = item.GetListOfItemsForSale().ToList();
		}
		new UpdateMarketDemands().Apply(gameInstance);
	}
}
