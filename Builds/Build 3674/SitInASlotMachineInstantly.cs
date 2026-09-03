using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Buildings;
using UnityEngine;

[TaskCategory("Big Ambitions/Casino")]
public class SitInASlotMachineInstantly : Action
{
	private const int MinTimePlaying = 16;

	private const int MaxTimePlaying = 32;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	private ItemController _slotMachineChair;

	private SlotMachineController _slotMachineController;

	private Transform _playSpot;

	private bool _canMoveToSlotMachine;

	private Timestamp _stopPlayingTimestamp;

	private readonly CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	public override void OnAwake()
	{
		_characterRunAnimation.Init(sharedCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		_slotMachineChair = CasinoBusinessHelper.GetRandomMachineSlotChair();
		if (!(_slotMachineChair == null) && _slotMachineChair.parentItemController is SlotMachineController slotMachineController)
		{
			_slotMachineController = slotMachineController;
			Vector3 position = _slotMachineChair.transform.position;
			_canMoveToSlotMachine = sharedCustomer.Value.tpc.navmeshAgent.Warp(position);
			if (_canMoveToSlotMachine)
			{
				OnPlaySpotReached();
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_slotMachineChair == null || _slotMachineController == null || !_canMoveToSlotMachine)
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

	private void OnPlaySpotReached()
	{
		_slotMachineChair.Occupied = true;
		sharedCustomer.Value.tpc.SitOnChair(_slotMachineChair.SittingPosition);
		if (sharedCustomer.Value.isHoldingADrink)
		{
			_characterRunAnimation.StartRunningAnimation(AnimationType.Drink);
		}
		else
		{
			sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.UsingSlotMachine);
		}
		_slotMachineController.PlaySlotMachineSounds();
		_stopPlayingTimestamp = TimeHelper.Now();
		_stopPlayingTimestamp.AddMinutes(Random.Range(16, 32));
	}

	private void OnPlayingFinished()
	{
		_slotMachineController.StopSlotMachineSounds();
		sharedCustomer.Value.tpc.animator.SetBool(PermanentAnimationType.UsingSlotMachine, state: false);
		sharedCustomer.Value.tpc.Reset();
		_slotMachineChair.Occupied = false;
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
		_slotMachineController?.StopSlotMachineSounds();
		_slotMachineController = null;
	}
}
