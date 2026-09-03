using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class InvestmentFundLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Entities.InvestmentFundName, BigAmbitions" };

	public override List<string> Keys => new List<string> { "InvestmentFund.name", "MessageData.investmentFund", "DataHolder.investmentFundName" };

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:investmentfund_euroenergyhigh" },
		{ 1, "ba:investmentfund_franklinus" },
		{ 2, "ba:investmentfund_alliancestechnologya" },
		{ 3, "ba:investmentfund_laceglobala" },
		{ 4, "ba:investmentfund_asiadynamicindustries" },
		{ 5, "ba:investmentfund_hgchinabonds" }
	};
}
