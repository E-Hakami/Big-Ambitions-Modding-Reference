using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class TimeStatesComparison : Conditional
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public CustomerTimeState[] compareTo;

	public override TaskStatus OnUpdate()
	{
		for (int i = 0; i < compareTo.Length; i++)
		{
			CustomerTimeState customerTimeState = compareTo[i];
			if (sharedCustomer.Value.customerTimeState == customerTimeState)
			{
				return TaskStatus.Success;
			}
		}
		return TaskStatus.Failure;
	}
}
