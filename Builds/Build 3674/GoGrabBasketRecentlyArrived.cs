using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("Big Ambitions")]
public class GoGrabBasketRecentlyArrived : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	private CharacterShowEmojiExpression _characterShowEmojiExpression = new CharacterShowEmojiExpression();

	private bool _shoppingBasketGrabbed;

	public override void OnAwake()
	{
		_characterShowEmojiExpression.Init(sharedCustomer.Value.tpc, base.Owner);
	}

	public override void OnStart()
	{
		_shoppingBasketGrabbed = true;
		ItemController itemController = sharedCustomer.Value.FindShoppingBasket();
		if (itemController != null)
		{
			sharedCustomer.Value.SetBasket(itemController);
		}
		else
		{
			OnShoppingBasketNotFound();
		}
	}

	private void OnShoppingBasketNotFound()
	{
		Vector3 randomPosition = sharedCustomer.Value.tpc.GetRandomPosition(2f, 1.5f);
		sharedCustomer.Value.tpc.navmeshAgent.Warp(randomPosition);
		_characterShowEmojiExpression.StartShowingEmoji(CharacterEmojiName.CustomerCantFindShoppingBasket);
		_shoppingBasketGrabbed = false;
	}

	public override TaskStatus OnUpdate()
	{
		if (_shoppingBasketGrabbed)
		{
			return TaskStatus.Success;
		}
		if (!_characterShowEmojiExpression.HasFinishedShowingEmoji())
		{
			return TaskStatus.Running;
		}
		sharedCustomer.Value.Leave();
		return TaskStatus.Success;
	}

	public override void OnEnd()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
	}

	public override void OnBehaviorComplete()
	{
		_characterShowEmojiExpression.StopShowingEmoji();
	}
}
