using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Hospital/HasHealthInsurancePlanOffers")]
public class HasHealthInsurancePlanOffers : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.healthInsurancePlanOffers.Count > 0;
	}
}
