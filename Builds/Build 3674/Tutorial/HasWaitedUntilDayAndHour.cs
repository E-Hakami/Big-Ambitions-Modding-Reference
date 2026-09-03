using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/HasWaitedUntilDayAndHour")]
public class HasWaitedUntilDayAndHour : QuestRequirement
{
	[SerializeField]
	private int day;

	[SerializeField]
	private int hour;

	public override bool CheckIfCompleted()
	{
		if (TimeHelper.CurrentDay >= day)
		{
			return TimeHelper.CurrentHour >= hour;
		}
		return false;
	}
}
