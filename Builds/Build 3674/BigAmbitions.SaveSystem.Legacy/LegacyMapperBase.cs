using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public abstract class LegacyMapperBase : ILegacyMapper
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string>();

	protected abstract Dictionary<int, string> Map { get; }

	public abstract List<string> Keys { get; }

	public virtual HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	public bool TryMap(int legacy, out string value)
	{
		return Map.TryGetValue(legacy, out value);
	}
}
