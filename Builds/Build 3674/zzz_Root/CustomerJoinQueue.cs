using System.Linq;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Queue")]
public class CustomerJoinQueue : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public SharedItemTag employeeStationTag;

	public SharedCharacterEmojiName noAvailableEmployeeStationsCharacterEmojiName;

	public SharedCharacterEmojiName noEmployeeStationsWithFreeSpotsCharacterEmojiName;

	[SharedRequired]
	public SharedBool sharedJoined;

	private CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private bool _complained;

	private bool _hasStartedjoiningWaitingLine;

	public override void OnAwake()
	{
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		Reset();
		if (sharedJoined != null)
		{
			sharedJoined.Value = false;
		}
		string[] allWithTag = employeeStationTag.AllWithTag;
		if (WaitingLinesHelper.GetAvailableWaitingLines(allWithTag).Any())
		{
			if (!WaitingLinesHelper.IsThereAWaitingLineWithSpotsAvailable(allWithTag))
			{
				Complain(noEmployeeStationsWithFreeSpotsCharacterEmojiName.Value);
			}
		}
		else
		{
			Complain(noAvailableEmployeeStationsCharacterEmojiName.Value);
		}
	}

	private void Complain(CharacterEmojiName characterEmojiName)
	{
		_characterShowEmojiExpression.StartShowingEmoji(characterEmojiName);
		_complained = true;
	}

	public override TaskStatus OnUpdate()
	{
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		if (_complained)
		{
			return TaskStatus.Success;
		}
		if (!_hasStartedjoiningWaitingLine)
		{
			StartJoiningWaitingLine();
		}
		if (sharedCustomer.Value.state != CustomerState.Served)
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Success;
	}

	private void StartJoiningWaitingLine()
	{
		WaitingLinesHelper.GetLessCrowdedWaitingLine(employeeStationTag.AllWithTag).WaitingLineHolder.JoinWaitingLine(sharedCustomer.Value);
		_hasStartedjoiningWaitingLine = true;
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
		_characterShowEmojiExpression.StopShowingEmoji();
		_complained = false;
		_hasStartedjoiningWaitingLine = false;
	}
}
