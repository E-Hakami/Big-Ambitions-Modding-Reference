using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Queue")]
public class CustomerJoinQueueInstantly : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedItemTag employeeStationTag;

	[SharedRequired]
	public SharedBool sharedJoined;

	private bool _canJoin;

	private bool _hasStartedJoiningWaitingLine;

	public override void OnStart()
	{
		if (sharedJoined != null)
		{
			sharedJoined.Value = false;
		}
		if (WaitingLinesHelper.GetAvailableWaitingLines(employeeStationTag.AllWithTag).Any() && WaitingLinesHelper.IsThereAWaitingLineWithSpotsAvailable(employeeStationTag.AllWithTag))
		{
			_canJoin = true;
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_canJoin)
		{
			return TaskStatus.Success;
		}
		if (!_hasStartedJoiningWaitingLine)
		{
			StartJoiningWaitingLineInstantly();
		}
		if (sharedCustomer.Value.state != CustomerState.Served)
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Success;
	}

	private void StartJoiningWaitingLineInstantly()
	{
		WaitingLinesHelper.GetLessCrowdedWaitingLine(employeeStationTag.AllWithTag).WaitingLineHolder.JoinWaitingLineInstantly(sharedCustomer.Value);
		_hasStartedJoiningWaitingLine = true;
		if (sharedJoined != null)
		{
			sharedJoined.Value = true;
		}
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private void Reset()
	{
		_canJoin = false;
		_hasStartedJoiningWaitingLine = false;
	}
}
