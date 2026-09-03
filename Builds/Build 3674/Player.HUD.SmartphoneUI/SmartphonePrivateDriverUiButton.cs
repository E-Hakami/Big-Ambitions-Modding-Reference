using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.SmartphoneUI;

public class SmartphonePrivateDriverUiButton : MonoBehaviour
{
	[SerializeField]
	private SmartphonePrivateDriverUI privateDriverUI;

	[SerializeField]
	private TextLocalizationComponent localizationComponent;

	private VehicleInstance _vehicleInstance;

	public void SetVehicleInstance(VehicleInstance vehicleInstance)
	{
		_vehicleInstance = vehicleInstance;
		localizationComponent.Key = vehicleInstance.vehicleTypeName;
	}

	public void OnClickButton()
	{
		if (_vehicleInstance != null)
		{
			privateDriverUI.OnClickVehicle(_vehicleInstance);
		}
	}
}
