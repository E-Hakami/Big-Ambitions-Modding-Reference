using BehaviorDesigner.Runtime.Tasks;
using Buildings.BuildingTypes.Shared.BusinessRequirement;

[TaskCategory("Big Ambitions")]
public class BusinessTypeRequiresScale : Conditional
{
	public override TaskStatus OnUpdate()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.businessType.businessRequirements.Exists((BusinessRequirement x) => x.businessRequirementName == "ba:businessrequirement_scale"))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
