using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCasinoCustomer : SharedVariable<CasinoCustomer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedCasinoCustomer(CasinoCustomer value)
	{
		return new SharedCasinoCustomer
		{
			mValue = value
		};
	}
}
