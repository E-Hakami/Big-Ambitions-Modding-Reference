using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/GolfHighScoreGoal")]
public class GolfHighScoreGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.golfHighScore;
	}
}
