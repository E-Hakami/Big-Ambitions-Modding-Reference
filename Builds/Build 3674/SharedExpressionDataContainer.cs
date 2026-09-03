using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedExpressionDataContainer : SharedVariable<ExpressionDataContainer>
{
	public override string ToString()
	{
		if (mValue != null)
		{
			return mValue.ToString();
		}
		return "null";
	}

	public static implicit operator SharedExpressionDataContainer(ExpressionDataContainer value)
	{
		return new SharedExpressionDataContainer
		{
			mValue = value
		};
	}
}
