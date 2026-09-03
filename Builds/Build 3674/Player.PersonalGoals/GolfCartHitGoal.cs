using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/GolfCartHitGoal")]
public class GolfCartHitGoal : GenericPersonalGoal
{
	protected override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.achievementsData.golfCartHit;
	}
}
