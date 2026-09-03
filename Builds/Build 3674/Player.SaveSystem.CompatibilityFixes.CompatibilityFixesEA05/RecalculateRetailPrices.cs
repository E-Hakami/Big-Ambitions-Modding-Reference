using BigAmbitions.Tags;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class RecalculateRetailPrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				string buildingType = buildingRegistration.GetBuildingType();
				if ((buildingType == "ba:buildingtype_office" || buildingType == "ba:buildingtype_retail") && buildingRegistration.cachedAvailableProducts != null && buildingRegistration.cachedAvailableProducts.Count != 0 && BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowplayercreation))
				{
					CompetitionHelper.RecalculateRetailPrices(buildingRegistration);
				}
			}
		}
	}
}
