using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Buildings;
using Buildings.BuildingTypes.Special;
using UnityEngine;

[TaskCategory("Big Ambitions/Casino")]
public class GoPlayOnAGameSpot : Action
{
	private const int MinTimePlaying = 16;

	private const int MaxTimePlaying = 32;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedCasinoGameType sharedCasinoGameType;

	[RequiredField]
	public SharedPermanentAnimationType sharedPermanentAnimationType;

	private PlaySpotsManager _gamePlaySpotsManager;

	private Transform _playSpot;

	private bool _canMoveToPlaySpot;

	private bool _hasStartedRotating;

	private bool _hasStartedPlaying;

	private Timestamp _stopPlayingTimestamp;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private readonly CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
		_characterRunAnimation.Init(sharedCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		_gamePlaySpotsManager = CasinoBusinessHelper.GetRandomCasinoGamePlaySpotsManager(sharedCasinoGameType.Value);
		if (!(_gamePlaySpotsManager == null))
		{
			_playSpot = _gamePlaySpotsManager.GetFreeSpot();
			_gamePlaySpotsManager.SetPlaySpotStatus(_playSpot, PlaySpotStatus.Reserved);
			Vector3 position = _playSpot.position;
			_canMoveToPlaySpot = _characterMoveToPosition.TryStartMovingToPosition(position);
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_playSpot == null || !_canMoveToPlaySpot)
		{
			return TaskStatus.Failure;
		}
		if (!_hasStartedRotating && _gamePlaySpotsManager.IsSpotOccupied(_playSpot))
		{
			return TaskStatus.Failure;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnPlaySpotReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedPlaying)
		{
			OnRotatingFinished();
		}
		if (!_stopPlayingTimestamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		OnPlayingFinished();
		return TaskStatus.Success;
	}

	private void OnPlaySpotReached()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_gamePlaySpotsManager.SetPlaySpotStatus(_playSpot, PlaySpotStatus.Occupied);
		Vector3 target = _playSpot.position + _playSpot.forward;
		_characterRotateTowards.StartRotatingTowards(target);
		_hasStartedRotating = true;
	}

	private void OnRotatingFinished()
	{
		if (sharedCustomer.Value.isHoldingADrink)
		{
			_characterRunAnimation.StartRunningAnimation(AnimationType.Drink);
		}
		else
		{
			sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value);
		}
		_stopPlayingTimestamp = TimeHelper.Now();
		_stopPlayingTimestamp.AddMinutes(Random.Range(16, 32));
		_hasStartedPlaying = true;
	}

	private void OnPlayingFinished()
	{
		sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value, state: false);
		_gamePlaySpotsManager.SetPlaySpotStatus(_playSpot, PlaySpotStatus.Free);
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
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StopRotating();
		_hasStartedRotating = false;
		_playSpot = null;
	}
}
