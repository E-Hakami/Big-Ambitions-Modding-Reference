using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverVehicleListEntry : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent nameText;

	[SerializeField]
	private Button unassignButton;

	private VehicleInstance _vehicleInstance;

	public void SetupEntry(VehicleInstance vehicleInstance)
	{
		_vehicleInstance = vehicleInstance;
		nameText.Key = _vehicleInstance.vehicleTypeName;
		unassignButton.onClick.AddListener(OnUnassign);
	}

	private void OnUnassign()
	{
		if (_vehicleInstance != null && DialogController.current.dialog is PrivateDriverServiceDialog privateDriverServiceDialog)
		{
			privateDriverServiceDialog.OnUnassignVehicle(_vehicleInstance).ShowEntry();
		}
	}
}
