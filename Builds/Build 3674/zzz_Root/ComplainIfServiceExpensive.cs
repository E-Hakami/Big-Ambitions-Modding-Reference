using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Entities;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class ComplainIfServiceExpensive : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[SharedRequired]
	public SharedBool sharedJoined;

	private bool _complained;

	private readonly ExpressionDataContainer _expressionDataContainer = new ExpressionDataContainer();

	private readonly CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	public override void OnAwake()
	{
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (!sharedJoined.Value)
		{
			return;
		}
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (entry.processed)
			{
				continue;
			}
			Item byName = ItemsGetter.GetByName(entry.itemName);
			if ((bool)byName && (byName.type & ItemType.ServiceProduct) != 0)
			{
				OrderHelper.Validate(sharedCustomer.Value.citizenData, entry, null);
				if (!entry.priceAccceptable)
				{
					_expressionDataContainer.itemName = entry.itemName;
					_characterShowEmojiExpression.StartShowingEmoji(CharacterEmojiName.CustomerTooHighPrice, 3f, _expressionDataContainer);
					_complained = true;
					break;
				}
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_complained)
		{
			return TaskStatus.Success;
		}
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		return TaskStatus.Failure;
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
	}
}
