using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedFullServiceCustomer : SharedVariable<FullServiceCustomer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedFullServiceCustomer(FullServiceCustomer value)
	{
		return new SharedFullServiceCustomer
		{
			mValue = value
		};
	}
}
