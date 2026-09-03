using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class PutACoatInTheArm : Action
{
	[SharedRequired]
	public SharedNightclubCustomer sharedNightclubCustomer;

	public override void OnStart()
	{
		sharedNightclubCustomer.Value.PutACoatInTheArm();
	}
}
