using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions/Gym")]
public class TryToMoveToWorkoutMachine : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedItemController sharedWorkoutMachineController;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private bool _canMoveToWorkoutMachine;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
	}

	public override void OnStart()
	{
		if (!(sharedWorkoutMachineController.Value == null))
		{
			Vector3 position = sharedWorkoutMachineController.Value.transform.position;
			if (sharedCustomer.Value.customerTimeState == CustomerTimeState.RecentlyArrived)
			{
				sharedCustomer.Value.tpc.navmeshAgent.Warp(position);
				sharedCustomer.Value.customerTimeState = CustomerTimeState.JustArrived;
			}
			else
			{
				_canMoveToWorkoutMachine = _characterMoveToPosition.TryStartMovingToPosition(position);
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (sharedWorkoutMachineController.Value == null)
		{
			return TaskStatus.Failure;
		}
		if (sharedWorkoutMachineController.Value.Occupied)
		{
			return TaskStatus.Failure;
		}
		if (!_canMoveToWorkoutMachine)
		{
			return TaskStatus.Failure;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Success;
	}

	public override void OnEnd()
	{
		_characterMoveToPosition.StopCheckingDestination();
	}

	public override void OnBehaviorComplete()
	{
		_characterMoveToPosition.StopCheckingDestination();
	}
}
