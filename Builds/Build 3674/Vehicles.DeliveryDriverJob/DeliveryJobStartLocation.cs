using Data.VehicleColors;
using HGAttributes;
using UnityEngine;

namespace Vehicles.DeliveryDriverJob;

[CreateAssetMenu(fileName = "DeliveryJobStartLocation", menuName = "BigAmbitions/DeliveryJob/StartLocation")]
public class DeliveryJobStartLocation : ScriptableObject
{
	public string vehicleTypeName;

	public VehicleColor vehicleColor;

	public Sprite mapPoiIcon;

	public Color mapPoiColor;

	[AutocompleteDropdown("Items")]
	public string[] possibleItems;

	public float radius;

	public float minutesPerMeter;

	public int minutesMin;

	public int minutesMax;

	public int destinationsCountMin = 3;

	public int destinationsCountMax = 3;

	public int minBoxesPerDestination = 1;

	public int maxBoxesPerDestination = 3;

	public float deliveryReward = 100f;

	public DeliveryJobTipsConfig tipsConfig;

	[AutocompleteDropdown("BuildingTypes")]
	public string[] destinationBuildingTypes;
}
