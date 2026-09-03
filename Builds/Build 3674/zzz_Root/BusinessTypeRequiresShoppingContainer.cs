using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Tags;

[TaskCategory("Big Ambitions")]
public class BusinessTypeRequiresShoppingContainer : Conditional
{
	public override TaskStatus OnUpdate()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedshoppingcontainer))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
