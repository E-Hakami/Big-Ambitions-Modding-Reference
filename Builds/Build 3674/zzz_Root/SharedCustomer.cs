using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCustomer : SharedVariable<Customer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedCustomer(Customer value)
	{
		return new SharedCustomer
		{
			mValue = value
		};
	}
}
