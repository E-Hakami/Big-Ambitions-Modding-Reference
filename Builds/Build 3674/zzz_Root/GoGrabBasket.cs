using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using UnityEngine;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class GoGrabBasket : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	private ItemController _shoppingBasketController;

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	private bool _canMove;

	private bool _hasStartedRunningAnimation;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedCustomer.Value.tpc.navmeshAgent);
		_characterRunAnimation.Init(sharedCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		_shoppingBasketController = sharedCustomer.Value.FindShoppingBasket();
		if (!(_shoppingBasketController == null))
		{
			_canMove = _characterMoveToPosition.TryStartMovingToPosition(GetTargetPosition());
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (_shoppingBasketController == null || !_canMove)
		{
			return TaskStatus.Failure;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRunningAnimation)
		{
			OnShoppingBasketReached();
		}
		if (!_characterRunAnimation.IsAnimationFinished())
		{
			return TaskStatus.Running;
		}
		sharedCustomer.Value.SetBasket(_shoppingBasketController);
		return TaskStatus.Success;
	}

	private void OnShoppingBasketReached()
	{
		_characterRunAnimation.StartRunningAnimation(AnimationType.UsingProducer, 1.5f);
		_hasStartedRunningAnimation = true;
		_characterMoveToPosition.StopCheckingDestination();
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
		_hasStartedRunningAnimation = false;
		_characterMoveToPosition.StopCheckingDestination();
	}

	private Vector3 GetTargetPosition()
	{
		int index = Random.Range(0, _shoppingBasketController.GetNavMeshTargetCount());
		return _shoppingBasketController.GetNavMeshTargetPosition(index);
	}
}
