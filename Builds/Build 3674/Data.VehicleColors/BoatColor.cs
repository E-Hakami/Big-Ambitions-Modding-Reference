using UnityEngine;

namespace Data.VehicleColors;

[CreateAssetMenu(menuName = "BigAmbitions/BoatColor")]
public class BoatColor : ScriptableObject
{
	public Color32 primaryColor;

	public Color32 secondaryColor;
}
