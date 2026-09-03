using System.Collections.Generic;
using Buildings;
using GleyTrafficSystem;
using Helpers;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Guiders;
using UI.Notification;
using UnityEngine;

namespace Player.HUD.SmartphoneUI;

public class SmartphonePrivateDriverUI : MonoBehaviour
{
	private PrivateDriverVehicle _privateDriverVehicle;

	[SerializeField]
	private SmartphonePrivateDriverUiButton buttonTemplate;

	[SerializeField]
	private GameObject listView;

	[SerializeField]
	private GameObject emptyView;

	[SerializeField]
	private GameObject dismissView;

	[SerializeField]
	private TextLocalizationComponent emptyText;

	[SerializeField]
	private TextLocalizationComponent dismissText;

	[SerializeField]
	private SpecialService privateDriverService;

	public static PrivateDriverVehicle CurrentVehicle
	{
		get
		{
			if (!InstanceBehavior<UIs>.Instance)
			{
				return null;
			}
			return InstanceBehavior<UIs>.Instance.smartphoneUI.privateDriverUI._privateDriverVehicle;
		}
	}

	private void Start()
	{
		emptyText.Arguments = new { privateDriverService.businessName };
	}

	private void OnEnable()
	{
		RebuildUI();
	}

	public void RebuildUI()
	{
		if ((bool)InstanceBehavior<GuidersManager>.Instance && !_privateDriverVehicle)
		{
			GuidersManager.ResetGuider(DirectionGuiderType.PrivateDriver);
		}
		List<VehicleInstance> privateDriverVehicleInstances = SaveGameManager.Current.privateDriverVehicleInstances;
		bool flag = privateDriverVehicleInstances != null && privateDriverVehicleInstances.Count > 0;
		dismissView.SetActive(_privateDriverVehicle);
		listView.SetActive(!_privateDriverVehicle & flag);
		emptyView.SetActive(!_privateDriverVehicle && !flag);
		if ((bool)_privateDriverVehicle)
		{
			dismissText.Arguments = new
			{
				vehicleName = _privateDriverVehicle.VehicleType.vehicleTypeName
			};
			return;
		}
		buttonTemplate.gameObject.SetActive(value: false);
		foreach (Transform item in buttonTemplate.transform.parent)
		{
			if (!(item == buttonTemplate.transform))
			{
				Object.Destroy(item.gameObject);
			}
		}
		if (!flag)
		{
			return;
		}
		foreach (VehicleInstance privateDriverVehicleInstance in SaveGameManager.Current.privateDriverVehicleInstances)
		{
			SmartphonePrivateDriverUiButton smartphonePrivateDriverUiButton = Object.Instantiate(buttonTemplate, buttonTemplate.transform.parent);
			smartphonePrivateDriverUiButton.gameObject.SetActive(value: true);
			smartphonePrivateDriverUiButton.SetVehicleInstance(privateDriverVehicleInstance);
		}
	}

	public void OnClickPrivateDriverButton()
	{
		base.gameObject.SetActive(!base.gameObject.activeSelf);
	}

	public void OnClickVehicle(VehicleInstance vehicleInstance)
	{
		if (TaxiSystem.IsTraveling)
		{
			return;
		}
		if ((bool)_privateDriverVehicle)
		{
			DismissPrivateDriver();
		}
		if (SaveGameManager.Current.activePrivateDriverContractUnpaid && !PrivateDriverHelpers.PayForActiveContract())
		{
			return;
		}
		if (SubwaySystem.IsRiding)
		{
			Notifications.ShowError("ba:notification_private_driver_subway", null, trackOnSaveGame: false);
			return;
		}
		_privateDriverVehicle = PrivateDriverHelpers.SummonPrivateDriverVehicle(vehicleInstance);
		if (!_privateDriverVehicle)
		{
			Notifications.ShowError("ba:notification_private_driver_unreachable", null, trackOnSaveGame: false);
			return;
		}
		RebuildUI();
		base.gameObject.SetActive(value: false);
	}

	public void DismissPrivateDriver()
	{
		DismissPrivateDriver(force: false);
	}

	public void DismissPrivateDriver(bool force, VehicleInstance vehicleInstance = null, bool instantRemove = false)
	{
		if (!_privateDriverVehicle || (TaxiSystem.IsTraveling && !force) || (vehicleInstance != null && _privateDriverVehicle.vehicleInstance != vehicleInstance))
		{
			return;
		}
		if (CityMap.IsOpen && InstanceBehavior<CityManager>.Instance.cityMap.Taxi as PrivateDriverVehicle == _privateDriverVehicle)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
		}
		int vehicleIndex = _privateDriverVehicle.GetVehicleIndex();
		if (vehicleIndex >= 0)
		{
			if (instantRemove)
			{
				TrafficManager.Instance.RemoveVehicle(vehicleIndex);
			}
			else
			{
				_privateDriverVehicle.DriveAway();
			}
		}
		else
		{
			ParkingSimulator.ReleaseParkedVehicle(_privateDriverVehicle.gameObject);
		}
		_privateDriverVehicle = null;
		RebuildUI();
	}

	public static bool IsCurrentVehicle(GameObject vehicleObject)
	{
		PrivateDriverVehicle currentVehicle = CurrentVehicle;
		if ((bool)currentVehicle)
		{
			return currentVehicle.gameObject == vehicleObject;
		}
		return false;
	}

	public void OnVehicleDestroyed(PrivateDriverVehicle vehicle)
	{
		if (_privateDriverVehicle == vehicle)
		{
			_privateDriverVehicle = null;
		}
	}
}
