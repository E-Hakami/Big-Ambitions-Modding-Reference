using BigAmbitions.Items;
using Blueprints;
using Buildings;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class FixRoofItemPositions : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			BuildingSizeInfo sizeInfo = new BuildingSizeInfo(buildingRegistration);
			MultipleHeightsBuildingController component = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(sizeInfo).GetComponent<MultipleHeightsBuildingController>();
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value.ItemCached.snapToCeiling && value.position.y < 0.1f)
				{
					value.position.y = (component ? component.GetCeilingYPositionForRoofObject(value.position) : BuildingSizeHelper.GetBuildingRoofPosition(buildingRegistration.BuildingCached.BuildingSize, 0));
				}
			}
		}
	}
}
