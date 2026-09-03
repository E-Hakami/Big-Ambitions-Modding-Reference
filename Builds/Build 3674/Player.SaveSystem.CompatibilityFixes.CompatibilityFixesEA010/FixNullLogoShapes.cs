using Enums;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class FixNullLogoShapes : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration.logoSettings.logoShape == null)
			{
				string businessTypeName = buildingRegistration.businessTypeName;
				string logoShape = ((businessTypeName != "ba:businesstype_empty") ? BusinessTypeHelper.GetData(businessTypeName).logoShapes.GetRandom() : "");
				buildingRegistration.logoSettings = new LogoSettings
				{
					backgroundColor = Colors.White,
					fontColor = Colors.Black,
					logoColor = Colors.Black,
					font = FontFace.Rubik,
					logoShape = logoShape
				};
			}
		}
	}
}
