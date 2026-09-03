using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedGymCustomer : SharedVariable<GymCustomer>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedGymCustomer(GymCustomer value)
	{
		return new SharedGymCustomer
		{
			mValue = value
		};
	}
}
