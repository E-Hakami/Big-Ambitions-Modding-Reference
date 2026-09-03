using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;
using Vehicles;

namespace Buildings;

public class VehicleDeliveryContractEntry : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent infoText;

	[SerializeField]
	private Button cancelButton;

	private VehicleDeliveryContract _vehicleDeliveryContract;

	public void SetupEntry(VehicleDeliveryContract vehicleDeliveryContract)
	{
		_vehicleDeliveryContract = vehicleDeliveryContract;
		string displayName = BuildingHelper.GetBuildingRegistration(vehicleDeliveryContract.deliveryAddress).GetDisplayName();
		string localization = vehicleDeliveryContract.vehicleTypeName.GetLocalization();
		string colorInfo = GetColorInfo(vehicleDeliveryContract.vehicleColor);
		infoText.SetData("ba:messagetype_dialog_vehicle_store_delivery_contracts_list_info".Localize(new
		{
			vehicleName = localization,
			colorInfo = colorInfo,
			destinationName = displayName,
			day = TimeHelper.GetDayOfWeek(vehicleDeliveryContract.deliveryDay).GetLocalizeKey(),
			number = vehicleDeliveryContract.deliveryDay,
			hour = vehicleDeliveryContract.deliveryHour.GetFormattedTime()
		}));
		cancelButton.onClick.AddListener(OnCancel);
	}

	private void OnCancel()
	{
		if (_vehicleDeliveryContract != null && DialogController.current.dialog is VehicleStoreDialog vehicleStoreDialog)
		{
			vehicleStoreDialog.OnCancelVehicleDelivery(_vehicleDeliveryContract).ShowEntry();
		}
	}

	private static string GetColorInfo(string vehicleColorName)
	{
		if (!VehicleHelper.TryGetVehicleColor(vehicleColorName, out var resultVehicleColor))
		{
			return "";
		}
		string text = ColorUtility.ToHtmlStringRGB(resultVehicleColor.tint);
		return " (" + resultVehicleColor.name + " <color=#" + text + ">■</color>)";
	}
}
