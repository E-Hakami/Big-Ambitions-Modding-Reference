namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateBusinessTypeNames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.businessTypeName == "ba:businesstype_empty")
			{
				if (buildingRegistration.BusinessName == "IRS")
				{
					buildingRegistration.businessTypeName = "ba:businesstype_irs";
				}
			}
			else
			{
				if (!(buildingRegistration.businessTypeName == "ba:businesstype_casino"))
				{
					continue;
				}
				if (buildingRegistration.BusinessName == "Keep Calm and Squat On")
				{
					buildingRegistration.businessTypeName = "ba:businesstype_gym";
					continue;
				}
				if (buildingRegistration.BusinessName == "Truck Garage")
				{
					buildingRegistration.businessTypeName = "ba:businesstype_truckgarage";
					continue;
				}
				string businessName = buildingRegistration.BusinessName;
				if (businessName == "United Gasoline" || businessName == "Manhattan Gas")
				{
					buildingRegistration.businessTypeName = "ba:businesstype_gasstation";
				}
			}
		}
		gameInstance.SelectedCitymapFilters.Clear();
	}
}
