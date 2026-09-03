using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/VehicleStoreSettings")]
public class VehicleStoreSettings : SpecialServiceSettings
{
	public bool canDeliver;

	public float deliveryPrice = 5000f;

	public int hoursToDeliver = 2;
}
