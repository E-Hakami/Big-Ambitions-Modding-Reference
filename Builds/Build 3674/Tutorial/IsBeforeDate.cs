using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Time/IsBeforeDate")]
public class IsBeforeDate : QuestRequirement
{
	public bool useYear;

	[HideIf("useYear")]
	public int day;

	[ShowIf("useYear")]
	public int year;

	public override bool CheckIfCompleted()
	{
		if (useYear)
		{
			return TimeHelper.GetYearsByDays(SaveGameManager.Current.Day) < year;
		}
		return SaveGameManager.Current.Day < day;
	}
}
