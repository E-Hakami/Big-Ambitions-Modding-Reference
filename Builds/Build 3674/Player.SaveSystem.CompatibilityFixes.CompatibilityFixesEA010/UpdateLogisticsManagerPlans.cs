using Buildings.Office.Headquarters;
using Entities;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateLogisticsManagerPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in gameInstance.logisticsManagerPlans)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(logisticsManagerPlan.targetAddress);
			if (buildingRegistration == null)
			{
				continue;
			}
			logisticsManagerPlan.isFactory = buildingRegistration.businessTypeName == "ba:businesstype_factory";
			if (logisticsManagerPlan.isFactory)
			{
				logisticsManagerPlan.destinations.RemoveAll(delegate(LogisticsManagerPlanDestination x)
				{
					string text = BuildingHelper.GetBuildingRegistration(x.deliveryTargetAddress)?.businessTypeName;
					return !(text == "ba:businesstype_warehouse") && !(text == "ba:businesstype_importexport");
				});
			}
		}
	}
}
