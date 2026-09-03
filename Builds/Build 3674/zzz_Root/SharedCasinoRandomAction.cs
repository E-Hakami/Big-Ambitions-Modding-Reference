using BehaviorDesigner.Runtime;

public class SharedCasinoRandomAction : SharedVariable<CasinoRandomAction>
{
	public override string ToString()
	{
		if (mValue != CasinoRandomAction.None)
		{
			return mValue.ToString();
		}
		return "None";
	}

	public static implicit operator SharedCasinoRandomAction(CasinoRandomAction value)
	{
		return new SharedCasinoRandomAction
		{
			mValue = value
		};
	}
}
