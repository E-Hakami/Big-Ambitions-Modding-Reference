using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using UnityEngine;

[TaskCategory("Big Ambitions")]
[TaskIcon("{SkinColor}WaitIcon.png")]
public class WaitMinutes : Action
{
	[UnityEngine.Tooltip("The amount of minutes to wait")]
	public SharedFloat waitMinutes = 1f;

	[UnityEngine.Tooltip("Should the wait be randomized?")]
	public SharedBool randomWait = false;

	[UnityEngine.Tooltip("The minimum wait minutes if random wait is enabled")]
	public SharedFloat randomWaitMin = 1f;

	[UnityEngine.Tooltip("The maximum wait minutes if random wait is enabled")]
	public SharedFloat randomWaitMax = 1f;

	private Timestamp _stopWaitingTimeStamp;

	public override void OnStart()
	{
		_stopWaitingTimeStamp = TimeHelper.Now();
		_stopWaitingTimeStamp.AddMinutes(randomWait.Value ? Random.Range(randomWaitMin.Value, randomWaitMax.Value) : waitMinutes.Value);
	}

	public override TaskStatus OnUpdate()
	{
		if (!_stopWaitingTimeStamp.IsInThePast())
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Success;
	}
}
