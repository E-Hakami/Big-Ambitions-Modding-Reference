using BigAmbitions.Tags;
using HGAttributes;
using NaughtyAttributes;
using UnityEngine;

namespace Vehicles.VehicleTypes;

[CreateAssetMenu(fileName = "VehicleType", menuName = "BigAmbitions/VehicleType", order = 0)]
public class VehicleType : TaggedScriptableObject
{
	private const string PlayerVehiclePrefabFolder = "Assets/Addressables/Prefabs/Vehicles/PlayerVehicles";

	private const string AIVehiclePrefabFolder = "Assets/Addressables/Prefabs/Vehicles";

	public string vehicleTypeName;

	public float price;

	[AutocompleteDropdown("Items")]
	public string itemVersion;

	[Header("Physical info")]
	public float maxFuel;

	public int maxCargoCapacity;

	public int maxSpeed;

	public float enginePower;

	public float brakeForce;

	[Range(20f, 70f)]
	public int turnRadius;

	[Range(0f, 1f)]
	public float damageIntensity;

	public bool fitsHandTruck;

	public bool fitsFlatbed;

	[Header("Perks")]
	public bool autoParkSupported;

	public bool taxDeductible;

	public bool hasRadio = true;

	public bool isLuxuryCar;

	[Header("Delivery vehicle info")]
	public int requiredDeliveryDriverSkillValue;

	public int destinationsThatCanDeliver;

	[Header("Miscellanea")]
	public bool countsForPersonalGoals = true;

	public bool spawnInPlayerObject;

	public bool usePedestrianCam;

	public int autoDestroyAfterMinutes = -1;

	public bool enclosed;

	[Header("Dirtiness")]
	public bool canGetDirty = true;

	[Tooltip("Time in seconds it takes for the vehicle to get from 0% to 100% dirty")]
	[ShowIf("canGetDirty")]
	public float dirtinessTimer = 600f;

	[Tooltip("Time in seconds it takes for the vehicle to get from 100% to 0% dirty in the rain")]
	[ShowIf("canGetDirty")]
	public float cleanByRainTimer = 360f;

	public bool IsMotorVehicle => maxFuel > 0f;
}
