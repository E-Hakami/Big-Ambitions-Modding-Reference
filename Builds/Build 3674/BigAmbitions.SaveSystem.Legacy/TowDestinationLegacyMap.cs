using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class TowDestinationLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "AutoTowServiceSettings.optionSelected" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:towdestination_gasstation" },
		{ 1, "ba:towdestination_autorepairshop" }
	};
}
