using BehaviorDesigner.Runtime;

public class SharedItemController : SharedVariable<ItemController>
{
	public override string ToString()
	{
		if (!(mValue == null))
		{
			return mValue.ToString();
		}
		return "null";
	}

	public static implicit operator SharedItemController(ItemController value)
	{
		return new SharedItemController
		{
			mValue = value
		};
	}
}
