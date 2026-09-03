namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables;

[TaskCategory("Big Ambitions/ItemController")]
public class SetSharedItemController : Action
{
	public SharedItemController targetValue;

	[RequiredField]
	public SharedItemController targetVariable;

	public override TaskStatus OnUpdate()
	{
		targetVariable.Value = targetValue?.Value;
		return TaskStatus.Success;
	}

	public override void OnReset()
	{
		targetValue = null;
		targetVariable = null;
	}
}
