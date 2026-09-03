using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/PlayerAgeGoal")]
public class PlayerAgeGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return TimeHelper.GetYearsByDays(TimeHelper.GetPlayerRealAgeInDays(PlayerHelper.CharacterData.ageInDays));
	}
}
