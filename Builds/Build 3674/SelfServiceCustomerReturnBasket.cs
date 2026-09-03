using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class SelfServiceCustomerReturnBasket : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	private ItemController _shoppingBasketController;

	private bool _hasStartedRotation;

	private bool _canMove;

	private readonly CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private readonly CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (!sharedCustomer.Value.hasABasket)
		{
			return;
		}
		_shoppingBasketController = sharedCustomer.Value.FindShoppingBasket();
		if (!(_shoppingBasketController == null))
		{
			_canMove = _characterMoveToPosition.TryStartMovingToPosition(GetTargetPosition());
			if (!_canMove)
			{
				RemoveBasket();
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!sharedCustomer.Value.hasABasket || _shoppingBasketController == null || !_canMove)
		{
			return TaskStatus.Success;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotation)
		{
			OnShoppingBasketReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		RemoveBasket();
		return TaskStatus.Success;
	}

	private Vector3 GetTargetPosition()
	{
		int index = Random.Range(0, _shoppingBasketController.GetNavMeshTargetCount());
		return _shoppingBasketController.GetNavMeshTargetPosition(index);
	}

	private void RemoveBasket()
	{
		sharedCustomer.Value.tpc.SetHandContent(null);
	}

	private void OnShoppingBasketReached()
	{
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StartRotatingTowards(_shoppingBasketController.transform.position);
		_hasStartedRotation = true;
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
		_hasStartedRotation = false;
		_characterMoveToPosition.StopCheckingDestination();
		_characterRotateTowards.StopRotating();
	}
}
