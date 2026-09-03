using BehaviorDesigner.Runtime;
using Buildings.BuildingTypes.Special;

public class SharedPlaySpotsManager : SharedVariable<PlaySpotsManager>
{
	public override string ToString()
	{
		if (!(mValue == null))
		{
			return mValue.ToString();
		}
		return "null";
	}

	public static implicit operator SharedPlaySpotsManager(PlaySpotsManager value)
	{
		return new SharedPlaySpotsManager
		{
			mValue = value
		};
	}
}
