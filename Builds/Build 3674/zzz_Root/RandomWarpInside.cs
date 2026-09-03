using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class RandomWarpInside : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public bool returnAlwaysSuccess;

	public override TaskStatus OnUpdate()
	{
		if (IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
		{
			sharedCustomer.Value.tpc.navmeshAgent.Warp(randomPosition);
			return TaskStatus.Success;
		}
		if (!returnAlwaysSuccess)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
