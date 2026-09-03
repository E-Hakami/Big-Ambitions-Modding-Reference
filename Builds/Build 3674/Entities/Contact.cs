using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.DayNightCycle;
using Dialogs;
using Extensions;
using Helpers;
using IngameDebugConsole;
using UI;
using UI.Notification;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities;

[Serializable]
public class Contact
{
	public const int MaxMessages = 20;

	public const string EmployeeContactDescription = "employee_contact_description";

	private static readonly List<Contact> AddedContactNotifications = new List<Contact>();

	[FormerlySerializedAs("name")]
	public string id;

	public Queue<TextMessage> messagesQueue;

	public Timestamp lastTimeUpdated;

	[IgnoreDataMember]
	public string streetName;

	[IgnoreDataMember]
	public int streetNumber;

	[IgnoreDataMember]
	public string description;

	[IgnoreDataMember]
	public ContactCategoryName category;

	[NonSerialized]
	[IgnoreDataMember]
	public ContactCellView contactCellView;

	[NonSerialized]
	[IgnoreDataMember]
	public CallDialogType callDialogTypeOverride;

	[IgnoreDataMember]
	public Address Address => new Address(streetName, streetNumber);

	[IgnoreDataMember]
	public bool IsEmployeeContact => description == "employee_contact_description";

	[IgnoreDataMember]
	public bool HasAddressAssigned
	{
		get
		{
			if (!string.IsNullOrEmpty(streetName))
			{
				return streetNumber != 0;
			}
			return false;
		}
	}

	[IgnoreDataMember]
	public int NumberOfUnreadMessages => messagesQueue.Count((TextMessage x) => x != null && !x.read);

	[IgnoreDataMember]
	public bool HasUnreadMessages => NumberOfUnreadMessages > 0;

	public Contact()
	{
	}

	public Contact(string id, ContactCategoryName category, string description = "Unknown", Address address = null, Queue<TextMessage> initialMessages = null)
	{
		this.id = id;
		this.description = description;
		this.category = category;
		if (address != null)
		{
			streetName = address.streetName;
			streetNumber = address.streetNumber;
		}
		messagesQueue = initialMessages ?? new Queue<TextMessage>(20);
		lastTimeUpdated = initialMessages?.Last().timestamp ?? TimeHelper.Now();
	}

	public void SendMessage(TextMessage textMessage, bool notify = true, bool sendNotificationInstantly = false)
	{
		lastTimeUpdated = textMessage.timestamp;
		messagesQueue.Enqueue(textMessage);
		CleanOldMessages();
		if (!(InstanceBehavior<UIs>.Instance != null) || textMessage.read || InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.ShowMessageIfOpen(this, textMessage))
		{
			return;
		}
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts);
		if (notify)
		{
			if (sendNotificationInstantly)
			{
				ContactsHelper.ShowNewMessageNotification(this);
			}
			else
			{
				ContactsHelper.AddContactToSendNotification(this);
			}
		}
	}

	public void OnClickNotification()
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.canvas.isActiveAndEnabled)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
			InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(this);
			ReadAllMessages();
		}
	}

	public static void OnClickNotificationMultiple()
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.canvas.isActiveAndEnabled)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
		}
	}

	public void ReceivePlayerMessage(TextMessage textMessage)
	{
		lastTimeUpdated = textMessage.timestamp;
		textMessage.isFromPlayer = true;
		textMessage.read = true;
		messagesQueue.Enqueue(textMessage);
		CleanOldMessages();
	}

	public void CleanOldMessages()
	{
		while (messagesQueue.Count > 20)
		{
			TextMessage removedMessage = messagesQueue.Dequeue();
			if (removedMessage == null || removedMessage.contextAction == null)
			{
				continue;
			}
			if (removedMessage.contextAction.type == TextMessage.ContextAction.ContextActionType.HealthInsurancePlanOffer)
			{
				if (!string.IsNullOrEmpty(removedMessage.contextAction.employeeInstanceId))
				{
					if (SaveGameManager.Current.CandidateEmployeeInstances.RemoveAll((EmployeeInstance x) => x.id == removedMessage.contextAction.employeeInstanceId) > 0)
					{
						EmployeeHelper.EmployeeInstancesDictionary.Remove(removedMessage.contextAction.employeeInstanceId);
					}
				}
				else if (!string.IsNullOrEmpty(removedMessage.contextAction.healthPlanOfferId))
				{
					SaveGameManager.Current.healthInsurancePlanOffers.RemoveAll((HealthInsurancePlanOffer x) => x.id == removedMessage.contextAction.healthPlanOfferId && !x.negotiationFinished);
				}
			}
			else if (removedMessage.contextAction.type == TextMessage.ContextAction.ContextActionType.SalaryNegotiation)
			{
				SaveGameManager.Current.candidateSalaryNegotiations.RemoveAll((CandidateSalaryNegotiation x) => x.id == removedMessage.contextAction.salaryNegotiationId && !x.completed);
				if (SaveGameManager.Current.CandidateEmployeeInstances.RemoveAll((EmployeeInstance x) => x.id == removedMessage.contextAction.employeeInstanceId) > 0)
				{
					EmployeeHelper.EmployeeInstancesDictionary.Remove(removedMessage.contextAction.employeeInstanceId);
				}
			}
		}
	}

	public void ReadAllMessages()
	{
		foreach (TextMessage item in messagesQueue)
		{
			item.read = true;
		}
		InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
	}

	public static Contact AddContact(BuildingRegistration linkedBuilding, ContactCategoryName category, bool hasWelcomeMessages = false)
	{
		return GetContact(linkedBuilding, category, hasWelcomeMessages);
	}

	public static Contact AddContact(string name, ContactCategoryName category, string description, BuildingRegistration linkedBuilding = null, bool hasWelcomeMessages = false)
	{
		return GetContact(name, category, description, linkedBuilding?.Address, hasWelcomeMessages);
	}

	public static Contact GetContact(BuildingRegistration linkedBuilding, ContactCategoryName category, bool hasWelcomeMessages = false)
	{
		return GetContact(linkedBuilding.BusinessName, category, linkedBuilding.businessTypeName, linkedBuilding.Address, hasWelcomeMessages);
	}

	public static Contact GetContact(string name, ContactCategoryName category, string description, Address address = null, bool hasWelcomeMessages = false, bool skipNewNotification = false)
	{
		Contact contact = SaveGameManager.Current.Contacts.Find((Contact x) => x.id == name && x.description == description);
		if (contact != null)
		{
			return contact;
		}
		return CreateContact(name, category, description, address, hasWelcomeMessages, skipNewNotification);
	}

	public static Contact EnsurePermanentContact(string id, ContactCategoryName category, string description)
	{
		Contact contact = SaveGameManager.Current.Contacts.Find((Contact x) => x.id == id);
		if (contact == null)
		{
			contact = GetContact(id, category, description, null, hasWelcomeMessages: false, skipNewNotification: true);
		}
		contact.category = category;
		contact.description = description;
		return contact;
	}

	private static Contact CreateContact(string name, ContactCategoryName category, string description, Address address = null, bool hasWelcomeMessages = false, bool skipNewNotification = false)
	{
		Contact contact = new Contact(name, category, description, address, hasWelcomeMessages ? GenerateWelcomeMessages(address) : null);
		if (hasWelcomeMessages)
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdateBadgeCount(AppName.Contacts, playSound: false);
		}
		SaveGameManager.Current.Contacts.Add(contact);
		if (category != ContactCategoryName.Employees && !SaveGameManager.Current.gameVariables.allContactsUnlocked && !skipNewNotification)
		{
			AddedContactNotifications.Add(contact);
		}
		ContactsApp.onContactAdded?.Invoke();
		return contact;
	}

	public static void ShowAddedContactNotifications()
	{
		if (AddedContactNotifications.Count != 0)
		{
			if (AddedContactNotifications.Count == 1)
			{
				Contact contact = AddedContactNotifications[0];
				Dictionary<string, string> notificationData = new Dictionary<string, string> { { "name", contact.id } };
				Notifications.Show(NotificationType.Info, "new_contact_added_notification", notificationData, 4f, null, contact.OnClickNotification);
			}
			else
			{
				Dictionary<string, string> notificationData2 = new Dictionary<string, string> { 
				{
					"count",
					AddedContactNotifications.Count.ToString()
				} };
				Notifications.Show(NotificationType.Info, "new_contact_added_notification_multiple", notificationData2, 4f, null, OnClickNotificationMultiple);
			}
			AddedContactNotifications.Clear();
		}
	}

	[ConsoleMethod("ReceiveMessage", "Receive a message from a contact", new string[] { })]
	public static void Command_ReceiveMessage(ContactCategoryName category, string messageKey)
	{
		Contact contact = SaveGameManager.Current.Contacts.Find((Contact c) => c.category == category && c.id == "Test" && c.description == "This is a test contact");
		if (contact == null)
		{
			contact = new Contact("Test", category, "This is a test contact");
			SaveGameManager.Current.Contacts.Add(contact);
			ContactsApp.onContactAdded?.Invoke();
		}
		contact.SendMessage(new TextMessage(messageKey));
	}

	public static Queue<TextMessage> GenerateWelcomeMessages(Address address)
	{
		Queue<TextMessage> queue = new Queue<TextMessage>(20);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration == null)
		{
			List<string> list = new List<string> { "ba:messagetype_phone_welcome_message_friendly_1", "ba:messagetype_phone_welcome_message_friendly_2", "ba:messagetype_phone_welcome_message_friendly_3" };
			queue.Enqueue(new TextMessage(list.GetRandom()));
			return queue;
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
		List<string> list2 = new List<string> { "ba:messagetype_phone_welcome_message_business_1", "ba:messagetype_phone_welcome_message_business_2", "ba:messagetype_phone_welcome_message_business_3", "ba:messagetype_phone_welcome_message_business_4" };
		queue.Enqueue(new TextMessage(list2.GetRandom(), messageData, read: true));
		return queue;
	}

	public Sprite GetPredefinedIconSprite()
	{
		string text = (IsEmployeeContact ? "employee" : id);
		Sprite[] contactIcons = InstanceBehavior<GlobalReferences>.Instance.contactIcons;
		foreach (Sprite sprite in contactIcons)
		{
			if (sprite.name == text)
			{
				return sprite;
			}
		}
		return null;
	}
}
