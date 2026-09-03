using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using BigAmbitions.Tags;

[TaskCategory("Big Ambitions/SelfServiceCustomer")]
public class CheckScaleAndComplainIfRequired : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedOrderEntry sharedOrderEntry;

	private bool _isExpressionShown;

	private readonly CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	public override void OnAwake()
	{
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		if (ItemsGetter.GetByName(sharedOrderEntry.Value.itemName).requiresWeighing && !InstanceBehavior<BuildingManager>.Instance.AreThereItemsByName(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isweighingscale)))
		{
			sharedOrderEntry.Value.processed = true;
			if (!_isExpressionShown)
			{
				_characterShowEmojiExpression.StartShowingEmoji(CharacterEmojiName.CustomerCantFindScale, 4f);
				_isExpressionShown = true;
			}
		}
	}

	public override TaskStatus OnUpdate()
	{
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		if (!sharedOrderEntry.Value.processed)
		{
			return TaskStatus.Success;
		}
		return TaskStatus.Failure;
	}

	public override void OnEnd()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
	}

	public override void OnBehaviorComplete()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
		_isExpressionShown = false;
	}
}
