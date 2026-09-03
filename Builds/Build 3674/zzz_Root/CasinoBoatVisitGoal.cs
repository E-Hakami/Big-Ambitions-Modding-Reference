using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/Casino Boat Visit Goal")]
public class CasinoBoatVisitGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.casinoBoatVisits;
	}
}
