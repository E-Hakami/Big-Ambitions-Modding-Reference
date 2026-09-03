using System;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.PlacementSystem;
using Buildings.Retail.Businesses.CinemaTheater;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.AI;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class WatchShow : BehaviorDesigner.Runtime.Tasks.Action
{
	private const float LingerDurationMax = 10f;

	private const float PathMaxSampleRadius = 10f;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	private Transform _sittingPosition;

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private bool _foundPosition;

	private int _startHour;

	private int _leaveHour;

	private float _lingerTimer;

	private bool _pendingRescan;

	private bool _isStanding;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
	}

	public override void OnStart()
	{
		_sittingPosition = CinemaTheaterHelper.TryReserveSittingPosition(sharedCustomer.Value.tpc);
		_foundPosition = StartMoveToSittingPosition(IsAlreadyInAction());
		PlacementSystem.onPlacementModeEnd = (System.Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new System.Action(OnPlacementModeEnd));
		InteriorDesignerUI.OnInteriorDesignerToggle.AddListener(OnInteriorDesignerToggle);
	}

	private void OnPlacementModeEnd()
	{
		_pendingRescan = true;
	}

	private void OnInteriorDesignerToggle(bool isInInteriorDesigner)
	{
		if (!isInInteriorDesigner)
		{
			_pendingRescan = true;
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_foundPosition)
		{
			return TaskStatus.Failure;
		}
		if (_pendingRescan)
		{
			RescanForChair();
		}
		if (_lingerTimer <= 0f)
		{
			if (!IsAlreadyInAction())
			{
				if (!_characterMoveToPosition.HasReachedDestination())
				{
					return TaskStatus.Running;
				}
				OnReachedDestination();
			}
			_lingerTimer = UnityEngine.Random.value * 10f;
			_startHour = TimeHelper.CurrentHour;
			_leaveHour = (_sittingPosition ? CinemaTheaterHelper.GetDutyCycleEndHour(_sittingPosition) : (_startHour + 1));
		}
		if (_lingerTimer > 0f)
		{
			ThirdPersonCharacter tpc = sharedCustomer.Value.tpc;
			if (!tpc.isSittingOn)
			{
				if (!TheaterStage.ActiveInstance)
				{
					return TaskStatus.Failure;
				}
				tpc.RotateTowards(TheaterStage.ActiveInstance.transform.position);
			}
			if (TimeHelper.IsInHourRange(_startHour, _leaveHour))
			{
				return TaskStatus.Running;
			}
			_lingerTimer -= Time.deltaTime;
			if (_lingerTimer > 0f)
			{
				return TaskStatus.Running;
			}
		}
		return TaskStatus.Success;
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
		_sittingPosition = null;
		_characterMoveToPosition.StopCheckingDestination();
		_lingerTimer = 0f;
		_pendingRescan = false;
		_isStanding = false;
		ThirdPersonCharacter tpc = sharedCustomer.Value.tpc;
		tpc.Reset();
		CinemaTheaterHelper.ReleaseSittingPosition(tpc);
		PlacementSystem.onPlacementModeEnd = (System.Action)Delegate.Remove(PlacementSystem.onPlacementModeEnd, new System.Action(OnPlacementModeEnd));
		InteriorDesignerUI.OnInteriorDesignerToggle.RemoveListener(OnInteriorDesignerToggle);
	}

	private bool IsAlreadyInAction()
	{
		return sharedCustomer.Value.customerTimeState == CustomerTimeState.AlreadyInAction;
	}

	private bool StartMoveToSittingPosition(bool instant)
	{
		bool isStanding = _isStanding;
		_isStanding = false;
		ThirdPersonCharacter tpc = sharedCustomer.Value.tpc;
		if (!tpc.navmeshAgent.isOnNavMesh)
		{
			return false;
		}
		if ((bool)_sittingPosition)
		{
			_characterMoveToPosition.StopCheckingDestination();
			if (instant)
			{
				OnReachedDestination();
				return true;
			}
			bool flag = false;
			for (float num = 0f; num <= 10f; num++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * num;
				if (NavMesh.SamplePosition(_sittingPosition.position + new Vector3(vector.x, 0f, vector.y), out var hit, 2f, -1) && _characterMoveToPosition.TryStartMovingToPosition(hit.position))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				OnReachedDestination();
			}
			return true;
		}
		_sittingPosition = null;
		if (!CinemaTheaterHelper.TryGetStandingAreaPosition(out var position))
		{
			return false;
		}
		if (instant)
		{
			_isStanding = true;
			tpc.navmeshAgent.Warp(position);
			OnReachedDestination();
			return true;
		}
		if (isStanding)
		{
			_isStanding = true;
			return true;
		}
		if (!_characterMoveToPosition.TryStartMovingToPosition(position))
		{
			return false;
		}
		_isStanding = true;
		return true;
	}

	private void OnReachedDestination()
	{
		ThirdPersonCharacter tpc = sharedCustomer.Value.tpc;
		if ((bool)_sittingPosition && CinemaTheaterHelper.IsSittingPositionReservedBy(_sittingPosition, tpc))
		{
			tpc.SitOnChair(_sittingPosition);
			return;
		}
		CinemaTheaterHelper.ReleaseSittingPosition(tpc);
		tpc.Reset();
		if ((object)_sittingPosition != null)
		{
			_sittingPosition = null;
			_pendingRescan = true;
		}
	}

	private void RescanForChair()
	{
		_pendingRescan = false;
		ThirdPersonCharacter tpc = sharedCustomer.Value.tpc;
		if ((bool)tpc.isSittingOn)
		{
			_sittingPosition = tpc.isSittingOn;
			if (CinemaTheaterHelper.EnsureSittingPositionReserved(tpc.isSittingOn, tpc))
			{
				return;
			}
			_sittingPosition = null;
		}
		if (!_sittingPosition || !CinemaTheaterHelper.IsSittingPositionReservedBy(_sittingPosition, tpc))
		{
			CinemaTheaterHelper.ReleaseSittingPosition(tpc);
			tpc.Reset();
			if (tpc.navmeshAgent.isOnNavMesh)
			{
				tpc.navmeshAgent.ResetPath();
			}
			_sittingPosition = CinemaTheaterHelper.TryReserveSittingPosition(tpc);
			_foundPosition = StartMoveToSittingPosition(instant: false);
			_lingerTimer = 0f;
		}
	}
}
