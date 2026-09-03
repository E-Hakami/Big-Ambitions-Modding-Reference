using System.Collections.Generic;
using Entities;
using Extensions;
using Streets;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class RescueVehiclesOnRoofTopsAndPark : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Vector3 safePosition = InstanceBehavior<GlobalReferences>.Instance.vehicleRespawnSafetySpot;
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			if (IsInsidePark(vehicleInstance))
			{
				SetVehicleToSafePosition(vehicleInstance, ref safePosition);
			}
			else if (vehicleInstance.position.y > 1f && !IsInsideMultiLayeredParking(vehicleInstance))
			{
				SetVehicleToSafePosition(vehicleInstance, ref safePosition);
			}
		}
	}

	private static bool IsInsidePark(VehicleInstance vehicleInstance)
	{
		if (!vehicleInstance.position.z.InRange(32f, 296f) || !vehicleInstance.position.x.InRange(-700f, -136f))
		{
			if (vehicleInstance.position.z.InRange(-9f, 32f))
			{
				return vehicleInstance.position.x.InRange(-700f, -183f);
			}
			return false;
		}
		return true;
	}

	private static bool IsInsideMultiLayeredParking(VehicleInstance vehicleInstance)
	{
		if (vehicleInstance.position.z.InRange(-197f, -143f))
		{
			return vehicleInstance.position.x.InRange(-449f, -400f);
		}
		return false;
	}

	private static void SetVehicleToSafePosition(VehicleInstance vehicleInstance, ref Vector3 safePosition)
	{
		vehicleInstance.position = safePosition;
		vehicleInstance.rotation = Quaternion.identity;
		safePosition.x -= 3f;
		Address address = new Address("ba:street_thirdstreet", 73);
		Contact contact = Contact.GetContact("auto_tow_service_ny", ContactCategoryName.General, "contact_description_special");
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"address",
			address.ToFormattedString()
		} };
		contact.SendMessage(new TextMessage("ba:messagetype_dialog_auto_tow_vehicle_recovery", messageData));
	}
}
