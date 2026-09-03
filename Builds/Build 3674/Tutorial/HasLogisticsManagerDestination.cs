using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using Streets;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Logistics/HasLogisticsManagerDestination")]
public class HasLogisticsManagerDestination : QuestRequirement
{
	[SerializeField]
	private QuestEntryTarget destinationTarget;

	[SerializeField]
	private bool onAnyTakenOverBusiness;

	public override bool CheckIfCompleted()
	{
		if (destinationTarget == null)
		{
			List<BuildingRegistration> playerBusinesses = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && !string.IsNullOrWhiteSpace(x.BusinessName)).ToList();
			return SaveGameManager.Current.logisticsManagerPlans.SelectMany((LogisticsManagerPlan x) => x.destinations).Any((LogisticsManagerPlanDestination x) => playerBusinesses.Any((BuildingRegistration b) => (!onAnyTakenOverBusiness || b.takenOver) && b.Address == x.deliveryTargetAddress));
		}
		Address destinationAddress = destinationTarget.GetAddress();
		if (destinationAddress == null || destinationAddress.IsUndefined())
		{
			return false;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(destinationAddress);
		if (buildingRegistration.businessTypeName != "ba:businesstype_empty" && BusinessTypeHelper.GetData(buildingRegistration).GetPrimaryRetailProducts().Count == 0)
		{
			return true;
		}
		return SaveGameManager.Current.logisticsManagerPlans.SelectMany((LogisticsManagerPlan x) => x.destinations).Any((LogisticsManagerPlanDestination x) => x.deliveryTargetAddress == destinationAddress);
	}
}
