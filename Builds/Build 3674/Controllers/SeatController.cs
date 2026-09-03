using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings;
using Buildings.Retail.Businesses.CinemaTheater;
using Entities;
using Extensions;
using Helpers;
using Player.HUD.ItemWarningIcons;
using PlayerActivity;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class SeatController : ItemController
{
	[SerializeField]
	private RestEnvironment restEnvironment;

	private readonly HashSet<Transform> _occupiedSittingPositions = new HashSet<Transform>();

	[SerializeField]
	private bool showCinemaTheaterSeatWarning;

	private bool _isInteractingWithParent;

	public RestEnvironment RestEnvironment => restEnvironment;

	public bool CanSit
	{
		get
		{
			if (allowResting && HasAvailableSittingPosition() && CanUseAvailableSpotOnOccupiedSeat() && !playerItemPurchaserSettings.enabled)
			{
				if (ItemPanelUI.IsVisible)
				{
					return PlayerHelper.IsHoldingBag;
				}
				return true;
			}
			return false;
		}
	}

	public override Transform SittingPosition
	{
		get
		{
			Transform sittingPosition = base.SittingPosition;
			if (IsSittingPositionAvailable(sittingPosition))
			{
				return sittingPosition;
			}
			Transform[] array = sittingPositions;
			foreach (Transform transform in array)
			{
				if (IsSittingPositionAvailable(transform))
				{
					return transform;
				}
			}
			return null;
		}
	}

	public override bool Occupied
	{
		get
		{
			if (HasOccupiedSittingPositions())
			{
				return true;
			}
			if (IsInCinemaOrTheater() && sittingPositions.Any(CinemaTheaterHelper.IsSittingPositionTaken))
			{
				return true;
			}
			return base.Occupied;
		}
		set
		{
			base.Occupied = value;
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (!allowResting)
		{
			customValue = "DisallowResting";
		}
	}

	public override void Start()
	{
		base.Start();
		UpdateRestingAvailability();
		SpawnDummyAiIfNeeded();
	}

	public virtual void OnSittingChanged(Transform seat, bool isSitting)
	{
		if (HasSittingPosition(seat))
		{
			if (isSitting)
			{
				_occupiedSittingPositions.Add(seat);
			}
			else
			{
				_occupiedSittingPositions.Remove(seat);
			}
			Occupied = HasOccupiedSittingPositions();
		}
	}

	private void SpawnDummyAiIfNeeded()
	{
		if (BuildingManager.isBuildingTemporarilyEditable)
		{
			return;
		}
		Building building = base.BuildingContext.Building;
		if (!ShouldSpawnDummyAi(building) || !building.SpecialService.settings.NpcSpawnersDictionary.TryGetValue(itemName, out var value))
		{
			return;
		}
		Transform[] array = sittingPositions;
		foreach (Transform sittingPosition in array)
		{
			if (value.spawnChance.Probability())
			{
				BuildingStationaryAiBehavior buildingStationaryAiBehavior = InstanceBehavior<CityManager>.Instance.pedestrianSpawner.buildingStationaryAiPool.GetPoolHandler().Get();
				InstanceBehavior<CityManager>.Instance.pedestrianSpawner.activeBuildingStationaryAis.Add(buildingStationaryAiBehavior);
				buildingStationaryAiBehavior.Initialize(value.buildingStationaryAiData, this, sittingPosition);
			}
		}
	}

	private bool ShouldSpawnDummyAi(Building building)
	{
		if (BuildingManager.IsInsideBuilding && building.SpecialService?.settings?.NpcSpawnersDictionary != null && string.IsNullOrEmpty(customValue) && !playerItemPurchaserSettings.enabled)
		{
			return !IsInAnEmployeeStation();
		}
		return false;
	}

	private void PerformActivity()
	{
		if (PlayerHelper.IsHoldingItem && !PlayerHelper.IsHoldingBag)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "fromname", itemName },
				{
					"toname",
					PlayerHelper.ItemInstanceInHands.itemName
				}
			};
			Notifications.Show(NotificationType.Warning, "notification_cant_interact_with_item_in_hand", notificationData, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		}
		else if (!CanSit)
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "itemname", itemName } };
			Notifications.Show(NotificationType.Error, "notification_this_item_is_occupied", notificationData2, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		}
		else
		{
			PlayerActivityUI.Show(restEnvironment, this);
		}
	}

	public void UpdateRestingAvailability()
	{
		if (playerItemPurchaserSettings.enabled || customValue == "DisallowResting")
		{
			DisableResting();
		}
		else if (!(base.BuildingContext.Building.BuildingType == "ba:buildingtype_residential") && IsInAnEmployeeStation())
		{
			DisableResting();
		}
		else
		{
			EnableResting();
		}
	}

	private bool IsInAnEmployeeStation()
	{
		if (!(base.BuildingContext.Building.BuildingType == "ba:buildingtype_residential") && !(base.BuildingContext.Registration.businessTypeName == "ba:businesstype_school") && parentItemController != null)
		{
			if ((parentItemController.Item.type & ItemType.EmployeeWorkstation) == 0 && !(parentItemController is BusinessEmployeeController))
			{
				return parentItemController.childItemControllers.Any((ItemController x) => (x.Item.type & ItemType.PointOfSale) == 0 && ((x.Item.type & ItemType.EmployeeWorkstation) != 0 || x is BusinessEmployeeController));
			}
			return true;
		}
		return false;
	}

	private void EnableResting()
	{
		allowResting = true;
	}

	private void DisableResting()
	{
		allowResting = false;
	}

	public Transform GetBestAngledSeatingPosition(Vector3 targetPosition)
	{
		Vector2 vector = new Vector2(targetPosition.x, targetPosition.z);
		float num = float.MaxValue;
		Transform result = null;
		Transform[] array = sittingPositions;
		foreach (Transform transform in array)
		{
			if (IsSittingPositionAvailable(transform))
			{
				Vector2 vector2 = new Vector2(transform.position.x, transform.position.z);
				Vector2 normalized = (vector - vector2).normalized;
				float num2 = Vector2.Angle(new Vector2(transform.forward.x, transform.forward.z), normalized);
				if (!(num2 >= num))
				{
					num = num2;
					result = transform;
				}
			}
		}
		return result;
	}

	public (string, Action) GetSeatControllerCta()
	{
		if (playerItemPurchaserSettings.enabled || !allowResting || PlayerHelper.IsUsingVehicle)
		{
			return ("", null);
		}
		if (PlayerHelper.IsHoldingItem && !PlayerHelper.IsHoldingBag)
		{
			return ("click_to_rest", PerformActivity);
		}
		if (IsInCinemaOrTheater())
		{
			if (sittingPositions.All(CinemaTheaterHelper.IsSittingPositionTaken))
			{
				return ("", null);
			}
			if (CinemaTheaterHelper.IsSeatUsable(this))
			{
				return ("click_to_watch_show", PerformActivity);
			}
		}
		return ("click_to_rest", PerformActivity);
	}

	public override bool ShouldReactToIoEnter()
	{
		if ((bool)parentItemController && parentItemController.ShouldReactToIoEnter())
		{
			return true;
		}
		return base.ShouldReactToIoEnter();
	}

	public override WarningIconType GetWarningIconType()
	{
		WarningIconType warningIconType = base.GetWarningIconType();
		if (warningIconType != WarningIconType.None)
		{
			return warningIconType;
		}
		if (!showCinemaTheaterSeatWarning || !IsInCinemaOrTheater() || !CinemaTheaterHelper.HasEvaluatedSeats || CinemaTheaterHelper.IsSeatUsable(this))
		{
			return WarningIconType.None;
		}
		return WarningIconType.Danger;
	}

	private bool HasSittingPosition(Transform seat)
	{
		if (!seat)
		{
			return false;
		}
		Transform[] array = sittingPositions;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == seat)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasAvailableSittingPosition()
	{
		Transform[] array = sittingPositions;
		foreach (Transform sittingPosition in array)
		{
			if (IsSittingPositionAvailable(sittingPosition))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanUseAvailableSpotOnOccupiedSeat()
	{
		if (base.Occupied)
		{
			return HasOccupiedSittingPositions();
		}
		return true;
	}

	private bool HasOccupiedSittingPositions()
	{
		return _occupiedSittingPositions.Count > 0;
	}

	public bool IsSittingPositionAvailable(Transform sittingPosition)
	{
		if ((bool)sittingPosition && sittingPosition.childCount == 0)
		{
			return !_occupiedSittingPositions.Contains(sittingPosition);
		}
		return false;
	}

	public static bool IsInCinemaOrTheater()
	{
		if (!BuildingManager.IsInsideBuilding || !InstanceBehavior<BuildingManager>.Instance.businessType)
		{
			return false;
		}
		string businessTypeName = InstanceBehavior<BuildingManager>.Instance.businessType.businessTypeName;
		if (!(businessTypeName == "ba:businesstype_cinema"))
		{
			return businessTypeName == "ba:businesstype_theater";
		}
		return true;
	}
}
