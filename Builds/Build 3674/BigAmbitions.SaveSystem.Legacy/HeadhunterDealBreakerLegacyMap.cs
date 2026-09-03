using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class HeadhunterDealBreakerLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Entities.HeadhuntersDealBreakerType, BigAmbitions" };

	public override List<string> Keys => new List<string> { "HeadhuntersDealBreakers.toggledDealBreakersTypes", "HeadhunterPlan.dealBreakerTypes", "Headhunter.dealBreakerTypes" };

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:headhuntersdealbreaker_fulltime" },
		{ 1, "ba:headhuntersdealbreaker_parttime" },
		{ 2, "ba:headhuntersdealbreaker_noweekends" },
		{ 3, "ba:headhuntersdealbreaker_fivedaysaweek" },
		{ 4, "ba:headhuntersdealbreaker_fourdaysaweek" },
		{ 5, "ba:headhuntersdealbreaker_nomorningshifts" },
		{ 6, "ba:headhuntersdealbreaker_noafternoonshifts" },
		{ 7, "ba:headhuntersdealbreaker_noeveningshifts" },
		{ 8, "ba:headhuntersdealbreaker_nonightshifts" },
		{ 9, "ba:headhuntersdealbreaker_nocleaningshifts" },
		{ 10, "ba:headhuntersdealbreaker_environmentdemand" },
		{ 11, "ba:headhuntersdealbreaker_equipmentdemand" },
		{ 12, "ba:headhuntersdealbreaker_benefitdemand" }
	};
}
