using BehaviorDesigner.Runtime.Tasks;

public class NightclubCustomerOnEnter : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		CustomerTimeState customerTimeState = sharedCustomer.Value.customerTimeState;
		if ((customerTimeState == CustomerTimeState.RecentlyArrived || customerTimeState == CustomerTimeState.AlreadyInAction) && IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
		{
			sharedCustomer.Value.tpc.navmeshAgent.Warp(randomPosition);
		}
	}
}
