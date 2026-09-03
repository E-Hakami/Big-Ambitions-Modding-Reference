using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using UnityEngine;

[TaskCategory("Big Ambitions/Nightclub")]
public class DrinkForAWhileIfHasADrink : Action
{
	private const int MinMinutesDrinking = 10;

	private const int MaxMinutesDrinking = 30;

	private const int MinMinutesBetweenSips = 5;

	private const int MaxMinutesBetweenSips = 10;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	public bool returnSuccessIfNoDrink;

	private bool _isHoldingADrink;

	private ThirdPersonCharacter _tpc;

	private Timestamp _stopDrinkingTimeStamp;

	private Timestamp _nextSipTimeStamp;

	public override void OnStart()
	{
		_isHoldingADrink = sharedCustomer.Value.isHoldingADrink;
		if (_isHoldingADrink)
		{
			_tpc = sharedCustomer.Value.tpc;
			_stopDrinkingTimeStamp = TimeHelper.Now();
			_stopDrinkingTimeStamp.AddMinutes(Random.Range(10, 30));
			TakeASip();
		}
	}

	private void TakeASip()
	{
		_tpc.animator.RunAnimationLength(AnimationType.Drink);
		_nextSipTimeStamp = TimeHelper.Now();
		_nextSipTimeStamp.AddMinutes(Random.Range(5, 10));
	}

	public override TaskStatus OnUpdate()
	{
		if (!_isHoldingADrink)
		{
			if (!returnSuccessIfNoDrink)
			{
				return TaskStatus.Failure;
			}
			return TaskStatus.Success;
		}
		if (_stopDrinkingTimeStamp.IsInThePast())
		{
			sharedCustomer.Value.RemoveDrink();
			return TaskStatus.Success;
		}
		if (_nextSipTimeStamp.IsInThePast())
		{
			TakeASip();
		}
		return TaskStatus.Running;
	}
}
