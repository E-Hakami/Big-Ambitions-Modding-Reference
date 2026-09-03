using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using Helpers;
using Player.HUD.ItemWarningIcons;
using UnityEngine;
using UnityEngine.AI;

namespace Buildings.Retail.Businesses.CinemaTheater;

public static class CinemaTheaterHelper
{
	private const int CinemaDutyCycleHours = 2;

	private static readonly List<ScreenCinemaController> CinemaScreens = new List<ScreenCinemaController>();

	private static readonly HashSet<SeatController> UsableSeats = new HashSet<SeatController>();

	private static readonly List<Transform> SittingPositions = new List<Transform>();

	private static readonly Dictionary<Transform, bool> SittingPositionDutyCycleOffsets = new Dictionary<Transform, bool>();

	private static readonly Dictionary<Transform, ThirdPersonCharacter> Reservations = new Dictionary<Transform, ThirdPersonCharacter>();

	public static float CinemaScreenLicensingFee => InstanceBehavior<GlobalReferences>.Instance.cinemaTheaterParams.cinemaScreenLicensingFee;

	public static float TheaterStageLicensingFee => InstanceBehavior<GlobalReferences>.Instance.cinemaTheaterParams.theaterStageLicensingFee;

	public static bool HasEvaluatedSeats { get; private set; }

	public static void PopulateLists()
	{
		ClearLists();
		if (!BuildingManager.IsInsideBuilding || (InstanceBehavior<BuildingManager>.Instance.building.BuildingType != "ba:buildingtype_cinema" && InstanceBehavior<BuildingManager>.Instance.building.BuildingType != "ba:buildingtype_theater"))
		{
			return;
		}
		bool flag = InstanceBehavior<BuildingManager>.Instance.building.BuildingType == "ba:buildingtype_cinema";
		if (flag)
		{
			CinemaScreens.AddRange(InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x is ScreenCinemaController).Cast<ScreenCinemaController>());
		}
		IEnumerable<ItemController> enumerable = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where(delegate(ItemController x)
		{
			if (!string.IsNullOrEmpty(x.itemName) && (x.Item.type & ItemType.Seat) != 0)
			{
				Transform[] sittingPositions2 = x.sittingPositions;
				if (sittingPositions2 != null)
				{
					return sittingPositions2.Length > 0;
				}
				return false;
			}
			return false;
		});
		List<SeatController> list = new List<SeatController>();
		foreach (ItemController item2 in enumerable)
		{
			if (!(item2 is SeatController item))
			{
				continue;
			}
			list.Add(item);
			bool flag2 = false;
			Transform[] sittingPositions = item2.sittingPositions;
			foreach (Transform transform in sittingPositions)
			{
				if (flag)
				{
					ScreenCinemaController visibleCinemaScreen = GetVisibleCinemaScreen(transform);
					if (!visibleCinemaScreen)
					{
						continue;
					}
					int num2 = CinemaScreens.IndexOf(visibleCinemaScreen);
					SittingPositionDutyCycleOffsets[transform] = num2 % 2 == 1;
				}
				else if (!GetVisibleTheaterStage(transform))
				{
					continue;
				}
				SittingPositions.Add(transform);
				flag2 = true;
			}
			if (flag2)
			{
				UsableSeats.Add(item);
			}
		}
		HasEvaluatedSeats = true;
		if ((bool)InstanceBehavior<ItemWarningIconManager>.Instance)
		{
			foreach (SeatController item3 in list)
			{
				InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(item3);
			}
		}
		TheaterStage.DeferRedistributeActors();
	}

	private static ScreenCinemaController GetVisibleCinemaScreen(Transform sittingPosition)
	{
		Vector3 forward = sittingPosition.forward;
		Vector3 vector = sittingPosition.position + Vector3.up;
		foreach (ScreenCinemaController cinemaScreen in CinemaScreens)
		{
			Vector3 vector2 = cinemaScreen.transform.position - vector;
			if (!(Vector3.Angle(forward, vector2) > 80f))
			{
				float num = vector2.magnitude - 0.1f;
				if (!(num <= 0f) && !Physics.Raycast(vector, vector2, num, LayerHelper.wallsLayerMask))
				{
					return cinemaScreen;
				}
			}
		}
		return null;
	}

	private static TheaterStage GetVisibleTheaterStage(Transform sittingPosition)
	{
		Vector3 forward = sittingPosition.forward;
		Vector3 vector = sittingPosition.position + Vector3.up * 2f;
		if (!TheaterStage.ActiveInstance)
		{
			return null;
		}
		Vector3 direction = TheaterStage.ActiveInstance.transform.position + Vector3.up * 2f - vector;
		Vector3 to = new Vector3(direction.x, 0f, direction.z);
		if (Vector3.Angle(forward, to) > 80f)
		{
			return null;
		}
		float num = direction.magnitude - 0.1f;
		if (num <= 0f || Physics.Raycast(vector, direction, num, LayerHelper.wallsLayerMask))
		{
			return null;
		}
		return TheaterStage.ActiveInstance;
	}

	public static bool EnsureSittingPositionReserved(Transform sittingPosition, ThirdPersonCharacter tpc)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.playerController.Character.isSittingOn == sittingPosition)
		{
			return false;
		}
		if (IsSittingPositionReserved(sittingPosition, tpc))
		{
			return false;
		}
		Reservations[sittingPosition] = tpc;
		return true;
	}

	public static bool IsSittingPositionTaken(Transform sittingPosition)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.playerController.Character.isSittingOn == sittingPosition)
		{
			return true;
		}
		if (Reservations.TryGetValue(sittingPosition, out var value) && (bool)value && value.isActiveAndEnabled)
		{
			return value.isSittingOn == sittingPosition;
		}
		return false;
	}

	private static bool IsSittingPositionReserved(Transform sittingPosition, ThirdPersonCharacter tpcToExclude)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.playerController.Character.isSittingOn == sittingPosition)
		{
			return true;
		}
		if (Reservations.TryGetValue(sittingPosition, out var value) && (bool)value && value != tpcToExclude)
		{
			return value.isActiveAndEnabled;
		}
		return false;
	}

	public static bool IsSittingPositionReservedBy(Transform sittingPosition, ThirdPersonCharacter tpc)
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.playerController.Character.isSittingOn == sittingPosition)
		{
			return false;
		}
		if (Reservations.TryGetValue(sittingPosition, out var value))
		{
			return tpc == value;
		}
		return false;
	}

	public static Transform TryReserveSittingPosition(ThirdPersonCharacter tpc)
	{
		foreach (KeyValuePair<Transform, ThirdPersonCharacter> reservation in Reservations)
		{
			if (reservation.Value == tpc && (bool)reservation.Key)
			{
				return reservation.Key;
			}
		}
		Transform transform = null;
		foreach (Transform sittingPosition in SittingPositions)
		{
			if ((bool)sittingPosition && !IsSittingPositionReserved(sittingPosition, tpc))
			{
				if (IsSittingPositionInActiveDutyCycle(sittingPosition))
				{
					transform = sittingPosition;
					break;
				}
				if (!transform)
				{
					transform = sittingPosition;
				}
			}
		}
		if ((bool)transform)
		{
			Reservations[transform] = tpc;
		}
		return transform;
	}

	public static bool IsValidSittingPosition(Transform sittingPosition)
	{
		return SittingPositions.Contains(sittingPosition);
	}

	private static bool IsSittingPositionInActiveDutyCycle(Transform sittingPosition)
	{
		if (InstanceBehavior<BuildingManager>.Instance.building.BuildingType == "ba:buildingtype_theater")
		{
			return true;
		}
		if (!SittingPositionDutyCycleOffsets.TryGetValue(sittingPosition, out var value))
		{
			return true;
		}
		bool flag = TimeHelper.CurrentHour % 2 < 1;
		if (!value)
		{
			return flag;
		}
		return !flag;
	}

	public static int GetDutyCycleEndHour(Transform sittingPosition)
	{
		if (!SittingPositionDutyCycleOffsets.TryGetValue(sittingPosition, out var value))
		{
			return TimeHelper.CurrentHour + 2;
		}
		int num = TimeHelper.CurrentHour % 2;
		bool flag = num < 1;
		if (value)
		{
			flag = !flag;
		}
		return (TimeHelper.CurrentHour + (flag ? (1 - num) : (2 - num))) % 24;
	}

	public static void ReleaseSittingPosition(ThirdPersonCharacter tpc)
	{
		KeyValuePair<Transform, ThirdPersonCharacter> keyValuePair = Reservations.FirstOrDefault((KeyValuePair<Transform, ThirdPersonCharacter> x) => x.Value == tpc);
		if ((bool)keyValuePair.Key)
		{
			Reservations.Remove(keyValuePair.Key);
		}
	}

	public static void ForceUnseatSittingPosition(Transform sittingPosition)
	{
		if (Reservations.TryGetValue(sittingPosition, out var value))
		{
			if ((bool)value && value.isActiveAndEnabled && value.TryGetComponent<Customer>(out var component))
			{
				component.CustomerLeaveBuilding();
			}
			Reservations.Remove(sittingPosition);
		}
	}

	public static bool IsSeatUsable(SeatController seatController)
	{
		return UsableSeats.Contains(seatController);
	}

	public static bool TryGetStandingAreaPosition(out Vector3 position)
	{
		position = Vector3.zero;
		if (!TheaterStage.ActiveInstance)
		{
			return false;
		}
		Transform audienceStandingArea = TheaterStage.ActiveInstance.audienceStandingArea;
		Bounds bounds = new Bounds(audienceStandingArea.position, audienceStandingArea.localScale);
		for (int i = 0; i < 10; i++)
		{
			if (NavMesh.SamplePosition(bounds.center + new Vector3(Random.Range(0f - bounds.extents.x, bounds.extents.x), 0f, Random.Range(0f - bounds.extents.z, bounds.extents.z)), out var hit, 1f, -1))
			{
				position = hit.position;
				return true;
			}
		}
		return false;
	}

	public static void ClearLists()
	{
		HasEvaluatedSeats = false;
		CinemaScreens.Clear();
		UsableSeats.Clear();
		SittingPositions.Clear();
		SittingPositionDutyCycleOffsets.Clear();
		Reservations.Clear();
	}
}
