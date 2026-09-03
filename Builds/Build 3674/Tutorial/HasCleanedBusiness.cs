using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasCleanedBusiness")]
public class HasCleanedBusiness : QuestRequirement
{
	public CustomBuildingTarget customBuildingTarget;

	public float minimumCleanliness;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return false;
		}
		if (!HasCleaningStation(buildingRegistration))
		{
			return false;
		}
		return buildingRegistration.GetCleanliness() >= minimumCleanliness;
	}

	private static bool HasCleaningStation(BuildingRegistration registration)
	{
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if (value.ItemCached.HasTag(TagRef.Itemtag.iscleaningstation))
			{
				return true;
			}
		}
		return false;
	}
}
