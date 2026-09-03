using System;
using BigAmbitions.Tags;
using Entities;
using UI.Smartphone.Apps.Contacts;
using Vehicles.VehicleTypes;

namespace Dialogs;

public static class AutoTowServiceHelper
{
	private const string ContactId = "auto_tow_service_ny";

	private const string ContactDescription = "contact_description_special";

	private const string WelcomeMessage = "phone_auto_tow_welcome";

	public static void Init()
	{
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(TryAddWelcomeContact));
	}

	private static void TryAddWelcomeContact(VehicleController vehicleController)
	{
		VehicleType vehicleType = vehicleController.vehicleType;
		if (vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle) || vehicleType.HasTag(TagRef.Vehicletag.isscooter))
		{
			return;
		}
		foreach (Contact contact in SaveGameManager.Current.Contacts)
		{
			if (!(contact.id != "auto_tow_service_ny"))
			{
				GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onEnterVehicle, new Action<VehicleController>(TryAddWelcomeContact));
				return;
			}
		}
		Contact.GetContact("auto_tow_service_ny", ContactCategoryName.General, "contact_description_special", null, hasWelcomeMessages: false, skipNewNotification: true).SendMessage(new TextMessage("phone_auto_tow_welcome"), notify: true, sendNotificationInstantly: true);
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onEnterVehicle, new Action<VehicleController>(TryAddWelcomeContact));
	}

	public static Contact GetAutoTowContact()
	{
		foreach (Contact contact in SaveGameManager.Current.Contacts)
		{
			if (contact.id == "auto_tow_service_ny")
			{
				return contact;
			}
		}
		return Contact.GetContact("auto_tow_service_ny", ContactCategoryName.General, "contact_description_special", null, hasWelcomeMessages: false, skipNewNotification: true);
	}
}
