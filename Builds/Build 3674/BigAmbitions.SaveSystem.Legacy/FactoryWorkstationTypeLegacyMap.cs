using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class FactoryWorkstationTypeLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "BigAmbitions.Factories.Workstations.FactoryWorkstationType, BigAmbitions.Factories" };

	public override List<string> Keys => new List<string> { "FactoryWorkstationInstance.workstationType", "workstationType" };

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 1, "ba:factoryworkstationtype_foodworkstation" },
		{ 2, "ba:factoryworkstationtype_bottledgoodsworkstation" },
		{ 3, "ba:factoryworkstationtype_gardenworkstation" },
		{ 4, "ba:factoryworkstationtype_consumergoodsworkstation" },
		{ 5, "ba:factoryworkstationtype_clothingworkstation" },
		{ 6, "ba:factoryworkstationtype_jewelryworkstation" },
		{ 7, "ba:factoryworkstationtype_electronicsworkstation" }
	};
}
