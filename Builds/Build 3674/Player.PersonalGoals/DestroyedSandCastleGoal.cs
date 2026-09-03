using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/DestroyedSandCastleGoal")]
public class DestroyedSandCastleGoal : GenericPersonalGoal
{
	protected override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.achievementsData.destroyedSandCastle;
	}
}
