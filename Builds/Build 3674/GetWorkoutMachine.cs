using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Extensions;

[TaskCategory("Big Ambitions/Gym")]
public class GetWorkoutMachine : Action
{
	[RequiredField]
	public SharedItemController sharedItemController;

	[RequiredField]
	public SharedWorkoutMachineController sharedWorkoutMachineController;

	[RequiredField]
	public SharedWorkoutTypes workoutTypesDone;

	[RequiredField]
	public SharedWorkoutType chosenWorkoutType;

	public override void OnStart()
	{
		WorkoutMachineController[] source = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x.Item != null && (x.Item.type & ItemType.WorkoutMachine) != 0 && !x.Occupied && string.IsNullOrEmpty(x.ItemInstance.parentId) && ItemHelper.GetMissingRequirements(x.ItemInstance).Count <= 0).OfType<WorkoutMachineController>().ToArray();
		WorkoutMachineController random = source.Where((WorkoutMachineController x) => !workoutTypesDone.Value.Contains(x.GetWorkoutExercise().workoutType)).GetRandom();
		if (random == null)
		{
			random = source.Where((WorkoutMachineController x) => x != sharedItemController.Value).GetRandom();
		}
		if (random == null)
		{
			sharedItemController.Value = null;
			sharedWorkoutMachineController.Value = null;
			return;
		}
		sharedItemController.Value = random;
		sharedWorkoutMachineController.Value = random;
		chosenWorkoutType.Value = random.GetWorkoutExercise().workoutType;
		workoutTypesDone.Value.Add(random.GetWorkoutExercise().workoutType);
	}

	public override TaskStatus OnUpdate()
	{
		if (!(sharedItemController.Value != null))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
