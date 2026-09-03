using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using Entities;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class TryUseChangingRoom : Action
{
	private const int MinMinutesWaiting = 5;

	private const int MaxMinutesWaiting = 10;

	private const float Chance = 0.5f;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private ChangingRoomController _changingRoomItemController;

	private Timestamp _waitUntil;

	private bool _hasStartedWaiting;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (Random.value > 0.5f || !IsClothingStore() || !HasBoughtAnyItem())
		{
			return;
		}
		Reset();
		_changingRoomItemController = GetChangingRoom();
		if (!(_changingRoomItemController == null))
		{
			_changingRoomItemController.Occupied = true;
			Vector3 position = _changingRoomItemController.npcSpot.position;
			if (sharedCustomer.Value.customerTimeState == CustomerTimeState.AlreadyInAction)
			{
				sharedCustomer.Value.tpc.navmeshAgent.Warp(position);
				sharedCustomer.Value.customerTimeState = CustomerTimeState.JustArrived;
			}
			else if (!_characterMoveToPosition.TryStartMovingToPosition(position))
			{
				_changingRoomItemController = null;
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_changingRoomItemController == null)
		{
			return TaskStatus.Success;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedWaiting)
		{
			OnReachedChangingRoom();
		}
		if (!_waitUntil.IsInThePast())
		{
			return TaskStatus.Running;
		}
		OnWaitingFinished();
		return TaskStatus.Success;
	}

	private void OnReachedChangingRoom()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_changingRoomItemController.CloseCurtains();
		_waitUntil = TimeHelper.Now();
		_waitUntil.AddMinutes(Random.Range(5, 10));
		_hasStartedWaiting = true;
	}

	private void OnWaitingFinished()
	{
		_changingRoomItemController.OpenCurtains();
		_changingRoomItemController.Occupied = false;
		_changingRoomItemController = null;
	}

	private ChangingRoomController GetChangingRoom()
	{
		ChangingRoomController result = null;
		float num = float.MaxValue;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (!allItemController || allItemController.Occupied || !(allItemController is ChangingRoomController changingRoomController))
			{
				continue;
			}
			PlayerItemPurchaserSettings playerItemPurchaserSettings = changingRoomController.playerItemPurchaserSettings;
			if (playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled)
			{
				float sqrMagnitude = (allItemController.transform.position - sharedCustomer.Value.tpc.transform.position).sqrMagnitude;
				if (!(sqrMagnitude > num))
				{
					num = sqrMagnitude;
					result = changingRoomController;
				}
			}
		}
		return result;
	}

	private bool HasBoughtAnyItem()
	{
		return sharedCustomer.Value.order.entries.Exists((OrderEntry x) => x.available && x.priceAccceptable);
	}

	private static bool IsClothingStore()
	{
		return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == "ba:businesstype_clothingstore";
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private void Reset()
	{
		if (_changingRoomItemController != null)
		{
			OnWaitingFinished();
		}
		_characterMoveToPosition.StopCheckingDestination();
		_characterShowEmojiExpression.StopShowingEmoji();
		_hasStartedWaiting = false;
	}
}
