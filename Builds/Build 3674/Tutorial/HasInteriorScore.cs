using Helpers;
using InteriorDesign;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasInteriorScore")]
public class HasInteriorScore : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	[SerializeField]
	private int score;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(playerStoreTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		return InteriorScoreCalculator.GetInteriorScorePercentage(buildingRegistration.interiorDesigns) >= score;
	}
}
