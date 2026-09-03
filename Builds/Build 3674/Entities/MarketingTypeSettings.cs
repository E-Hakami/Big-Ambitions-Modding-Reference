using System.Collections.Generic;
using System.Linq;

namespace Entities;

public class MarketingTypeSettings
{
	public MarketingTypeName marketingTypeName;

	public float pricePerDay;

	public int sqmReach;

	private static readonly List<MarketingTypeSettings> All = new List<MarketingTypeSettings>
	{
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.SmallInternet,
			pricePerDay = 100f,
			sqmReach = 20
		},
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.MediumInternet,
			pricePerDay = 250f,
			sqmReach = 40
		},
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.LargeInternet,
			pricePerDay = 500f,
			sqmReach = 60
		},
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.SmallBillboard,
			pricePerDay = 500f,
			sqmReach = 100
		},
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.MediumBillboard,
			pricePerDay = 2500f,
			sqmReach = 250
		},
		new MarketingTypeSettings
		{
			marketingTypeName = MarketingTypeName.LargeBillboard,
			pricePerDay = 6000f,
			sqmReach = 600
		}
	};

	public static MarketingTypeSettings Get(MarketingTypeName typeName)
	{
		return All.FirstOrDefault((MarketingTypeSettings x) => x.marketingTypeName == typeName);
	}
}
