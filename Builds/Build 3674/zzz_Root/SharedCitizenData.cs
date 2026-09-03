using System;
using AI.Citizens;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedCitizenData : SharedVariable<CitizenData>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedCitizenData(CitizenData value)
	{
		return new SharedCitizenData
		{
			mValue = value
		};
	}
}
