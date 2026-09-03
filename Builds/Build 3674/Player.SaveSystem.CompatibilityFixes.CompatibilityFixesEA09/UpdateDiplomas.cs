namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateDiplomas : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.BuildingCached == null)
			{
				continue;
			}
			if (buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				if (!flag)
				{
					Diploma diploma = EducationHelper.GetDiploma(DiplomaName.Headquarters);
					diploma.completed = true;
					diploma.minutesStudied = EducationHelper.GetDiplomaData(DiplomaName.Headquarters).requiredMinutes;
					flag = true;
				}
			}
			else if (!flag2 && buildingRegistration.GetBuildingType() == "ba:buildingtype_office")
			{
				Diploma diploma2 = EducationHelper.GetDiploma(DiplomaName.OfficeBusinesses);
				diploma2.completed = true;
				diploma2.minutesStudied = EducationHelper.GetDiplomaData(DiplomaName.OfficeBusinesses).requiredMinutes;
				flag2 = true;
			}
			else if (!flag3 && buildingRegistration.businessTypeName == "ba:businesstype_factory")
			{
				Diploma diploma3 = EducationHelper.GetDiploma(DiplomaName.ProductManufacturing);
				diploma3.completed = true;
				diploma3.minutesStudied = EducationHelper.GetDiplomaData(DiplomaName.ProductManufacturing).requiredMinutes;
				flag3 = true;
			}
			if (flag2 & flag & flag3)
			{
				break;
			}
		}
	}
}
