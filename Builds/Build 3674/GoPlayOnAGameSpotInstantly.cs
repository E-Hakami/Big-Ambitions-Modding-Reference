using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using Buildings;
using Buildings.BuildingTypes.Special;
using UnityEngine;

[TaskCategory("Big Ambitions/Casino")]
public class GoPlayOnAGameSpotInstantly : Action
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

	private Timestamp _stopPlayingTimestamp;

	public override void OnStart()
	{
		_gamePlaySpotsManager = CasinoBusinessHelper.GetRandomCasinoGamePlaySpotsManager(sharedCasinoGameType.Value);
		if (!(_gamePlaySpotsManager == null))
		{
			_playSpot = _gamePlaySpotsManager.GetFreeSpot();
			_gamePlaySpotsManager.SetPlaySpotStatus(_playSpot, PlaySpotStatus.Occupied);
			Vector3 position = _playSpot.position;
			_canMoveToPlaySpot = sharedCustomer.Value.tpc.navmeshAgent.Warp(position);
			if (_canMoveToPlaySpot)
			{
				sharedCustomer.Value.tpc.transform.rotation = Quaternion.LookRotation(_playSpot.forward);
				sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value);
				_stopPlayingTimestamp = TimeHelper.Now();
				_stopPlayingTimestamp.AddMinutes(Random.Range(16, 32));
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_playSpot == null || !_canMoveToPlaySpot)
		{
			return TaskStatus.Failure;
		}
		if (!_stopPlayingTimestamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		OnPlayingFinished();
		return TaskStatus.Success;
	}

	private void OnPlayingFinished()
	{
		sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value, state: false);
		_gamePlaySpotsManager.SetPlaySpotStatus(_playSpot, PlaySpotStatus.Free);
	}
}
