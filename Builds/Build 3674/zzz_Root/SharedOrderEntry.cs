using System;
using BehaviorDesigner.Runtime;
using Entities;

[Serializable]
public class SharedOrderEntry : SharedVariable<OrderEntry>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedOrderEntry(OrderEntry value)
	{
		return new SharedOrderEntry
		{
			mValue = value
		};
	}
}
