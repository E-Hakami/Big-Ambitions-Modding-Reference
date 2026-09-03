using System.Linq;
using Entities;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/TotalDailyCustomerGoal")]
public class TotalDailyCustomerGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return (from x in SaveGameManager.Current.BuildingRegistrations
			where x.RentedByPlayer
			select (x.orderHistory.Count != 0) ? x.orderHistory.Max((OrderHistoryEntry o) => o.totalCustomers) : 0).DefaultIfEmpty(0).Max();
	}
}
