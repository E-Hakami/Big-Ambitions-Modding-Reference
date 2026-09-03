using System.Collections.Generic;
using System.Linq;
using HGAttributes;

namespace Vehicles;

public class TowDestinationHelper
{
	public const string AddressableLabel = "TowDestinations";

	public static readonly List<TowDestinationData> TowDestinations = new List<TowDestinationData>();

	[AutocompleteProvider("TowDestinations")]
	private static IEnumerable<string> AllTowTypes => TowDestinations.Select((TowDestinationData tow) => tow.towType);

	public static void OnTowDestinationsLoaded(IList<TowDestinationData> towDestinations)
	{
		TowDestinations.Clear();
		TowDestinations.AddRange(towDestinations);
	}

	public static TowDestinationData GetData(string towType)
	{
		return TowDestinations.FirstOrDefault((TowDestinationData tow) => tow.towType == towType);
	}
}
