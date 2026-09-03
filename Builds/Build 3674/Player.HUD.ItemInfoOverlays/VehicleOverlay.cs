using BigAmbitions.Tags;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;
using Vehicles.VehicleTypes;

namespace Player.HUD.ItemInfoOverlays;

public class VehicleOverlay : IOverlay
{
	[Header("Vehicle")]
	[SerializeField]
	private Button manageStorageButton;

	[SerializeField]
	private Button addHandTruckToStorageButton;

	[SerializeField]
	private Button addItemsToStorageButton;

	[SerializeField]
	private Button enterVehicleButton;

	[SerializeField]
	private TextLocalizationComponent enterVehicleField;

	private void Start()
	{
		manageStorageButton.onClick.AddListener(delegate
		{
			((VehicleController)linkedController).MoveAndManageStorage();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
		addHandTruckToStorageButton.onClick.AddListener(delegate
		{
			((VehicleController)linkedController).MoveAndAddCartToStorage();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
		addItemsToStorageButton.onClick.AddListener(delegate
		{
			((VehicleController)linkedController).MoveAndAddHandTruckItemsToStorage();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
		enterVehicleButton.onClick.AddListener(delegate
		{
			((VehicleController)linkedController).DriveVehicle();
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		});
	}

	public override bool IsValid(EntityController entityController)
	{
		return entityController is VehicleController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (CityMap.IsOpen || VehicleHelper.IsInsideMotorVehicle())
		{
			return false;
		}
		return (entityController as VehicleController).vehicleType.maxCargoCapacity > 0;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (CtaManager.ctaAction != null)
		{
			return;
		}
		VehicleController vehicleController = entityController as VehicleController;
		VehicleType vehicleType = vehicleController.vehicleType;
		if (SaveGameManager.Current.ActiveVehicleId != null)
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if (currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
			{
				bool num = vehicleType.fitsHandTruck && currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.canbestoredsmall);
				bool flag = vehicleType.fitsFlatbed && currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.canbestoredbig);
				if ((num | flag) && currentVehicle.cargoInstances.Count == 0 && vehicleController.vehicleInstance.cargoInstances.Count < vehicleType.maxCargoCapacity)
				{
					addHandTruckToStorageButton.gameObject.SetActive(value: true);
					manageStorageButton.gameObject.SetActive(value: true);
					addItemsToStorageButton.gameObject.SetActive(value: false);
					enterVehicleButton.gameObject.SetActive(value: false);
				}
				else if (currentVehicle.cargoInstances.Count > 0)
				{
					addItemsToStorageButton.gameObject.SetActive(value: true);
					manageStorageButton.gameObject.SetActive(value: true);
					addHandTruckToStorageButton.gameObject.SetActive(value: false);
					enterVehicleButton.gameObject.SetActive(value: false);
				}
			}
		}
		else if (PlayerHelper.ItemInstanceInHands == null && vehicleController != null && vehicleController.vehicleInstance.cargoInstances.Count > 0)
		{
			enterVehicleField.Key = (vehicleController.vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle) ? "use_cart" : "enter_vehicle");
			manageStorageButton.gameObject.SetActive(value: true);
			enterVehicleButton.gameObject.SetActive(value: true);
			addHandTruckToStorageButton.gameObject.SetActive(value: false);
			addItemsToStorageButton.gameObject.SetActive(value: false);
		}
	}
}
