using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using UnityEngine;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class SelfServiceCustomerTryFindItem : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedItemController sharedItemController;

	[RequiredField]
	public SharedVector3 sharedTargetPosition;

	[RequiredField]
	public SharedOrderEntry sharedOrderEntry;

	public bool useNearestNavMeshTarget;

	private bool _navMeshTargetFound;

	private bool _hasStartedShowingExpression;

	private ExpressionDataContainer _expressionDataContainer = new ExpressionDataContainer();

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (sharedOrderEntry.Value.processed)
		{
			return;
		}
		Item byName = ItemsGetter.GetByName(sharedOrderEntry.Value.itemName);
		if (byName == null || (byName.type & ItemType.ServiceProduct) != 0)
		{
			return;
		}
		sharedItemController.Value = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(sharedOrderEntry.Value.itemName, sharedTargetPosition.Value);
		if (sharedItemController.Value != null)
		{
			if (!InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer && sharedItemController.Value is ShelfController { fillState: <=0.0 })
			{
				sharedItemController.Value = null;
				return;
			}
			if (useNearestNavMeshTarget)
			{
				Vector3 position = sharedCustomer.Value.tpc.transform.position;
				sharedTargetPosition.Value = sharedItemController.Value.GetClosestNavMeshTargetPositionStraightLine(position);
				_navMeshTargetFound = sharedTargetPosition.Value != Vector3.zero;
			}
			else
			{
				_navMeshTargetFound = sharedItemController.Value.TryGetRandomAvailableNavMeshTargetPosition(out var navMeshPosition);
				if (_navMeshTargetFound)
				{
					sharedTargetPosition.Value = navMeshPosition;
				}
			}
			if (!_navMeshTargetFound)
			{
				StartRandomMovement();
			}
		}
		else
		{
			StartRandomMovement();
		}
	}

	private void StartRandomMovement()
	{
		Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition();
		if (!_characterMoveToPosition.TryStartMovingToPosition(randomPosition))
		{
			_characterMoveToPosition.StopCheckingDestination();
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (sharedOrderEntry.Value.processed)
		{
			return TaskStatus.Failure;
		}
		Item byName = ItemsGetter.GetByName(sharedOrderEntry.Value.itemName);
		if (byName == null || (byName.type & ItemType.ServiceProduct) != 0)
		{
			return TaskStatus.Failure;
		}
		if (sharedItemController.Value != null && _navMeshTargetFound)
		{
			return TaskStatus.Success;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedShowingExpression)
		{
			OnRandomMovementFinished();
		}
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Failure;
	}

	private void OnRandomMovementFinished()
	{
		_characterMoveToPosition.StopCheckingDestination();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			CharacterEmojiName emojiName = ((sharedItemController.Value == null) ? CharacterEmojiName.CustomerCantFindItem : CharacterEmojiName.CustomerCantReachItem);
			_expressionDataContainer.itemName = sharedOrderEntry.Value.itemName;
			_characterShowEmojiExpression.StartShowingEmoji(emojiName, 4f, _expressionDataContainer);
			_hasStartedShowingExpression = true;
		}
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
		_hasStartedShowingExpression = false;
	}
}
