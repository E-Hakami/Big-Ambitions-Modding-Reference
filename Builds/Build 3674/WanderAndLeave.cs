using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class WanderAndLeave : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedFloat randomRadius = 5f;

	public SharedFloat offset = 0f;

	public bool showExpression;

	public SharedCharacterEmojiName sharedCharacterEmojiName;

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private bool _canMove;

	private bool _hasStartedLeaving;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
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
		if (!_hasStartedLeaving)
		{
			OnDestinationReached();
		}
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		sharedCustomer.Value.Leave();
		return TaskStatus.Success;
	}

	private void OnDestinationReached()
	{
		if (showExpression)
		{
			_characterShowEmojiExpression.StartShowingEmoji(sharedCharacterEmojiName.Value);
		}
		_characterMoveToPosition.StopCheckingDestination();
		_hasStartedLeaving = true;
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
		_characterShowEmojiExpression.StopShowingEmoji();
		_hasStartedLeaving = false;
	}
}
