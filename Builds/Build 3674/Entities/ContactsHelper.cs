using System;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using Helpers;
using UI.Apps.Contacts;
using UI.Notification;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Entities;

public static class ContactsHelper
{
	private const int MaxAmountOfNotifications = 5;

	private const string AddressableLabel = "ContactPresets";

	private static readonly List<Contact> ContactsToSendNotification = new List<Contact>();

	private static Dictionary<string, ContactPreset> ContactPresets;

	private static Contact ParkingTicketContact;

	private static int PendingParkingTicketNotifications;

	private static bool HasUpdatedCategories;

	private static void Init()
	{
		if (ContactPresets != null)
		{
			return;
		}
		ContactPresets = new Dictionary<string, ContactPreset>();
		foreach (ContactPreset item in Addressables.LoadAssetsAsync<ContactPreset>("ContactPresets", null).WaitForCompletion())
		{
			ContactPresets.Add(item.id, item);
		}
	}

	public static void UpdateCategories()
	{
		if (HasUpdatedCategories)
		{
			return;
		}
		Init();
		foreach (ContactPreset preset in ContactPresets.Values)
		{
			Contact contact = SaveGameManager.Current.Contacts.Find((Contact x) => x.id == preset.id);
			if (contact != null && contact.category != preset.category)
			{
				contact.category = preset.category;
			}
		}
		HasUpdatedCategories = true;
	}

	public static void AddContactToSendNotification(Contact contact)
	{
		ContactsToSendNotification.Add(contact);
	}

	public static void AddParkingTicketNotification(Contact contact)
	{
		ParkingTicketContact = contact;
		PendingParkingTicketNotifications++;
	}

	public static void RunHourly()
	{
		if (PendingParkingTicketNotifications > 0)
		{
			if (PendingParkingTicketNotifications == 1)
			{
				ShowNewMessageNotification(ParkingTicketContact);
			}
			else
			{
				Dictionary<string, string> notificationData = new Dictionary<string, string> { 
				{
					"amount",
					PendingParkingTicketNotifications.ToString()
				} };
				Notifications.Show(NotificationType.Info, "contacts_parking_tickets_received", notificationData, 4f, null, null, notificationSound: false);
			}
		}
		if (ContactsToSendNotification.Count > 5)
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { 
			{
				"amount",
				ContactsToSendNotification.Count.ToString()
			} };
			Notifications.Show(NotificationType.Info, "employeehelper_notification_employee_amount_messaged_you", notificationData2, 4f, null, null, notificationSound: false);
		}
		else
		{
			foreach (Contact item in ContactsToSendNotification)
			{
				ShowNewMessageNotification(item);
			}
		}
		ContactsToSendNotification.Clear();
		ParkingTicketContact = null;
		PendingParkingTicketNotifications = 0;
	}

	public static void ShowNewMessageNotification(Contact contact)
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { { "sender", contact.id } };
		Notifications.Show(NotificationType.Info, "contacts_new_message", notificationData, 4f, null, contact.OnClickNotification, notificationSound: false);
	}

	public static void UnlockAllContacts()
	{
		foreach (Building allBuilding in BuildingHelper.allBuildings)
		{
			if (!(allBuilding.SpecialService == null) && allBuilding.SpecialService.isBusinessContact)
			{
				allBuilding.GetRegistration().GetOrAddBusinessContact();
			}
		}
	}

	public static ContactPreset GetPreset(string id)
	{
		Init();
		return ContactPresets.GetValueOrDefault(id);
	}

	public static List<string> GetPermanentIds()
	{
		Init();
		return (from x in ContactPresets.Values
			where x.isPermanent
			select x.id).ToList();
	}

	public static void FillBillboardPresets(List<ContactPreset> presets)
	{
		Init();
		presets.Clear();
		foreach (ContactPreset value in ContactPresets.Values)
		{
			if (value.hasBillboard)
			{
				presets.Add(value);
			}
		}
	}

	public static bool TryGetFirstNameInitial(string fullName, out char initial)
	{
		initial = '\0';
		if (string.IsNullOrEmpty(fullName))
		{
			return false;
		}
		ReadOnlySpan<char> readOnlySpan = fullName.AsSpan().TrimStart();
		if (readOnlySpan.IsEmpty)
		{
			return false;
		}
		initial = char.ToUpperInvariant(readOnlySpan[0]);
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ContactsToSendNotification.Clear();
		ContactPresets = null;
		ParkingTicketContact = null;
		PendingParkingTicketNotifications = 0;
		HasUpdatedCategories = false;
	}
}
