using BehaviorDesigner.Runtime;
using Buildings.BuildingTypes.Special;

public class SharedCasinoGameType : SharedVariable<CasinoGameType>
{
	public override string ToString()
	{
		if (mValue != CasinoGameType.Blackjack)
		{
			return mValue.ToString();
		}
		return "Blackjack";
	}

	public static implicit operator SharedCasinoGameType(CasinoGameType value)
	{
		return new SharedCasinoGameType
		{
			mValue = value
		};
	}
}
