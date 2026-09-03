using System.Collections.Generic;
using System.Threading.Tasks;
using BlueprintsUI;

namespace Blueprints;

public abstract class BlueprintController
{
	internal readonly Dictionary<int, List<Blueprint>> blueprintsCache = new Dictionary<int, List<Blueprint>>();

	public readonly Dictionary<ulong, Blueprint> workshopBlueprintsBySteamId = new Dictionary<ulong, Blueprint>();

	public abstract Task<List<Blueprint>> GetBlueprints(int page, BlueprintSortInfo sortInfo);

	public abstract int GetMaxPageNumber();

	public virtual void ClearCache()
	{
		workshopBlueprintsBySteamId.Clear();
		blueprintsCache.Clear();
	}
}
