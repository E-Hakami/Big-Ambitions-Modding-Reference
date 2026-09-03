using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class IsTimeToLeave : Conditional
{
	[SharedRequired]
	public SharedNightclubCustomer nightclubCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!nightclubCustomer.Value.leavingTime.IsInThePast())
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
