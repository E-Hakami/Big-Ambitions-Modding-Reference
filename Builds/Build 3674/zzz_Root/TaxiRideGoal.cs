using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/Taxi Ride Goal")]
public class TaxiRideGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.taxiRides;
	}
}
