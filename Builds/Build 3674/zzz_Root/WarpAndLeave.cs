using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class WarpAndLeave : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedFloat randomRadius = 5f;

	public SharedFloat offset = 0f;

	public bool showExpression;

	public SharedCharacterEmojiName sharedCharacterEmojiName;

	private CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	public override void OnAwake()
	{
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition(randomRadius.Value, offset.Value);
		sharedCustomer.Value.tpc.navmeshAgent.Warp(randomPosition);
		if (showExpression)
		{
			_characterShowEmojiExpression.StartShowingEmoji(sharedCharacterEmojiName.Value);
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		sharedCustomer.Value.Leave();
		return TaskStatus.Success;
	}

	public override void OnEnd()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
	}

	public override void OnBehaviorComplete()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
	}
}
