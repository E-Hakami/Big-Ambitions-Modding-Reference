using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class IsPlayerOwnedBusiness : Conditional
{
	public override TaskStatus OnUpdate()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
