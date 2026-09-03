using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Hairdresser")]
public class HasAnyHairChangeEntry : Conditional
{
	[SharedRequired]
	public SharedHairdresserCustomer sharedHairdresserCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedHairdresserCustomer.Value.hasAnyHairChange)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
