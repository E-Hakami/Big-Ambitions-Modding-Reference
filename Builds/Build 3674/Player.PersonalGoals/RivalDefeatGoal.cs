using BigAmbitions.Rivals;
using Extensions;
using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/RivalDefeatGoal")]
public class RivalDefeatGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.specialRivalStates?.CountWhere((SpecialRivalState x) => x.isDefeated) ?? 0;
	}
}
