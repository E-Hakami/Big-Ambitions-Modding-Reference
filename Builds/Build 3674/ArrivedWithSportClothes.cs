using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Gym")]
public class ArrivedWithSportClothes : Conditional
{
	[SharedRequired]
	public SharedGymCustomer sharedGymCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedGymCustomer.Value.arrivedWithSportClothes)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
