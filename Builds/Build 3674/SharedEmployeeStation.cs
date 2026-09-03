using BehaviorDesigner.Runtime;

public class SharedEmployeeStation : SharedVariable<EmployeeStationController>
{
	public override string ToString()
	{
		if (!(mValue == null))
		{
			return mValue.ToString();
		}
		return "null";
	}

	public static implicit operator SharedEmployeeStation(EmployeeStationController value)
	{
		return new SharedEmployeeStation
		{
			mValue = value
		};
	}
}
