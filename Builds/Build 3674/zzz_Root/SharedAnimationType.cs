using System;
using BehaviorDesigner.Runtime;
using BigAmbitions.Characters;

[Serializable]
public class SharedAnimationType : SharedVariable<AnimationType>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedAnimationType(AnimationType value)
	{
		return new SharedAnimationType
		{
			mValue = value
		};
	}
}
