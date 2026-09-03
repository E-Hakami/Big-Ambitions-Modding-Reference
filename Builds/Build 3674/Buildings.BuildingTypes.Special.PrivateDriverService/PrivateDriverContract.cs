using System.Collections.Generic;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

[CreateAssetMenu(fileName = "PrivateDriverContract", menuName = "BigAmbitions/SpecialService/PrivateDriverContract", order = 0)]
public class PrivateDriverContract : ScriptableObject
{
	public string key;

	public string description;

	public float costPerDay;

	public int maxCars;

	public List<string> usableVehicleTypes;
}
