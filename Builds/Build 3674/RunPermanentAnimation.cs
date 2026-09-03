using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using UnityEngine;

public class RunPermanentAnimation : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public SharedPermanentAnimationType sharedPermanentAnimationType;

	[UnityEngine.Tooltip("The amount of minutes to run the animation. Set it to 0 to run it indefinitely.")]
	public SharedFloat waitMinutes = 0f;

	[UnityEngine.Tooltip("Should the wait be randomized?")]
	public SharedBool randomWait = false;

	[UnityEngine.Tooltip("The minimum wait minutes if random wait is enabled")]
	public SharedFloat randomWaitMin = 1f;

	[UnityEngine.Tooltip("The maximum wait minutes if random wait is enabled")]
	public SharedFloat randomWaitMax = 1f;

	private Timestamp _stopWaitingTimeStamp;

	public override void OnStart()
	{
		if (BaseHuman.GetHandObjectNameFromPermanentAnimationType(sharedPermanentAnimationType.Value) != null)
		{
			sharedCustomer.Value.tpc.AddHandObject(BaseHuman.GetHandObjectNameFromPermanentAnimationType(sharedPermanentAnimationType.Value));
		}
		sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value);
		if (waitMinutes.Value != 0f || randomWait.Value)
		{
			_stopWaitingTimeStamp = TimeHelper.Now();
			_stopWaitingTimeStamp.AddMinutes(randomWait.Value ? Random.Range(randomWaitMin.Value, randomWaitMax.Value) : waitMinutes.Value);
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (waitMinutes.Value == 0f && !randomWait.Value)
		{
			return TaskStatus.Success;
		}
		if (!_stopWaitingTimeStamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		if (BaseHuman.GetHandObjectNameFromPermanentAnimationType(sharedPermanentAnimationType.Value) != null)
		{
			sharedCustomer.Value.tpc.RemoveHandObject();
		}
		sharedCustomer.Value.tpc.animator.SetBool(sharedPermanentAnimationType.Value, state: false);
		return TaskStatus.Success;
	}
}
