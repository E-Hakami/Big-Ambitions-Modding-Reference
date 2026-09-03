using System.Collections.Generic;
using Entities;
using Extensions;
using Streets;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RescueVehiclesUnderStockCo : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Vector3 safePosition = InstanceBehavior<GlobalReferences>.Instance.vehicleRespawnSafetySpot;
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			if (IsUnderStockCo(vehicleInstance))
			{
				SetVehicleToSafePosition(vehicleInstance, ref safePosition);
			}
		}
	}

	private static bool IsUnderStockCo(VehicleInstance vehicleInstance)
	{
		if (vehicleInstance.position.x.InRange(3.5478f, 24.5478f) && vehicleInstance.position.y.InRange(0f, 4f))
		{
			return vehicleInstance.position.z.InRange(267.29f, 277.29f);
		}
		return false;
	}

	private static void SetVehicleToSafePosition(VehicleInstance vehicleInstance, ref Vector3 safePosition)
	{
		vehicleInstance.position = safePosition;
		vehicleInstance.rotation = Quaternion.identity;
		safePosition.x -= 3f;
		Address address = new Address("ba:street_thirdstreet", 76);
		Contact contact = Contact.GetContact("auto_tow_service_ny", ContactCategoryName.General, "contact_description_special");
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"address",
			address.ToFormattedString()
		} };
		contact.SendMessage(new TextMessage("ba:messagetype_dialog_auto_tow_vehicle_recovery", messageData));
	}
}
