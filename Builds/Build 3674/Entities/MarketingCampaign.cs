using System;

namespace Entities;

[Serializable]
public class MarketingCampaign
{
	public Address agencyAddress;

	public MarketingTypeName marketingTypeName;

	public bool enabled;
}
