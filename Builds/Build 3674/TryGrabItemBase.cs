using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.SoundSystem;
using UnityEngine;

public abstract class TryGrabItemBase : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedItemController sharedItemController;

	[RequiredField]
	public SharedVector3 sharedTargetPosition;

	[RequiredField]
	public SharedOrderEntry sharedOrderEntry;

	protected bool hasStartedRotation;

	protected bool complained;

	protected bool hasStartedAnimation;

	protected bool hasStartedWaiting;

	protected float stopWaitingTime;

	protected readonly ExpressionDataContainer expressionDataContainer = new ExpressionDataContainer();

	protected readonly CharacterMoveToPosition characterMoveToPosition = new CharacterMoveToPosition();

	protected readonly CharacterShowEmojiExpression characterShowEmojiExpression = new CharacterShowEmojiExpression();

	protected readonly CharacterRotateTowards characterRotateTowards = new CharacterRotateTowards();

	protected readonly CharacterRunAnimation characterRunAnimation = new CharacterRunAnimation();

	public override void OnAwake()
	{
		characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
		characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
		characterRunAnimation.Init(sharedCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		if (!(sharedItemController.Value == null))
		{
			characterMoveToPosition.TryStartMovingToPosition(sharedTargetPosition.Value);
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (sharedItemController.Value == null)
		{
			return TaskStatus.Success;
		}
		if (!characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!hasStartedRotation)
		{
			OnItemReached();
		}
		if (!characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!hasStartedAnimation)
		{
			OnRotationFinished();
		}
		if (!characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		if (complained)
		{
			return TaskStatus.Success;
		}
		if (!characterRunAnimation.IsAnimationFinished())
		{
			return TaskStatus.Running;
		}
		if (!hasStartedWaiting)
		{
			OnAnimationFinished();
		}
		if (!(stopWaitingTime > Time.time))
		{
			return TaskStatus.Success;
		}
		return TaskStatus.Running;
	}

	protected virtual void OnItemReached()
	{
		characterMoveToPosition.StopCheckingDestination();
		characterRotateTowards.StartRotatingTowards(sharedItemController.Value.transform.position);
		hasStartedRotation = true;
	}

	protected abstract void OnRotationFinished();

	protected abstract void OnAnimationFinished();

	protected void PlayCharacterGrabSound()
	{
		SoundType type = ((sharedCustomer.Value.citizenData.Gender == Gender.Male) ? SoundType.NpcMaleProducerInteraction : SoundType.NpcFemaleProducerInteraction);
		InstanceBehavior<SfxManager>.Instance.PlayAudio(type, transform.position);
	}

	protected void Complain(CharacterEmojiName characterEmojiName)
	{
		characterShowEmojiExpression.StartShowingEmoji(characterEmojiName, 4f, expressionDataContainer);
		complained = true;
	}

	public override void OnEnd()
	{
		ResetTask();
	}

	public override void OnBehaviorComplete()
	{
		ResetTask();
	}

	protected virtual void ResetTask()
	{
		characterMoveToPosition.StopCheckingDestination();
		characterShowEmojiExpression.StopShowingEmoji();
		characterRotateTowards.StopRotating();
		hasStartedRotation = false;
		complained = false;
		hasStartedAnimation = false;
		hasStartedWaiting = false;
	}
}
