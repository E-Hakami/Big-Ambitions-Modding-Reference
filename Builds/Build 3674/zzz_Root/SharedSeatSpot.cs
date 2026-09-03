using BehaviorDesigner.Runtime;

public class SharedSeatSpot : SharedVariable<SeatSpot>
{
	public override string ToString()
	{
		if (mValue != null)
		{
			return mValue.ToString();
		}
		return "null";
	}

	public static implicit operator SharedSeatSpot(SeatSpot value)
	{
		return new SharedSeatSpot
		{
			mValue = value
		};
	}
}
