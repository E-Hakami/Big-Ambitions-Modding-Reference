using System;
using BehaviorDesigner.Runtime;
using BigAmbitions.Characters;

[Serializable]
public class SharedPermanentAnimationType : SharedVariable<PermanentAnimationType>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedPermanentAnimationType(PermanentAnimationType value)
	{
		return new SharedPermanentAnimationType
		{
			mValue = value
		};
	}
}
