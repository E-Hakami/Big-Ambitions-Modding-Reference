using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class BusinessProductSourceLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "BusinessType.productSources" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 1, "ba:businessproductsource_wholesaler" },
		{ 2, "ba:businessproductsource_importer" },
		{ 3, "ba:businessproductsource_factory" }
	};
}
