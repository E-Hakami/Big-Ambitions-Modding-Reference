using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using UnityEngine;
using UnityEngine.AI;

public class DJEmployee : Employee
{
	private const int minMinutesBetweenEncouragements = 10;

	private const int maxMinutesBetweenEncouragements = 30;

	private Timestamp _changeAnimationTimestamp;

	private bool _pendingResumeAnimation;

	public override void Start()
	{
		base.Start();
		employeeTpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		SetNextEncouragementTimestamp();
		employeeTpc.animator.SetBool(PermanentAnimationType.DJ);
	}

	protected override void Update()
	{
		if (base.IsAway)
		{
			return;
		}
		if (_pendingResumeAnimation)
		{
			employeeTpc.animator.SetBool(PermanentAnimationType.DJ);
			_pendingResumeAnimation = false;
		}
		if (_changeAnimationTimestamp.IsInThePast())
		{
			TryStartToiletCoroutine();
			if (base.IsAway)
			{
				employeeTpc.animator.SetBool(PermanentAnimationType.DJ, state: false);
				_pendingResumeAnimation = true;
			}
			else
			{
				SetNextEncouragementTimestamp();
				employeeTpc.animator.SetTrigger(AnimationType.DJEncouraging);
			}
		}
	}

	private void SetNextEncouragementTimestamp()
	{
		_changeAnimationTimestamp = TimeHelper.Now();
		_changeAnimationTimestamp.AddMinutes(Random.Range(10, 30));
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}
}
