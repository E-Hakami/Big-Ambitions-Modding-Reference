using Entities;
using HGAttributes;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace UI.Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest Actions/CreateMarketEvent")]
public class CreateMarketEvent : Reward
{
	public string neighbourhood;

	public MarketEventType type;

	[AutocompleteDropdown("Items")]
	public string itemName;

	[Tooltip("Will start the next day, when demands are recalculated")]
	public bool startEventInstantly;

	public bool overrideDemandBoost;

	[ShowIf("overrideDemandBoost")]
	public int customDemandBoost;

	public override void OnComplete()
	{
		if (type == MarketEventType.Hype && string.IsNullOrEmpty(itemName))
		{
			ProductMarketHelper.GenerateHypeEventOnRandomWholesalerItems(null, overrideDemandBoost ? customDemandBoost : (-1));
			return;
		}
		ProductMarketHelper.CreateItemRelatedMarketEvent(type, itemName, neighbourhood, null, startEventInstantly ? SaveGameManager.Current.Day : (-1), overrideDemandBoost ? new int?(customDemandBoost) : ((int?)null));
		ProductMarketHelper.UpdateMarketDemands();
	}
}
