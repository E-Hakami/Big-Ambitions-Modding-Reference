using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using Buildings.BuildingTypes.Shared.Dirtiness;
using PlayerActivity;

[TaskCategory("Big Ambitions/Gym")]
public class UseWorkoutMachine : Action
{
	[RequiredField]
	public SharedInt minutesSpentOnMachines;

	[RequiredField]
	public SharedWorkoutMachineController sharedWorkoutMachineController;

	[RequiredField]
	public SharedWorkoutTypes sharedWorkoutTypes;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	private Timestamp _endTime;

	private int _lastMinutesInMachine;

	private WorkoutAnimatorController _workoutAnimatorController;

	private bool _isWorkingOut;

	public override void OnStart()
	{
		sharedWorkoutMachineController.Value.Occupied = true;
		sharedCustomer.Value.tpc.ForceToTransform(sharedWorkoutMachineController.Value.characterPosition);
		sharedCustomer.Value.tpc.SetItemIKTargets(sharedWorkoutMachineController.Value, smooth: true);
		if (_workoutAnimatorController == null)
		{
			_workoutAnimatorController = new WorkoutAnimatorController();
		}
		_workoutAnimatorController.InitAnimations(sharedCustomer.Value.tpc, sharedWorkoutMachineController.Value);
		_isWorkingOut = true;
		_workoutAnimatorController.StartWorkoutAnimation();
		sharedCustomer.Value.tpc.navmeshAgent.enabled = false;
		_endTime = TimeHelper.Now();
		_endTime.AddMinutes(minutesSpentOnMachines.Value);
		_lastMinutesInMachine = (int)TimeHelper.NowInMinutes();
		sharedWorkoutTypes.Value.Add(sharedWorkoutMachineController.Value.GetWorkoutExercise().workoutType);
	}

	public override TaskStatus OnUpdate()
	{
		if (!_endTime.IsInThePast())
		{
			if (TimeHelper.NowInMinutes() - (float)_lastMinutesInMachine >= 1f)
			{
				_lastMinutesInMachine = (int)TimeHelper.NowInMinutes();
				_workoutAnimatorController.UpdateAnimations();
			}
			return TaskStatus.Running;
		}
		FinishWorkingOut();
		return TaskStatus.Success;
	}

	public override void OnBehaviorComplete()
	{
		FinishWorkingOut();
	}

	private void FinishWorkingOut()
	{
		if (_isWorkingOut)
		{
			_isWorkingOut = false;
			_workoutAnimatorController.StopWorkoutAnimation();
			sharedCustomer.Value.tpc.SetItemIKTargets(null, smooth: true);
			if (BuildingManager.IsInsideBuilding && (bool)sharedWorkoutMachineController?.Value)
			{
				BuildingCleanlinessHelper.ApplyDirt(InstanceBehavior<BuildingManager>.Instance.buildingRegistration, sharedWorkoutMachineController.Value.ItemInstance);
				sharedCustomer.Value.tpc.navmeshAgent.Warp(sharedWorkoutMachineController.Value.EndOfWorkoutPoint.position);
				sharedCustomer.Value.tpc.ForceToRotation(sharedWorkoutMachineController.Value.EndOfWorkoutPoint.rotation);
				sharedWorkoutMachineController.Value.Occupied = false;
				sharedCustomer.Value.tpc.Reset();
			}
		}
	}
}
