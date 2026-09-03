using System.Linq;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Special.FurnitureStore;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RestoreDeliverySpotInPAndQWarehouses : ICompatibilityFix
{
	private const string BuildingSizeP = "ba:buildingsize_p";

	private const string BuildingSizeQ = "ba:buildingsize_q";

	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !(buildingRegistration.GetBuildingType() != "ba:buildingtype_warehouse") && (!(buildingRegistration.BuildingCached.BuildingSize != "ba:buildingsize_p") || !(buildingRegistration.BuildingCached.BuildingSize != "ba:buildingsize_q")))
			{
				MoveDeliverySpot(buildingRegistration);
			}
		}
	}

	private static void MoveDeliverySpot(BuildingRegistration registration)
	{
		ItemInstance itemInstance = registration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot");
		if (itemInstance != null && itemInstance.position == Vector3.zero)
		{
			FurnitureDeliveryHelper.PlaceDeliverySpotOnDefaultPosition(registration, itemInstance);
		}
	}
}
