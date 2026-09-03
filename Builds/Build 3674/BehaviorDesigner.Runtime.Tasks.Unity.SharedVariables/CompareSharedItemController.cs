namespace BehaviorDesigner.Runtime.Tasks.Unity.SharedVariables;

[TaskCategory("Big Ambitions/ItemController")]
public class CompareSharedItemController : Conditional
{
	[RequiredField]
	public SharedItemController variable;

	[RequiredField]
	public SharedItemController compareTo;

	public override TaskStatus OnUpdate()
	{
		if (!(variable.Value == compareTo.Value))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}

	public override void OnReset()
	{
		variable = null;
		compareTo = null;
	}
}
