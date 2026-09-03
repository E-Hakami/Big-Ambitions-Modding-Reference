using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Hairdresser")]
public class HasHairShampooing : Conditional
{
	[SharedRequired]
	public SharedHairdresserCustomer sharedHairdresserCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedHairdresserCustomer.Value.hasHairShampooing)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
