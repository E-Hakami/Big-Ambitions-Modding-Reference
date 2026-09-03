using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Logistics/HasAnyTypeOfDeliveriesOnTakenOverBusiness")]
public class HasAnyTypeOfDeliveriesOnTakenOverBusiness : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		List<BuildingRegistration> takenOverPlayerBusinesses = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && !string.IsNullOrWhiteSpace(x.BusinessName) && x.takenOver).ToList();
		if (takenOverPlayerBusinesses.Count == 0)
		{
			return false;
		}
		if (takenOverPlayerBusinesses.Any((BuildingRegistration x) => x.GetBuildingType() == "ba:buildingtype_office"))
		{
			return true;
		}
		if (SaveGameManager.Current.logisticsManagerPlans.SelectMany((LogisticsManagerPlan x) => x.destinations).Any((LogisticsManagerPlanDestination x) => takenOverPlayerBusinesses.Any((BuildingRegistration b) => b.Address == x.deliveryTargetAddress)))
		{
			return true;
		}
		return SaveGameManager.Current.DeliveryContracts.Select((DeliveryContract x) => x.businessAddress).Any((Address x) => takenOverPlayerBusinesses.Any((BuildingRegistration b) => b.Address == x));
	}
}
