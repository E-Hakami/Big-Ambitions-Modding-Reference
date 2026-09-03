using System.Collections.Generic;
using Controllers;
using Helpers;
using UnityEngine;

namespace PlayerActivity;

public class EntertainSeatingResolver
{
	private const int AngleMargin = 15;

	private const float MaxSeatingAngle = 45f;

	private readonly Collider[] _itemsInBox = new Collider[64];

	private readonly List<SeatController> _bestSeats = new List<SeatController>(20);

	private readonly EntityController _targetObject;

	private readonly float _maxSeatingDistance;

	private readonly float _halfMaxSeating;

	public EntertainSeatingResolver(EntityController targetObject, float maxSeatingDistance)
	{
		_targetObject = targetObject;
		_maxSeatingDistance = maxSeatingDistance;
		_halfMaxSeating = maxSeatingDistance / 2f;
	}

	public EntertainSeatingResult Resolve()
	{
		if (!(_targetObject != null))
		{
			return ResolveNearPlayer();
		}
		return ResolveFaceTargetObject();
	}

	private EntertainSeatingResult ResolveFaceTargetObject()
	{
		Transform transform = _targetObject.transform;
		int num = OverlapBox(transform.position + transform.forward * _halfMaxSeating);
		if (num <= 0)
		{
			return default(EntertainSeatingResult);
		}
		Vector3 position = transform.position;
		SeatController seatController = FindBestChairFacingTarget(num, position);
		if (!seatController)
		{
			return default(EntertainSeatingResult);
		}
		return new EntertainSeatingResult(seatController, seatController.GetBestAngledSeatingPosition(position));
	}

	private EntertainSeatingResult ResolveNearPlayer()
	{
		Vector3 position = PlayerHelper.GetPosition();
		int num = OverlapBox(position);
		if (num <= 0)
		{
			return default(EntertainSeatingResult);
		}
		SeatController seatController = null;
		Transform position2 = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			if (!_itemsInBox[i].TryGetComponent<SeatController>(out var component))
			{
				continue;
			}
			Transform closestSittingPosition = GetClosestSittingPosition(component, position);
			if ((bool)closestSittingPosition)
			{
				float num3 = Vector3.SqrMagnitude(position - closestSittingPosition.position);
				if (num3 < num2)
				{
					num2 = num3;
					seatController = component;
					position2 = closestSittingPosition;
				}
			}
		}
		if (!seatController)
		{
			return default(EntertainSeatingResult);
		}
		return new EntertainSeatingResult(seatController, position2);
	}

	private int OverlapBox(Vector3 center)
	{
		return Physics.OverlapBoxNonAlloc(center, new Vector3(_halfMaxSeating, 2.5f, _halfMaxSeating), _itemsInBox, Quaternion.identity, LayerHelper.interactiveItemsAndOutlinedLayerMask);
	}

	private SeatController FindBestChairFacingTarget(int numItems, Vector3 devicePosition)
	{
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		_bestSeats.Clear();
		for (int i = 0; i < numItems; i++)
		{
			if (!TryGetReachableSeat(i, devicePosition, out var seatController))
			{
				continue;
			}
			float minSeatAngleFacingTarget = GetMinSeatAngleFacingTarget(seatController, devicePosition);
			if (!(minSeatAngleFacingTarget > 45f))
			{
				if (minSeatAngleFacingTarget < num)
				{
					num = minSeatAngleFacingTarget;
					_bestSeats.Clear();
					_bestSeats.Add(seatController);
				}
				else if (Mathf.Abs(minSeatAngleFacingTarget - num) < 15f)
				{
					_bestSeats.Add(seatController);
				}
			}
		}
		if (_bestSeats.Count == 1)
		{
			return _bestSeats[0];
		}
		SeatController result = null;
		foreach (SeatController bestSeat in _bestSeats)
		{
			float num3 = Vector3.SqrMagnitude(devicePosition - bestSeat.transform.position);
			if (num3 < num2)
			{
				num2 = num3;
				result = bestSeat;
			}
		}
		return result;
	}

	private bool TryGetReachableSeat(int itemIndex, Vector3 devicePosition, out SeatController seatController)
	{
		seatController = null;
		if (!_itemsInBox[itemIndex].TryGetComponent<SeatController>(out seatController))
		{
			return false;
		}
		Vector3 position = seatController.transform.position;
		return !Physics.Raycast(devicePosition, position - devicePosition, Mathf.Min(Vector3.Distance(devicePosition, position), _maxSeatingDistance), LayerHelper.wallsLayerMask);
	}

	private static float GetMinSeatAngleFacingTarget(SeatController seatController, Vector3 targetPosition)
	{
		Transform transform = seatController.transform;
		Vector2 vector = new Vector2(transform.position.x, transform.position.z);
		Vector2 normalized = (new Vector2(targetPosition.x, targetPosition.z) - vector).normalized;
		float num = float.MaxValue;
		Transform[] sittingPositions = seatController.sittingPositions;
		foreach (Transform transform2 in sittingPositions)
		{
			if (seatController.IsSittingPositionAvailable(transform2))
			{
				float num2 = Vector2.Angle(new Vector2(transform2.forward.x, transform2.forward.z), normalized);
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	private static Transform GetClosestSittingPosition(SeatController seatController, Vector3 position)
	{
		Transform result = null;
		float num = float.MaxValue;
		Transform[] sittingPositions = seatController.sittingPositions;
		foreach (Transform transform in sittingPositions)
		{
			if (seatController.IsSittingPositionAvailable(transform))
			{
				float num2 = Vector3.SqrMagnitude(position - transform.position);
				if (num2 < num)
				{
					num = num2;
					result = transform;
				}
			}
		}
		return result;
	}
}
