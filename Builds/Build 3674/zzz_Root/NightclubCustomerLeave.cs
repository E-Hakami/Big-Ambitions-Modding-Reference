using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using UnityEngine;

[TaskCategory("Big Ambitions/Nightclub")]
public class NightclubCustomerLeave : Action
{
	[RequiredField]
	public SharedNightclubCustomer sharedNightclubCustomer;

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	private CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private CharacterRunAnimation _characterRunAnimation = new CharacterRunAnimation();

	private bool _hasStartedRotating;

	private bool _hasStartedAnimation;

	public override void OnAwake()
	{
		_characterMoveToPosition.Init(sharedNightclubCustomer.Value.tpc.navmeshAgent);
		_characterRotateTowards.Init(sharedNightclubCustomer.Value.tpc, base.Owner);
		_characterRunAnimation.Init(sharedNightclubCustomer.Value.tpc);
	}

	public override void OnStart()
	{
		sharedNightclubCustomer.Value.RemoveDrink();
		if (sharedNightclubCustomer.Value.hasCoat && !(sharedNightclubCustomer.Value.coatCheckController == null))
		{
			Vector3 position = sharedNightclubCustomer.Value.coatCheckController.pickingSpot.position;
			if (!_characterMoveToPosition.TryStartMovingToPosition(position))
			{
				sharedNightclubCustomer.Value.coatCheckController = null;
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!sharedNightclubCustomer.Value.hasCoat)
		{
			sharedNightclubCustomer.Value.Leave();
			return TaskStatus.Success;
		}
		if (sharedNightclubCustomer.Value.coatCheckController == null)
		{
			sharedNightclubCustomer.Value.PutACoatInTheArm();
			sharedNightclubCustomer.Value.Leave();
			return TaskStatus.Success;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnCoatCheckReached();
		}
		if (!_characterRotateTowards.HasFinishedRotating())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedAnimation)
		{
			OnRotatingFinished();
		}
		if (!_characterRunAnimation.IsAnimationFinished())
		{
			return TaskStatus.Running;
		}
		OnCoatCheckAnimationFinished();
		return TaskStatus.Success;
	}

	private void OnCoatCheckReached()
	{
		Vector3 employeePosition = sharedNightclubCustomer.Value.coatCheckController.GetEmployeePosition();
		_characterRotateTowards.StartRotatingTowards(employeePosition);
		_hasStartedRotating = true;
		_characterMoveToPosition.StopCheckingDestination();
	}

	private void OnRotatingFinished()
	{
		_characterRunAnimation.StartRunningAnimation(AnimationType.UsingProducer, 1.5f);
		_hasStartedAnimation = true;
	}

	private void OnCoatCheckAnimationFinished()
	{
		sharedNightclubCustomer.Value.PutACoatInTheArm();
		sharedNightclubCustomer.Value.coatCheckController.DecreaseStoredCoats();
		sharedNightclubCustomer.Value.Leave();
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
		_characterRotateTowards.StopRotating();
		_characterMoveToPosition.StopCheckingDestination();
		_hasStartedRotating = false;
		_hasStartedAnimation = false;
	}
}
