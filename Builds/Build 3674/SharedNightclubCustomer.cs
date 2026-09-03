using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedNightclubCustomer : SharedVariable<NightclubCustomer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedNightclubCustomer(NightclubCustomer value)
	{
		return new SharedNightclubCustomer
		{
			mValue = value
		};
	}
}
