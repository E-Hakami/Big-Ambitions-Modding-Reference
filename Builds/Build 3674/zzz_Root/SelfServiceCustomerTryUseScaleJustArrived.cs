using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Extensions;
using UnityEngine;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class SelfServiceCustomerTryUseScaleJustArrived : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedBool sharedScaleUsed;

	private ItemController _scaleController;

	private bool _canReachScale;

	private bool _hasStartedComplaining;

	private bool _hasStartedRotating;

	private bool _hasStartedRunningAnimation;

	private ExpressionDataContainer _expressionDataContainer = new ExpressionDataContainer();

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private readonly CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	private readonly List<ItemController> _availableScales = new List<ItemController>();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
		_characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
		_characterRunAnimation.Init(sharedCustomer.Value.tpc);
		_expressionDataContainer.itemName = ItemsGetter.GetRandomByTag(TagRef.Itemtag.isweighingscale);
	}

	public override void OnStart()
	{
		sharedScaleUsed.Value = false;
		_scaleController = FindRandomScale();
		if (_scaleController == null)
		{
			return;
		}
		if (_scaleController.TryGetRandomAvailableRealNavMeshTargetPosition(out var navMeshPosition))
		{
			if (_characterMoveToPosition.TryStartMovingToPosition(navMeshPosition))
			{
				_canReachScale = true;
				return;
			}
			Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition();
			if (!_characterMoveToPosition.TryStartMovingToPosition(randomPosition))
			{
				_characterMoveToPosition.StopCheckingDestination();
			}
		}
		_canReachScale = false;
	}

	public override TaskStatus OnUpdate()
	{
		if (_scaleController == null)
		{
			return TaskStatus.Success;
		}
		if (!_canReachScale)
		{
			return CantReachScaleUpdate();
		}
		return CanReachScaleUpdate();
	}

	private TaskStatus CantReachScaleUpdate()
	{
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedComplaining)
		{
			OnRandomMovementFinished();
		}
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Success;
	}

	private TaskStatus CanReachScaleUpdate()
	{
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnScaleReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRunningAnimation)
		{
			OnRotationFinished();
		}
		if (!_characterRunAnimation.IsAnimationFinished())
		{
			return TaskStatus.Running;
		}
		BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, _scaleController.ItemInstance);
		sharedScaleUsed.Value = true;
		return TaskStatus.Success;
	}

	private ItemController FindRandomScale()
	{
		_availableScales.Clear();
		for (int i = 0; i < InstanceBehavior<BuildingManager>.Instance.allItemControllers.Count; i++)
		{
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.allItemControllers[i];
			if (itemController.Item.HasTag(TagRef.Itemtag.isweighingscale))
			{
				_availableScales.Add(itemController);
			}
		}
		return _availableScales.GetRandom();
	}

	private void OnScaleReached()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StartRotatingTowards(_scaleController.transform.position);
		_hasStartedRotating = true;
	}

	private void OnRotationFinished()
	{
		_characterRunAnimation.StartRunningAnimation(AnimationType.UsingProducer, 1.5f);
		_hasStartedRunningAnimation = true;
	}

	private void OnRandomMovementFinished()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterShowEmojiExpression.StartShowingEmoji(CharacterEmojiName.CustomerCantFindScale, 4f, _expressionDataContainer);
		_hasStartedComplaining = true;
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
		_canReachScale = false;
		_hasStartedComplaining = false;
		_hasStartedRotating = false;
		_hasStartedRunningAnimation = false;
		_characterMoveToPosition.StopCheckingDestination();
		_characterShowEmojiExpression.StopShowingEmoji();
		_characterRotateTowards.StopRotating();
	}
}
