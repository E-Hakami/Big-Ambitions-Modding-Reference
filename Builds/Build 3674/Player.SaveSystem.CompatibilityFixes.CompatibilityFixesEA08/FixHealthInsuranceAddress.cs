using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixHealthInsuranceAddress : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (Contact contact in gameInstance.Contacts)
		{
			if (!(contact.id != "hospital_health_insurance_manager"))
			{
				contact.streetName = "ba:street_seventhstreet";
				contact.streetNumber = 2;
				break;
			}
		}
	}
}
