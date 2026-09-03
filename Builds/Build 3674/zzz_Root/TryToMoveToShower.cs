using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions/Gym")]
public class TryToMoveToShower : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedItemController sharedShowerController;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
	}

	public override void OnStart()
	{
		if (!(sharedShowerController.Value == null))
		{
			Vector3 position = sharedShowerController.Value.transform.position;
			if (sharedCustomer.Value.customerTimeState == CustomerTimeState.AlmostLeaving)
			{
				sharedCustomer.Value.tpc.navmeshAgent.Warp(position);
				sharedCustomer.Value.customerTimeState = CustomerTimeState.JustArrived;
			}
			else if (!_characterMoveToPosition.TryStartMovingToPosition(position))
			{
				sharedCustomer.Value.Leave();
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (sharedShowerController.Value == null)
		{
			return TaskStatus.Failure;
		}
		if (sharedShowerController.Value.Occupied)
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
