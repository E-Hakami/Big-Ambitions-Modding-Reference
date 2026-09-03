using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/Parking Ticket Goal")]
public class ParkingTicketGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.parkingTickets;
	}
}
