using BigAmbitions.Tags;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasCurrentBusinesses")]
public class HasCurrentBusinesses : QuestRequirement
{
	[SerializeField]
	private int maxBusinessesToQualify;

	public override bool CheckIfCompleted()
	{
		return BuildingHelper.GetPlayerBuildingRegistrations(PlayerBuildingFilterExcludingResidentialAndNewRented).Count <= maxBusinessesToQualify;
	}

	private bool PlayerBuildingFilterExcludingResidentialAndNewRented(BuildingRegistration buildingRegistration)
	{
		BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
		if (data != null && data.HasTag(TagRef.Businesstag.generatesrevenue))
		{
			return buildingRegistration.creationDay != -1;
		}
		return false;
	}
}
