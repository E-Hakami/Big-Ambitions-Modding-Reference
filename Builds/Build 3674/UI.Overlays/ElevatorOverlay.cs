using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.PlacementSystem;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using Player.PlayerMissions;
using UI.Elements;
using UI.Notification;
using UnityEngine;

namespace UI.Overlays;

[Serializable]
public class ElevatorOverlay : IOverlay
{
	private Floor _currentFloor;

	private Vector3 _elevatorPosition;

	public static void Show(Floor currentFloor, Vector3 elevatorPosition)
	{
		InstanceBehavior<UIs>.Instance.overlayUI.elevator.SetElevator(currentFloor, elevatorPosition);
		OverlayUI.Show(InstanceBehavior<UIs>.Instance.overlayUI.elevator);
	}

	public static void Hide()
	{
		OverlayUI.Hide(InstanceBehavior<UIs>.Instance.overlayUI.elevator);
	}

	private void SetElevator(Floor currentFloor, Vector3 elevatorPosition)
	{
		_currentFloor = currentFloor;
		_elevatorPosition = elevatorPosition;
	}

	private void ParkingFloor()
	{
		if (!PlacementSystem.IsInPlacementMode && !UndergroundParkingManager.IsInsideParking && BuildingManager.IsInsideBuilding)
		{
			CoroutineUtility.Run(EnterParkingFloorFromBuildingCoroutine(InstanceBehavior<BuildingManager>.Instance.cityBuildingController));
			Hide();
		}
	}

	private static IEnumerator EnterParkingFloorFromBuildingCoroutine(CityBuildingController cbc)
	{
		yield return InstanceBehavior<BuildingManager>.Instance.ExitFromBuildingCoroutine(0, playFadeAnimation: true, onlyFadeIn: true);
		yield return UndergroundParkingManager.EnterParkingCoroutine(cbc.undergroundParkingEntrance, 1, playFadeAnimation: true, onlyFadeOut: true);
	}

	private void BuildingFloor()
	{
		if (PlacementSystem.IsInPlacementMode || BuildingManager.IsInsideBuilding || !UndergroundParkingManager.IsInsideParking)
		{
			return;
		}
		CityBuildingController cbc = UndergroundParkingManager.currentParkingEntrance.parentCbc;
		PlayerMission currentPlayerMission = SaveGameManager.Current.currentPlayerMission;
		if (currentPlayerMission != null && currentPlayerMission.TryDeliverToAddress(cbc.building.Address))
		{
			return;
		}
		if (SaveGameManager.Current.interiorInstallationFirmContracts.Any((InteriorInstallationFirmContract x) => x.addressToDoTheInstallation == cbc.building.Address && x.dayOfInstallation - 1 <= SaveGameManager.Current.Day))
		{
			Notifications.ShowError("cant_enter_building_while_interior_installation", "cant_enter_building_while_interior_installation");
		}
		else if (SaveGameManager.Current.movingServiceContracts.Any((MovingServiceContract x) => (x.originMovingAddress == cbc.building.Address || x.destinationMovingAddress == cbc.building.Address) && x.movingDay - 1 <= SaveGameManager.Current.Day))
		{
			Notifications.ShowError("cant_enter_building_while_interior_installation", "cant_enter_building_while_interior_installation");
		}
		else if (BuildingHelper.CanEnterBuilding(cbc.building.Address))
		{
			float entranceFee = cbc.buildingRegistration.GetEntranceFeeForPlayer();
			if (entranceFee > 0f)
			{
				HudConfirm.Show("buildingmanager_entrance_fee_confirm".Localize(new
				{
					businessName = cbc.buildingRegistration.BusinessName,
					entranceFee = entranceFee.ToCurrencyFormat()
				}), default(LanguageChangeEventDataHolder), delegate
				{
					Dictionary<string, string> data = new Dictionary<string, string> { 
					{
						"businessName",
						cbc.buildingRegistration.BusinessName
					} };
					TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_entrancefee", data);
					if (GameManager.ChangeMoneySafe(0f - entranceFee, transactionInfo))
					{
						InstanceBehavior<BuildingManager>.Instance.EnterBuilding(cbc.building);
						if (cbc.buildingRegistration.businessTypeName == "ba:businesstype_nightclub")
						{
							NightclubBusinessHelper.OnEnterBuilding();
						}
					}
				});
			}
			else
			{
				InstanceBehavior<BuildingManager>.Instance.EnterBuilding(cbc.building);
			}
			Hide();
		}
		else
		{
			Notifications.Show(NotificationType.Error, "elevator_cant_enter_building", null, 4f, "CantEnterBuilding");
		}
	}

	private void ExitFloor()
	{
		if (!PlacementSystem.IsInPlacementMode)
		{
			if (UndergroundParkingManager.IsInsideParking)
			{
				UndergroundParkingManager.ExitParking();
			}
			else if (BuildingManager.IsInsideBuilding)
			{
				InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0);
			}
			Hide();
		}
	}

	public Vector3 GetTargetPosition()
	{
		return _elevatorPosition;
	}

	public LabelInfo GetFirstLineLabel()
	{
		return new LabelInfo("elevatoroverlay_header");
	}

	public LabelInfo GetSecondLineLeftLabel()
	{
		return null;
	}

	public LabelInfo GetSecondLineRightLabel()
	{
		return null;
	}

	public ButtonInfo[] GetButtons()
	{
		bool interactable = VehicleHelper.GetCurrentVehicle()?.VehicleType.spawnInPlayerObject ?? true;
		List<ButtonInfo> list = new List<ButtonInfo>();
		foreach (Floor value in Enum.GetValues(typeof(Floor)))
		{
			if (_currentFloor != value)
			{
				string color = ((value == Floor.Exit) ? "yellow" : "blue");
				Action onClick = value switch
				{
					Floor.Building => BuildingFloor, 
					Floor.Exit => ExitFloor, 
					Floor.Parking => ParkingFloor, 
					_ => null, 
				};
				PlayerAction playerAction = ((value != Floor.Exit) ? PlayerAction.Interact : PlayerAction.SecondaryInteract);
				ButtonInfo item = new ButtonInfo("Elevator" + value.ToStringFast(), value.GetLocalizeKey(), color, onClick, playerAction, interactable);
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
