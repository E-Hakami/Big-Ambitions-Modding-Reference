using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class RandomMovementInRadius : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedFloat randomRadius = 5f;

	public SharedFloat offset = 0f;

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private bool _canMove;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
	}

	public override void OnStart()
	{
		Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition(randomRadius.Value, offset.Value);
		_canMove = _characterMoveToPosition.TryStartMovingToPosition(randomPosition);
	}

	public override TaskStatus OnUpdate()
	{
		if (!_canMove)
		{
			return TaskStatus.Success;
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
