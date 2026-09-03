using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using Buildings;
using Dancing;
using Extensions;
using UnityEngine;

[TaskCategory("Big Ambitions/Nightclub")]
public class NightclubDanceInAction : Action
{
	[RequiredField]
	public SharedNightclubCustomer sharedNightclubCustomer;

	private DanceSpot _danceSpot;

	private Timestamp _stopDancingTimestamp;

	public override void OnStart()
	{
		Vector3 dancingPosition = GetDancingPosition();
		sharedNightclubCustomer.Value.tpc.navmeshAgent.Warp(dancingPosition);
		LookToDJIfThereIsOne();
		StartDancing();
		SetDancingStopTime();
	}

	public override TaskStatus OnUpdate()
	{
		if (_stopDancingTimestamp.IsInThePast())
		{
			sharedNightclubCustomer.Value.tpc.StopDancing();
			sharedNightclubCustomer.Value.ReleaseDanceSpot();
			return TaskStatus.Success;
		}
		return TaskStatus.Running;
	}

	private Vector3 GetDancingPosition()
	{
		_danceSpot = NightclubBusinessHelper.GetRandomDanceFloorSpot();
		if (_danceSpot != null)
		{
			_danceSpot.Occupy();
			sharedNightclubCustomer.Value.danceSpot = _danceSpot;
			return _danceSpot.position;
		}
		return sharedNightclubCustomer.Value.tpc.GetRandomPosition();
	}

	private void LookToDJIfThereIsOne()
	{
		ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindClosestItemByName(transform.position, NightclubBusinessHelper.DJBoothItemName);
		if (itemController != null)
		{
			sharedNightclubCustomer.Value.tpc.transform.LookAt(itemController.transform.position);
		}
	}

	private void StartDancing()
	{
		sharedNightclubCustomer.Value.tpc.SetDance(Dances.GetAllDances().GetRandom());
	}

	private void SetDancingStopTime()
	{
		_stopDancingTimestamp = TimeHelper.Now();
		_stopDancingTimestamp.AddMinutes(Random.Range(10, 30));
	}
}
