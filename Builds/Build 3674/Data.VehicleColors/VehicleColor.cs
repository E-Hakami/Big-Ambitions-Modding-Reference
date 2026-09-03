using UnityEngine;

namespace Data.VehicleColors;

[CreateAssetMenu(menuName = "BigAmbitions/VehicleColor")]
public class VehicleColor : ScriptableObject
{
	public float randomWeight;

	public Color32 tint;

	public Color32 fresnelColor;

	public float fresnelPower;
}
