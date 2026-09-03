using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using Dancing;
using UnityEngine;

[TaskCategory("Big Ambitions/Nightclub")]
public class RunShyDanceAnimation : Action
{
	[SerializeField]
	private int minDancingMinutes = 10;

	[SerializeField]
	private int maxDancingMinutes = 30;

	[SharedRequired]
	public SharedNightclubCustomer nightclubCustomer;

	private Timestamp _stopDancingTimestamp;

	public override void OnStart()
	{
		nightclubCustomer.Value.tpc.SetDance(DanceType.Dance5);
		_stopDancingTimestamp = TimeHelper.Now();
		_stopDancingTimestamp.AddMinutes(Random.Range(minDancingMinutes, maxDancingMinutes));
	}

	public override TaskStatus OnUpdate()
	{
		if (_stopDancingTimestamp.IsInThePast())
		{
			nightclubCustomer.Value.tpc.StopDancing();
			return TaskStatus.Success;
		}
		return TaskStatus.Running;
	}
}
