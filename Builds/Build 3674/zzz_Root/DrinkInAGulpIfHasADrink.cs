using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using UnityEngine;

[TaskCategory("Big Ambitions/Nightclub")]
public class DrinkInAGulpIfHasADrink : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	private bool _isHoldingADrink;

	private ThirdPersonCharacter _tpc;

	private float _animationDuration;

	private float _startTime;

	public override void OnStart()
	{
		_isHoldingADrink = sharedCustomer.Value.isHoldingADrink;
		if (_isHoldingADrink)
		{
			_tpc = sharedCustomer.Value.tpc;
			_animationDuration = _tpc.animator.RunAnimationLength(AnimationType.Drink);
			_startTime = Time.time;
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_isHoldingADrink)
		{
			return TaskStatus.Success;
		}
		if (_startTime + _animationDuration < Time.time)
		{
			sharedCustomer.Value.RemoveDrink();
			return TaskStatus.Success;
		}
		return TaskStatus.Running;
	}
}
