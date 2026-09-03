using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class RenameAIBusiness : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration registration) => registration.BusinessName == "Gastronom ItaIia"))
		{
			item.BusinessName = "Gastronom Italia";
		}
	}
}
