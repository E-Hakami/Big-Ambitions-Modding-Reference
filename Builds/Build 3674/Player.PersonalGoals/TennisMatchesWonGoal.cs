using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/TennisMatchesWonGoal")]
public class TennisMatchesWonGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.tennisMatchesWon;
	}
}
