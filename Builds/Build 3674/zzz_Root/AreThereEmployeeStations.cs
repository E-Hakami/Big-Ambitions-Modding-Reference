using System.Linq;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class AreThereEmployeeStations : Conditional
{
	public SharedItemTag employeeStationTag;

	public override TaskStatus OnUpdate()
	{
		if (!WaitingLinesHelper.GetAvailableWaitingLines(employeeStationTag.AllWithTag).Any())
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
