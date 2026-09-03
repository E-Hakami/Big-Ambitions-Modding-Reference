using System.Linq;
using Entities;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Marketing/HasMarketingCampaigns")]
public class HasMarketingCampaigns : QuestRequirement
{
	public int minimumAmount;

	public bool anyType;

	[HideIf("anyType")]
	public MarketingTypeName marketingTypeName;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.BuildingRegistrations.Sum((BuildingRegistration x) => x.marketingCampaigns.Count((MarketingCampaign m) => m.enabled && (anyType || m.marketingTypeName == marketingTypeName))) >= minimumAmount;
	}
}
