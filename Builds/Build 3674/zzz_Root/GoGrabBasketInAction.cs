using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class GoGrabBasketInAction : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		ItemController itemController = sharedCustomer.Value.FindShoppingBasket();
		if (itemController != null)
		{
			sharedCustomer.Value.SetBasket(itemController);
		}
		else
		{
			sharedCustomer.Value.Leave();
		}
	}
}
