using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class CustomerLeave : Action
{
	public override void OnStart()
	{
		GetComponent<Customer>().Leave();
	}
}
