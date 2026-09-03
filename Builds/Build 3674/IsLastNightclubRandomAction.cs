using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class IsLastNightclubRandomAction : Conditional
{
	[SharedRequired]
	public SharedNightclubCustomer nightclubCustomer;

	public NightclubRandomAction nightclubRandomAction;

	public override TaskStatus OnUpdate()
	{
		if (nightclubCustomer.Value.lastAction != nightclubRandomAction)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
