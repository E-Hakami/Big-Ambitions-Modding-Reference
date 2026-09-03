using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class SetLastNightclubRandomAction : Action
{
	[SharedRequired]
	public SharedNightclubCustomer sharedNightclubCustomer;

	public NightclubRandomAction nightclubRandomAction;

	public override void OnStart()
	{
		sharedNightclubCustomer.Value.lastAction = nightclubRandomAction;
	}
}
