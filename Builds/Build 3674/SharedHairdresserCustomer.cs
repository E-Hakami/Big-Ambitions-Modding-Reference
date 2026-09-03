using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedHairdresserCustomer : SharedVariable<HairdresserCustomer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedHairdresserCustomer(HairdresserCustomer value)
	{
		return new SharedHairdresserCustomer
		{
			mValue = value
		};
	}
}
