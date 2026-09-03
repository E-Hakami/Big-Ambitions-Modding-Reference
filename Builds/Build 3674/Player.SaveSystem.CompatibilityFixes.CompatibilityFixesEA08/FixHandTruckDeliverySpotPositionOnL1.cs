using System.Linq;
using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixHandTruckDeliverySpotPositionOnL1 : ICompatibilityFix
{
	private const string AffectedBuildingSize = "ba:buildingsize_n";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if ((bool)buildingRegistration.BuildingCached && !(buildingRegistration.BuildingCached.BuildingSize != "ba:buildingsize_n") && buildingRegistration.BuildingCached.BuildingVersion == 1)
			{
				ItemInstance itemInstance = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_handtruckspawner");
				if (itemInstance != null)
				{
					itemInstance.position.x -= 0.5f;
				}
				ItemInstance itemInstance2 = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot");
				if (itemInstance2 != null)
				{
					itemInstance2.position.x -= 0.5f;
				}
			}
		}
	}
}
