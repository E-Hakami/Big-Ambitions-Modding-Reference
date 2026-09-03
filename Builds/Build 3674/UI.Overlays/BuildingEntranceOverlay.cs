using System;
using Helpers;
using Streets;
using UI.Elements;
using UI.Smartphone;
using UnityEngine;

namespace UI.Overlays;

[Serializable]
public class BuildingEntranceOverlay : IOverlay
{
	private CityBuildingController _currentCBC;

	private Transform _currentBuildingDoor;

	private LabelInfo BuildingTypeLabel => new LabelInfo(_currentCBC.building.BuildingType, InstanceBehavior<GlobalReferences>.Instance.colors.white);

	private LabelInfo BusinessNameLabel => new LabelInfo(_currentCBC.buildingRegistration.BusinessName, InstanceBehavior<GlobalReferences>.Instance.colors.white, localize: false);

	private LabelInfo RentedByYouLabel => new LabelInfo("buildingresume_rented_by_you", InstanceBehavior<GlobalReferences>.Instance.colors.yellow);

	private LabelInfo OccupiedLabel => new LabelInfo("buildingresume_occupied", InstanceBehavior<GlobalReferences>.Instance.colors.red);

	private LabelInfo BusinessOpenLabel => new LabelInfo("common_open", InstanceBehavior<GlobalReferences>.Instance.colors.green);

	private LabelInfo BusinessClosedLabel => new LabelInfo("common_closed", InstanceBehavior<GlobalReferences>.Instance.colors.red);

	private ButtonInfo OpenInBizManButton => new ButtonInfo("OpenInBizMan", "open_in_bizman", "gray", OpenInBizMan, PlayerAction.SecondaryInteract);

	public static void Show(CityBuildingController cbc, Transform door)
	{
		InstanceBehavior<UIs>.Instance.overlayUI.buildingEntrance.SetBuilding(cbc, door);
		OverlayUI.Show(InstanceBehavior<UIs>.Instance.overlayUI.buildingEntrance);
	}

	public static void Hide()
	{
		OverlayUI.Hide(InstanceBehavior<UIs>.Instance.overlayUI.buildingEntrance);
	}

	private void SetBuilding(CityBuildingController cbc, Transform door)
	{
		_currentCBC = cbc;
		_currentBuildingDoor = door;
	}

	private void EnterBuilding()
	{
		if (SubwaySystem.IsRiding || BuildingPreview.isPreviewing || FullMenu.IsOpen)
		{
			return;
		}
		if (!BuildingHelper.CanEnterBuilding(_currentCBC.buildingRegistration.Address) && _currentCBC.buildingRegistration.AvailableForRent)
		{
			OpenInBizMan();
			return;
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (!(selectedVehicle != null) || selectedVehicle.vehicleType.spawnInPlayerObject)
		{
			_currentCBC.Interact();
		}
	}

	private void OpenInBizMan()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(_currentCBC.building.Address, "Presentation");
	}

	public Vector3 GetTargetPosition()
	{
		return _currentBuildingDoor.position;
	}

	public LabelInfo GetFirstLineLabel()
	{
		if (!string.IsNullOrEmpty(_currentCBC.buildingRegistration.BusinessName))
		{
			return BusinessNameLabel;
		}
		return BuildingTypeLabel;
	}

	public LabelInfo GetSecondLineLeftLabel()
	{
		return AddressLabel(_currentCBC.building.Address);
	}

	public LabelInfo GetSecondLineRightLabel()
	{
		string buildingType = _currentCBC.building.BuildingType;
		if (buildingType == "ba:buildingtype_residential" || buildingType == "ba:buildingtype_warehouse")
		{
			if (_currentCBC.buildingRegistration.RentedByPlayer)
			{
				return RentedByYouLabel;
			}
			if (!_currentCBC.buildingRegistration.AvailableForRent)
			{
				return OccupiedLabel;
			}
			return AvailableLabel(InstanceBehavior<GlobalReferences>.Instance.colors.green);
		}
		if (_currentCBC.buildingRegistration.AvailableForRent)
		{
			return AvailableLabel(InstanceBehavior<GlobalReferences>.Instance.colors.blue);
		}
		if (!BusinessHelper.IsBusinessOpen(_currentCBC.buildingRegistration))
		{
			return BusinessClosedLabel;
		}
		return BusinessOpenLabel;
	}

	public ButtonInfo[] GetButtons()
	{
		bool interactable = false;
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle == null || currentVehicle.VehicleType.spawnInPlayerObject)
		{
			interactable = BuildingHelper.CanEnterBuilding(_currentCBC.building.Address);
		}
		return new ButtonInfo[2]
		{
			EnterBuildingButton(interactable),
			OpenInBizManButton
		};
	}

	private LabelInfo AvailableLabel(Color32 color)
	{
		return new LabelInfo("common_available_for_rent", color);
	}

	private LabelInfo AddressLabel(Address address)
	{
		return new LabelInfo(address.ToFormattedString(), InstanceBehavior<GlobalReferences>.Instance.colors.white, localize: false);
	}

	private ButtonInfo EnterBuildingButton(bool interactable)
	{
		return new ButtonInfo("EnterBuilding", "common_enter_building", "blue", EnterBuilding, PlayerAction.Interact, interactable);
	}
}
