using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BigAmbitions.DayNightCycle;
using Controllers;
using Data.VehicleColors;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using UI.Notification;
using UnityEngine;
using Vehicles;
using Vehicles.VehicleTypes;

namespace Buildings;

public class ContractVehicleForSale : IPurchasableAsset
{
	private readonly VehicleType _vehicleType;

	private string _selectedColorName;

	public string VehicleName => _vehicleType.vehicleTypeName;

	public ShowcaseVehicleController ShowcaseVehicleController { get; }

	public ContractVehicleForSale(VehicleType vehicleType, ShowcaseVehicleController showcaseVehicleController)
	{
		_vehicleType = vehicleType;
		ShowcaseVehicleController = showcaseVehicleController;
		_selectedColorName = GetDefaultColorName();
		if (ShowcaseVehicleController != null)
		{
			ShowcaseVehicleController.SetColor(_selectedColorName);
		}
	}

	public string GetLocalizeKey()
	{
		return VehicleName;
	}

	public float GetPurchasePrice()
	{
		return _vehicleType.price;
	}

	public string GetInitialColor()
	{
		return _selectedColorName;
	}

	public List<(string key, string value)> GetSpecs()
	{
		List<(string, string)> obj = new List<(string, string)>
		{
			("common_cargo_capacity", _vehicleType.maxCargoCapacity.ToString()),
			("vehicle_tank_capacity", _vehicleType.maxFuel.ToString(CultureInfo.InvariantCulture)),
			("vehicle_auto_park_support", (_vehicleType.autoParkSupported ? "common_yes" : "common_no").Localize().ToString())
		};
		string item = ((_vehicleType.maxSpeed == 0) ? "-" : _vehicleType.maxSpeed.ToString());
		obj.Add(("vehicle_max_speed", item));
		return obj;
	}

	public List<(string, Color32)> GetColors()
	{
		List<(string, Color32)> list = new List<(string, Color32)>();
		VehicleColor[] vehicleColors = InstanceBehavior<GlobalReferences>.Instance.vehicleColors;
		foreach (VehicleColor vehicleColor in vehicleColors)
		{
			list.Add((vehicleColor.name, vehicleColor.tint));
		}
		return list;
	}

	public void SetColor(string colorName, bool updateVisuals = true)
	{
		if (!string.IsNullOrEmpty(colorName))
		{
			_selectedColorName = colorName;
			if (ShowcaseVehicleController != null)
			{
				ShowcaseVehicleController.SetColor(colorName, updateVisuals);
			}
		}
	}

	public void ResetColor()
	{
		SetColor(_selectedColorName);
	}

	public bool Purchase()
	{
		if (ShowcaseVehicleController != null)
		{
			return ShowcaseVehicleController.Purchase();
		}
		List<Transform> customPositions = InstanceBehavior<BuildingManager>.Instance.cityBuildingController.customPositions;
		if (customPositions == null || customPositions.Count <= 0)
		{
			return false;
		}
		Transform transform = customPositions[0];
		Bounds bounds = new Bounds(transform.position, Vector3.one * 6f);
		for (int i = 0; i < SaveGameManager.Current.VehicleInstances.Count; i++)
		{
			VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances[i];
			if (bounds.Contains(vehicleInstance.position))
			{
				Notifications.ShowError("purchasevehicleui_notification_recently_purchase_vehicle_is_blocking", "purchasevehicleui_notification_recently_purchase_vehicle_is_blocking");
				return false;
			}
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "vehicleName", VehicleName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_vehiclebought", data);
		if (_vehicleType.taxDeductible)
		{
			transactionInfo.SetTaxDeductibleName("tax_vehicle");
		}
		if (!GameManager.ChangeMoneySafe(0f - GetPurchasePrice(), transactionInfo, null, null, force: false, showNotification: true))
		{
			return false;
		}
		float fuel = _vehicleType.maxFuel * Random.Range(0.97f, 0.98f);
		VehicleHelper.CreateAndSpawnVehicle(new VehicleInstance(VehicleName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = _selectedColorName,
			fuel = fuel
		}, transform.position, transform.rotation);
		return true;
	}

	public void Order(Address deliveryAddress, Contact storeContact, bool showNotification)
	{
		if (ShowcaseVehicleController != null)
		{
			ShowcaseVehicleController.Order(deliveryAddress, storeContact, showNotification);
		}
		else if (!(deliveryAddress == null) && storeContact != null && !(storeContact.Address == null))
		{
			VehicleStoreSettings vehicleStoreSettings = VehicleDeliveryHelper.GetVehicleStoreSettings(storeContact.Address);
			if (!(vehicleStoreSettings == null))
			{
				float deliveryPrice = GetPurchasePrice() + vehicleStoreSettings.deliveryPrice;
				Timestamp timestamp = TimeHelper.Now().AddHours(vehicleStoreSettings.hoursToDeliver);
				VehicleDeliveryContract item = new VehicleDeliveryContract
				{
					deliveryAddress = deliveryAddress,
					vehicleStoreAddress = storeContact.Address,
					deliveryDay = timestamp.Day,
					deliveryHour = timestamp.Hour,
					vehicleColor = _selectedColorName,
					vehicleTypeName = VehicleName,
					deliveryPrice = deliveryPrice
				};
				SaveGameManager.Current.vehicleDeliveryContracts.Add(item);
			}
		}
	}

	public IEnumerator ShowcaseAnimation()
	{
		if (!(ShowcaseVehicleController == null))
		{
			IEnumerator animation = ShowcaseVehicleController.ShowcaseAnimation();
			while (animation.MoveNext())
			{
				yield return animation.Current;
			}
		}
	}

	public IEnumerator CancelShowcaseAnimation()
	{
		if (!(ShowcaseVehicleController == null))
		{
			IEnumerator animation = ShowcaseVehicleController.CancelShowcaseAnimation();
			while (animation.MoveNext())
			{
				yield return animation.Current;
			}
		}
	}

	private static string GetDefaultColorName()
	{
		VehicleColor[] vehicleColors = InstanceBehavior<GlobalReferences>.Instance.vehicleColors;
		if (vehicleColors != null && vehicleColors.Length > 0)
		{
			return vehicleColors[0].name;
		}
		return string.Empty;
	}
}
