using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class CustomerDemandLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Entities.CustomerDemandType, BigAmbitions" };

	public override List<string> Keys => new List<string> { "BuildingRegistration.cachedFulfilledCustomerDemands", "CustomerDemandSet.type", "Order.customerDemandTypes" };

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:customerdemand_music" },
		{ 1, "ba:customerdemand_seating" },
		{ 2, "ba:customerdemand_employeeuniforms" },
		{ 3, "ba:customerdemand_interiordesign" },
		{ 4, "ba:customerdemand_workoutvariety" },
		{ 5, "ba:customerdemand_toilet" },
		{ 6, "ba:customerdemand_toiletprivacy" },
		{ 7, "ba:customerdemand_sink" }
	};
}
