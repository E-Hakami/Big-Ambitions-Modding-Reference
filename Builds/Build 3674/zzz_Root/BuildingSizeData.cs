using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/BuildingSizeData")]
public class BuildingSizeData : ScriptableObject
{
	public string buildingSize;

	public BuildingVersion[] buildingVersions;

	public int squareMeters;

	public float[] wallHeights;

	public int numberOfEntrances = 1;

	public int numberOfVehicleSlots;

	public CustomerCapacity[] customerCapacities;

	public int GetCustomerCapacity(string buildingType, int buildingVersion)
	{
		CustomerCapacity[] array = customerCapacities;
		foreach (CustomerCapacity customerCapacity in array)
		{
			if (customerCapacity.buildingType == buildingType && (customerCapacity.buildingVersion == 0 || customerCapacity.buildingVersion == buildingVersion))
			{
				return customerCapacity.amount;
			}
		}
		return -1;
	}
}
