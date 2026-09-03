using BehaviorDesigner.Runtime.Tasks;
using HGAttributes;

[TaskCategory("Big Ambitions")]
public class IsBusinessType : Conditional
{
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	public override TaskStatus OnUpdate()
	{
		if (!(InstanceBehavior<BuildingManager>.Instance.businessType.businessTypeName == businessTypeName))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
