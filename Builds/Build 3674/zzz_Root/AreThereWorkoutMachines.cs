using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;

[TaskCategory("Big Ambitions/Gym")]
public class AreThereWorkoutMachines : Conditional
{
	public override TaskStatus OnUpdate()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.allItemControllers.Exists((ItemController x) => (x.Item.type & ItemType.WorkoutMachine) != 0))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
