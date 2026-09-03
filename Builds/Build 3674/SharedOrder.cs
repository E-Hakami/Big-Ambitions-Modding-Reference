using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedOrder : SharedVariable<Order>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedOrder(Order value)
	{
		return new SharedOrder
		{
			mValue = value
		};
	}
}
