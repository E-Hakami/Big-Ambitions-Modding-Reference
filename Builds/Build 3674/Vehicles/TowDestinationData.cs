using UnityEngine;

namespace Vehicles;

[CreateAssetMenu(fileName = "TowDestination", menuName = "BigAmbitions/Vehicles/TowDestination")]
public class TowDestinationData : ScriptableObject
{
	public string towType;

	public int servicePrice = 500;
}
