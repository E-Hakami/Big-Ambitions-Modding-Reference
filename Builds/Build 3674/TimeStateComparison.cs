using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class TimeStateComparison : Conditional
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public CustomerTimeState compareTo;

	public override TaskStatus OnUpdate()
	{
		if (sharedCustomer.Value.customerTimeState != compareTo)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
