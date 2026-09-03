using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoParkSettings", menuName = "BigAmbitions/AutoParkSettings")]
public class AutoParkSettings : ScriptableObject
{
	[BoxGroup("Spot Generation")]
	[Tooltip("How much free curb a gap needs before it becomes a spot. Measured from bumper to bumper.")]
	[Range(0f, 15f)]
	[SerializeField]
	private float minGapLength = 5.5f;

	[BoxGroup("Spot Generation")]
	[Tooltip("Room kept to the cars on both ends of a spot. Stops a flush park from despawning the neighbors.")]
	[Range(0f, 2f)]
	[SerializeField]
	private float obstaclePadding = 0.5f;

	[BoxGroup("Spot Offering")]
	[Tooltip("Extra length a spot needs on top of the car itself before it lights up. Counted per end.")]
	[Range(0f, 2f)]
	[SerializeField]
	private float vehiclePadding = 0.25f;

	public float ObstaclePadding => obstaclePadding;

	public float VehiclePadding => vehiclePadding;

	public float MinSpotLength => Mathf.Max(0f, minGapLength - obstaclePadding * 2f);
}
