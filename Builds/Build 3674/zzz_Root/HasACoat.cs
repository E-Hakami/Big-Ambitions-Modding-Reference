using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class HasACoat : Conditional
{
	[SharedRequired]
	public SharedNightclubCustomer nightclubCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!nightclubCustomer.Value.hasCoat)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
