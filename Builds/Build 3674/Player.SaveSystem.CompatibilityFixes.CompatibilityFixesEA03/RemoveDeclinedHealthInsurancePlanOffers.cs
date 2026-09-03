using System.Linq;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class RemoveDeclinedHealthInsurancePlanOffers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (HealthInsurancePlanOffer healthInsurancePlan in gameInstance.healthInsurancePlanOffers.ToList())
		{
			if (!healthInsurancePlan.negotiationFinished && gameInstance.Contacts.FirstOrDefault((Contact x) => x.id == "hospital_health_insurance_manager")?.messagesQueue.FirstOrDefault((TextMessage x) => x.contextAction?.healthPlanOfferId == healthInsurancePlan.id) == null)
			{
				gameInstance.healthInsurancePlanOffers.Remove(healthInsurancePlan);
			}
		}
	}
}
