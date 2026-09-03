using System.Collections.Generic;

namespace Entities;

public static class BoatTypes
{
	private static readonly List<BoatType> All = new List<BoatType>
	{
		new BoatType
		{
			type = BoatTypeName.Speedboat,
			price = 3200000
		},
		new BoatType
		{
			type = BoatTypeName.Yacht,
			price = 2500000
		},
		new BoatType
		{
			type = BoatTypeName.LuxuryYacht,
			price = 90000000,
			taxDeductible = true,
			isLuxuryYacht = true
		}
	};

	public static BoatType GetBoatType(this BoatTypeName typeName)
	{
		return All.Find((BoatType boat) => boat.type == typeName);
	}
}
